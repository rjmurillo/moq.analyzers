---
name: dotnet-api-design-standards
description: "Apply the .NET BCL-quality and Framework Design Guidelines bar that moq.analyzers holds all code to. Load this skill when writing or reviewing ANY C# in src/ (analyzers, code fixes, Common helpers, PerfDiff), when naming a new file/type/member, when designing a method signature (Try-pattern, out params, nullable annotations), when deciding whether to catch/throw/return-null, when touching a per-operation analyzer callback (allocations, LINQ, caching), when writing XML docs, or when a reviewer says \"not BCL quality\" and you need to know what that concretely means here. Do NOT load for: pure formatting/style trivia (dotnet format automates that), Roslyn API mechanics (see roslyn-analyzer-reference), Moq API semantics (see moq-api-reference), build/CI commands (see moq-analyzers-build-and-env), or the process for landing a change (see moq-analyzers-change-control)."
---

# .NET API Design Standards — the moq.analyzers quality bar

**Who this is for:** engineers/agents who know C# but have never worked to Base Class
Library (BCL) standards. "BCL quality" = the bar Microsoft holds `System.*` code to:
crash-free, allocation-conscious, thread-safe by default, precisely documented, with
API shapes (Try-patterns, nullability, exception contracts) that cannot be misused.

**Why the bar is this high:** these analyzers execute inside consumers' compilers and
IDEs on every keystroke, in mission-critical codebases. `.github/copilot-instructions.md`
(the repo's binding AI/contributor contract) states: *"All code MUST adhere to the strict
quality and performance standards of the .NET Base Class Library (BCL)"* and *"An analyzer
crash is worse than a missed diagnostic."* Every rule below is enforced somewhere
(compiler, analyzer package, CI, or review) — none is aspirational.

Priority order (fixed, from project ground rules): (1) no analyzer crashes,
(2) no false positives/negatives, (3) per-keystroke performance, (4) thread safety.
When two guidelines conflict, the higher priority wins.

---

## 1. Naming conventions

### 1.1 File and type naming (mandatory, from `.github/copilot-instructions.md` "AI Agent Coding Rules" #3)

| Artifact | Pattern | Real example (verified 2026-07-02) |
|---|---|---|
| Analyzer | `src/Analyzers/[Description]Analyzer.cs` | `NoSealedClassMocksAnalyzer.cs` |
| Code fix | `src/CodeFixes/[Description]Fixer.cs` | `VerifyOverridableMembersFixer.cs` |
| Analyzer tests | `tests/Moq.Analyzers.Test/[Description]AnalyzerTests.cs` | `NoSealedClassMocksAnalyzerTests.cs` |
| Code-fix tests | `tests/Moq.Analyzers.Test/[Description]CodeFixTests.cs` | `CallbackSignatureShouldMatchMockedMethodCodeFixTests.cs` |
| Large test split | partial-class files `[Description]AnalyzerTests.[Facet].cs` | `ConstructorArgumentsShouldMatchAnalyzerTests.Doppelganger.cs` (9 partials) |
| Benchmark | `tests/Moq.Analyzers.Benchmarks/Moq{Id}[Description]Benchmarks.cs` | `Moq1202RaiseEventBenchmarks.cs` |
| Shared helper | `src/Common/[Concept]Extensions.cs` or `[Concept]Helpers.cs` | `SemanticModelExtensions.cs`, `MockDetectionHelpers.cs` |

`[Description]` is a sentence-like behavioral name ("what rule does this enforce"),
not the rule ID: `SetupShouldBeUsedOnlyForOverridableMembersAnalyzer`, not `Moq1200Analyzer`.

### 1.2 The registry

`docs/rules/README.md` is the master table: **ID → Category → Title → Implementation
File**, one row per rule (25 rules Moq1000–Moq1600 as of 2026-07-02). Every new or
renamed analyzer MUST update this table — it is how humans and tools map a diagnostic
ID to its source file. It also defines the ID range allocation (Moq1000-1099 Usage,
1100-1299 Correctness, 1300-1399 Usage, 1400-1599 Best Practice, 1600-1699 Usage,
1700-1999 reserved).

### 1.3 Member naming (observed repo-wide, matches Framework Design Guidelines)

| Member | Convention | Example (real) |
|---|---|---|
| Types, methods, properties | PascalCase | `MoqKnownSymbols.Mock1Setup` |
| Private fields | `_camelCase` | `_mockBehaviorStrict` (`src/Common/WellKnown/MoqKnownSymbols.cs`) |
| Descriptor plumbing | `static readonly` `Title`/`Message`/`Description` + `Rule` | `NoSealedClassMocksAnalyzer.cs:12-24` |
| Boolean-returning probes | `Is...`/`Has...`/`Try...` prefix | `IsValidMockCreation`, `TryGetMockedTypeFromGeneric` |
| Generic-arity twins | suffix arity digit | `Mock`/`Mock1`, `ILogger`/`ILogger1`, `Task1` (mirrors metadata `` Mock`1 ``) |
| Visibility | `public` only for analyzer/fixer classes Roslyn must discover; everything in `src/Common` is `internal` | `internal static class MockDetectionHelpers` |

Analyzer classes must live in namespace **exactly `Moq.Analyzers`** —
`AllAnalyzersVerifier` discovers analyzers by reflection over that namespace, and a
wrong namespace silently drops the analyzer from the "no diagnostics" test suites.

---

## 2. API shape rules

### 2.1 Symbols and strong types over strings (ADR-001 — settled doctrine)

`docs/architecture/ADR-001-symbol-based-detection-over-string-matching.md`: all type
and method identity checks use `ISymbol` + `SymbolEqualityComparer.Default`. String
comparison of names is **never** a semantic decision. The single sanctioned use of a
name string is a *fast-path pre-filter that rejects early before an authoritative
symbol check* (pattern: `SemanticModelExtensions.IsMoqFluentInvocation`).

Enforced mechanically by `src/BannedSymbols.txt`:

- `Compilation.GetTypeByMetadataName` is banned → use `KnownSymbols` /
  `MoqKnownSymbols` (`src/Common/WellKnown/`).
- Raw `Diagnostic.Create` is banned → use `DiagnosticExtensions.CreateDiagnostic`
  (`src/Common/DiagnosticExtensions.cs`), which also normalizes non-source locations
  to `Location.None`.

Same principle for data: prefer enums over magic strings, `ImmutableArray<T>` over
`IEnumerable<T>` in stored state, strong record types over property-bag dictionaries.
When a dictionary is unavoidable (Roslyn's diagnostic `Properties` bag is
`ImmutableDictionary<string, string?>`), wrap it in a typed parser — see
`DiagnosticEditProperties.TryGetFromImmutableDictionary` below.

### 2.2 Try-pattern discipline

The Try-pattern (`bool TryX(..., out T? result)`) is the required shape for "may fail,
failure is normal" operations. Contract, in order:

1. Assign the `out` parameter on **every** path; failure paths set it to `null`/default
   **before** any early return.
2. Return `false` for every failure; **never throw** for expected-failure inputs.
3. Annotate with `[NotNullWhen(true)]` so callers get flow-analysis: no null check and
   no `!` needed after a `true` return.
4. Result must be non-null **whenever** the method returns `true` — the annotation is a
   promise the implementation must keep.

Real example (`src/Common/MockDetectionHelpers.cs:102-113`, verbatim):

```csharp
internal static bool TryGetMockedTypeFromGeneric(ITypeSymbol type, [NotNullWhen(true)] out ITypeSymbol? mockedType)
{
    mockedType = null;

    if (type is not INamedTypeSymbol namedType || namedType.TypeArguments.Length != 1)
    {
        return false;
    }

    mockedType = namedType.TypeArguments[0];
    return true;
}
```

There are ~20 `[NotNullWhen(true)]` sites across `src/` (grep `NotNullWhen` to see
them). A **missing** annotation forces callers into `!` suppressions: the 2026-07-02
audit flagged `TryGetEventNameFromLambdaSelector` (`src/Common/SemanticModelExtensions.cs`)
whose missing annotation forces `eventName!` at `src/Common/EventSyntaxExtensions.cs:173`.
Do not replicate that: annotate the Try-method instead of suppressing at the call site.

**Known pitfall — `Enum.TryParse` violates the Try contract** (audit finding, tracked as
issue #1257, open as of 2026-07-02): `Enum.TryParse("7", out EditType e)` returns `true`
with the undefined value `(EditType)7`, which later detonates in a `switch` fallback arm.
`src/Common/DiagnosticEditProperties.cs:68` has exactly this hole. When parsing enums
from untrusted/round-tripped strings, add a definedness check:

```csharp
if (!Enum.TryParse(editTypeString, out EditType editType)) { return false; }
// netstandard2.0 has no generic Enum.IsDefined<TEnum>; use the Type-based overload.
if (!Enum.IsDefined(typeof(EditType), editType)) { return false; }
```

### 2.3 Out-param and return contracts

- Never use in-band sentinels (`-1`, `NaN`, magic strings) where a `bool` + `out` or a
  nullable return is available. (PerfDiff's internal `double.NaN` sentinel was audited
  as a fragile contract — do not copy it into new code.)
- Multi-value success results: multiple `[NotNullWhen(true)]` outs on one Try-method is
  fine — see `IMethodSymbolExtensions.TryGetOverloadWithParameterOfType`
  (`src/Common/IMethodSymbolExtensions.cs:46`) with two annotated outs.
- Distinguish tri-state honestly: `ConstructorArgumentsShouldMatchAnalyzer` uses
  `bool?` where `null` means "ignore, don't diagnose" — document such semantics in
  `<returns>`.

---

## 3. Nullable annotation discipline

- `<Nullable>enable</Nullable>` is set repo-wide in
  `build/targets/compiler/Compiler.props` (every project inherits it). New files never
  opt out (`#nullable disable` is not used anywhere in `src/`).
- Roslyn APIs return nullable symbols constantly (`GetSymbolInfo(...).Symbol`,
  `knownSymbols.Mock1`, `operation.Type`). The rule: **check, don't assert**. The
  null-forgiving operator `!` on a value you have not proven non-null is banned by
  project doctrine.
- History that made this doctrine (all on `main`): `ffed678` "use
  SymbolEqualityComparer for symbol comparisons" (#997), `f9ec6ca` "replace
  null-forgiving operator with proper null guards" (#998), `c61a66a` "treat
  unresolvable parameter types as mismatch in callback validation" (#1000), `f0161a7`
  "guard code fix registration when prerequisites are null" (#1004). Each was a
  hardening fix for a `!` or missing guard that could crash or mis-diagnose on
  mid-edit code. Mid-edit code (unresolved symbols, error types, incomplete argument
  lists) is a first-class input, not an edge case.
- The narrow sanctioned exception: `!` backed by a **local, provable** invariant.
  `MockBehaviorDiagnosticAnalyzerBase` uses `knownSymbols.MockBehavior!` only because
  its registration guard already returned early when `MockBehavior is null`. The audit
  still recommends making such invariants local (null check or `Debug.Assert`) because
  a future subclass can bypass the guard. Prefer the guard; if you must use `!`, the
  proof must be in the same method or an inherited contract stated in XML docs.
- `SymbolEqualityComparer.Default.Equals(a, b)` for all symbol comparisons — never
  `==` on `ISymbol` (and never `==` on boxed `ConstantValue.Value` objects either;
  that is reference equality of boxes — audit finding A-10).

---

## 4. Exceptions vs graceful degradation

Two different worlds with opposite rules. Know which one you are in.

### 4.1 Analyzer world (`src/Analyzers`, `src/CodeFixes`, `src/Common`) — ZERO catch blocks

Verified 2026-07-02: these three directories contain **no catch blocks at all** (grep
`catch` — only comments match). This is by design, not omission:

- An unhandled exception in an analyzer surfaces as an `AD0001` diagnostic and Roslyn
  **disables the analyzer for the session** — the worst possible outcome (priority-1
  violation). You do not fix that with `try/catch`; a swallowed exception hides a bug
  and can silently kill diagnostics. You fix it by making the crash impossible.
- Therefore: **no defensive catch-all, and no reachable throw either.** Robustness
  comes from guards — pattern matching (`is not INamedTypeSymbol namedType`), bounds
  checks before indexing (`Arguments.Count > 0` before `Arguments[0]`), `TypeKind.Error`
  checks, and early `return` (missed diagnostic on weird input beats any exception).
- Unguarded casts/indexing are audit-class bugs even when "currently unreachable":
  the 2026-07-02 audit's single Critical finding is an unguarded
  `(IArrayTypeSymbol)paramsParameter.Type` cast in
  `ConstructorArgumentsShouldMatchAnalyzer.cs` that throws `InvalidCastException` on
  C# 13 `params ReadOnlySpan<T>` collections in newer host compilers.
- `throw` is acceptable only for **programmer-error preconditions in internal
  helpers** where the caller controls the input completely: `ArgumentNullException`
  guards in `EnumerableExtensions.DefaultIfNotSingle`, the index/kind validation
  throws in `SyntaxGeneratorExtensions`. These document a contract, and their call
  sites must guarantee the precondition (when they can't — stale IDE state — the
  caller must validate first and no-op; see issue #1257's fix plan).
- Code fixes degrade by **returning the original document**, never by throwing: a
  no-op lightbulb is annoying; a "code fix encountered an error" dialog is a bug.

### 4.2 Tool world (`src/tools/PerfDiff`) — catch narrowly, log, exit distinctly

PerfDiff is a CLI gate, not an analyzer. It has exactly 4 C# catch sites (exhaustive,
verified 2026-07-02), all narrow and all loud:

| Site | Catches | Behavior |
|---|---|---|
| `src/tools/PerfDiff/Program.cs:53` | `FileNotFoundException` | log + `return 1` (`UnhandledExceptionExitCode`) |
| `src/tools/PerfDiff/Program.cs:60` | `OperationCanceledException` | log + `return 2` (`CancelledExitCode`) |
| `src/tools/PerfDiff/BDN/BenchmarkFileReader.cs:32` | JSON/IO/security exception list | per-file log + null; caller hard-fails |
| `src/tools/PerfDiff/ETL/EtlDiffer.cs:31` | `InvalidOperationException or IOException or UnauthorizedAccessException` | degrade to "comparison unavailable" |

The pattern to copy: **specific exception types (use `when (ex is A or B)` filters),
log with context, convert to a distinct exit code**. Never `catch (Exception)`, never
an empty catch body. For a gate tool the failure-direction matters: an exception that
becomes exit 1 falsely *blocks* CI (annoying, visible); a swallowed one falsely
*passes* it (silent, dangerous). Design so errors block.

---

## 5. Allocation-conscious patterns for hot paths

A "hot path" here = any code reachable from a per-operation/per-syntax-node callback,
i.e., code that runs for every matching node in every file on every keystroke.

### 5.1 Cache symbols once per compilation (ADR-006)

`docs/architecture/ADR-006-wellknown-types-pattern-for-moq-symbol-resolution.md`:
construct `MoqKnownSymbols` exactly once inside `RegisterCompilationStartAction`, then
pass it into every callback. Never construct it (or call any metadata lookup) inside a
per-operation callback. This was retrofitted at real cost — commits `9febdda` (#1026,
per-operation → per-compilation) and `3b5ac71` (#1033, same fix in 9 more analyzers).
Canonical shape: `NoSealedClassMocksAnalyzer.RegisterCompilationStartAction` — create
`MoqKnownSymbols`, gate on `IsMockReferenced()`, gate on the specific symbols you
need, then register the narrow operation kinds.

### 5.2 No LINQ / closures / formatting on the no-diagnostic path

The overwhelmingly common outcome of a callback is "no diagnostic". Anything you
compute before deciding is pure overhead multiplied by every keystroke:

- **Defer string building to the report path.** `ToDisplayString()`,
  `FormatArguments()`, `StringBuilder` work happens only inside the branch that calls
  `ReportDiagnostic`. Current cleanup of exactly this class of defect: issue #1260
  ("Defer report-path allocations in Moq1001/1002, Moq1202/1204, and Moq1400/1410 hot
  paths", open as of 2026-07-02) — read it for before/after code.
- **Reject cheaply before binding.** `SemanticModel.GetSymbolInfo` is the most
  expensive call in a callback. Pre-filter by syntax shape and (per ADR-001's fast-path
  allowance) by member-name text before binding, and thread
  `context.CancellationToken` into every `GetSymbolInfo`/`GetTypeInfo` call. Current
  cleanup: issue #1259 (five analyzers bind every invocation, open as of 2026-07-02).
- **Replace LINQ chains with loops in callbacks.** `DescendantsAndSelf().OfType<T>().Any(closure)`
  allocates an enumerator + delegate + closure per call; an explicit `foreach` with a
  pattern match allocates nothing. `.Where(...).ToArray()` over members → indexed loop
  over `GetMembers(...)`.
- **Resolve once, pass through.** If a helper already resolved a symbol, surface it via
  an out param instead of letting the caller re-bind it (the Raise/Raises analyzers'
  double-bind is part of #1260).

### 5.3 Zero-allocation views over syntax

`src/Common/FilteredArgumentList.cs` is the house exemplar: a `readonly struct`
(`[StructLayout(LayoutKind.Auto)]`) wrapping a `SeparatedSyntaxList<ArgumentSyntax>`
that *logically* excludes one element by index — replacing an earlier
`ToArray()`/`RemoveAt()` approach (its superseded predecessor `ArrayExtensions` is
documented as such in the file header). It even defers its own `FormatArguments()`
string building behind a method so callers can delay it to the report path. Pattern:
when you need "collection minus/plus an element" transiently, write an indexing view
struct, not a copy.

### 5.4 Watchdogs

`Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers`, `Meziantou.Analyzer`,
`EffectiveCSharp.Analyzers` (ECS0900 flags boxing; all ECS rules = warning via
`build/targets/codeanalysis/.globalconfig`) run on every build; `PedanticMode` turns
warnings into errors in CI. The PerfDiff benchmark gate (ADR-008) is a required PR
check — a real regression blocks merge. Every rule has (or should get) a
`tests/Moq.Analyzers.Benchmarks/Moq{Id}...Benchmarks.cs`.

---

## 6. Immutability and thread-safety defaults

All 24 analyzer classes (25 rule IDs) call `EnableConcurrentExecution()` — every
callback may run on many threads at once. The defaults that make that safe (audit verified all of `src/` clean
on 2026-07-02):

| Default | Rule | Repo evidence |
|---|---|---|
| Static state is `static readonly` immutable | descriptors, `LocalizableString`s, name arrays only; **zero mutable statics** reachable from callbacks | every analyzer's `Rule` field |
| Lazy init uses `LazyThreadSafetyMode.ExecutionAndPublication` | value computed once even under concurrent access | `MoqKnownSymbols.CreateLazyMethods` (`src/Common/WellKnown/MoqKnownSymbols.cs:611-617`) |
| Shared caches are `ConcurrentDictionary`-backed | via `WellKnownTypeProvider.GetOrCreate(compilation)` from AnalyzerUtilities (pinned ≤3.3.4, ADR-004) | `KnownSymbols` ctor (`src/Common/WellKnown/KnownSymbols.cs`) |
| Per-compilation state flows as parameters | no fields written from callbacks; capture `knownSymbols` in the registration lambda | `RegisterOperationAction(ctx => Analyze(ctx, knownSymbols), ...)` |

**Caution — `Lazy` caches exceptions.** Under `ExecutionAndPublication`, a factory
that throws rethrows the same exception to every later caller for the compilation's
lifetime. The audit flagged `CreateLazySingleField`'s `SingleOrDefault()` (throws on a
duplicated source-defined member → permanently poisoned symbol, persistent AD0001).
Lazy factories must be non-throwing: `FirstOrDefault()` or the repo's
`EnumerableExtensions.DefaultIfNotSingle` (documented as "SingleOrDefault combined
with a catch that returns default", implemented as an allocation-free loop).

---

## 7. XML documentation house rules

`GenerateDocumentationFile=true` repo-wide (`build/targets/codeanalysis/CodeAnalysis.props`)
means missing docs on **public** members = CS1591 = build warning = CI error under
PedanticMode. SA1600 (docs on *all* members) is deliberately `silent`
(`.editorconfig:106-107`), but the observed house standard documents `internal` shared
helpers in `src/Common` too — do the same.

Required tag usage (all examples verifiable in the cited files):

| Tag | Use for | Real example |
|---|---|---|
| `<see cref="X"/>` | any code identifier reference | `<see cref="ImmutableDictionary{TKey, TValue}"/>` — `DiagnosticEditProperties.cs:38` |
| `<see langword="true"/>` | keywords: `true`/`false`/`null`/`params` | `<see langword="true"/> if parsing succeeded` — `DiagnosticEditProperties.cs:54` |
| `<paramref name="p"/>` | referring to a parameter in prose | `<paramref name="source"/> is <see langword="null"/>` — `EnumerableExtensions.cs:6` |
| `<c>...</c>` | inline code that isn't a resolvable cref (generic Moq syntax like `Mock{T}`) | `a valid <c>Mock{T}</c> creation` — `MockDetectionHelpers.cs:13` |
| `<remarks>` | perf notes, thread-safety notes, design intent | ExecutionAndPublication rationale — `MoqKnownSymbols.cs:610-614` |
| `<exception cref="E">` | every throw a caller can trigger | `EnumerableExtensions.cs:6,30` |
| `<inheritdoc/>` / `<inheritdoc cref=".."/>` | overrides and overload families | `NoSealedClassMocksAnalyzer.cs:26,29`; `EnumerableExtensions.cs:5` |

Try-pattern doc shape (copy it): `<param name="result">The output X if parsing
succeeded, otherwise <see langword="null"/>.</param>` + `<returns><see langword="true"/>
if ...; <see langword="false"/> otherwise.</returns>`.

---

## 8. Complexity budget

| Budget | Limit | Enforcement (2026-07-02) |
|---|---|---|
| Method length | ≤ 60 lines | Meziantou `MA0051` at default severity in `src/` (warning → CI error via PedanticMode); relaxed to `suggestion` in `tests/.editorconfig`, `none` in `src/tools/PerfDiff/.editorconfig` |
| Cyclomatic complexity | ≤ 10 per method | project doctrine (maintainer review bar; Codacy runs `lizard` for complexity) — no in-repo `.editorconfig` pin, so reviewers enforce it |
| Coverage | 100% block coverage for new analyzer code | review gate; coverage summary is required PR evidence |

Suppressions are allowed only with a justification that explains *why the budget
doesn't apply*, not *that it was inconvenient*. Good: `MoqKnownSymbols.cs:66` —
`#pragma warning disable MA0051 // Constructor length is proportional to the number of
Moq symbols cached; each line is a single-field assignment.` Tolerated-but-tracked
debt: the `[SuppressMessage(..., Justification = "Should be fixed...")]` pair in
`SetExplicitMockBehaviorAnalyzer.cs:30` / `SetStrictMockBehaviorAnalyzer.cs:54`. Do
not add new suppressions of the second kind; refactor instead (extract guard clauses,
split the method along its comment boundaries).

---

## 9. Reviewer checklist: what "not BCL quality" concretely means here

Run this list against any diff in `src/`. Each item is a real, previously-shipped (or
audit-caught) defect class in this repo — none is theoretical.

### Crash safety (priority 1)

- [ ] No unguarded cast (`(IArrayTypeSymbol)x`) — pattern-match instead (`is not ... return`).
- [ ] No unguarded indexing (`Arguments[0]`) — check `Count` first.
- [ ] No `!` on a value not proven non-null in the same method/contract.
- [ ] No `SingleOrDefault`/`Single`/`First` where multiplicity isn't guaranteed — `FirstOrDefault` or `DefaultIfNotSingle`.
- [ ] Error types (`TypeKind.Error`) and mid-edit shapes handled by early return.
- [ ] No catch blocks in analyzer/codefix/Common code; no throw reachable from analyzer callbacks.

### API shape

- [ ] Failure-is-normal ops use Try-pattern with `[NotNullWhen(true)]`, no exceptions.
- [ ] Enum parsing from strings checks `Enum.IsDefined` (netstandard2.0: `typeof`-overload).
- [ ] Symbol identity via `SymbolEqualityComparer`/`IsInstanceOf`, never `==` or name strings (ADR-001).
- [ ] New Moq symbols registered in `MoqKnownSymbols` + a resolves-non-null test (phantom-symbol trap).
- [ ] Diagnostics created via `DiagnosticExtensions.CreateDiagnostic` (BannedSymbols enforces).

### Hot path

- [ ] `MoqKnownSymbols` created once per compilation start, never per operation (ADR-006).
- [ ] Cheap rejection (syntax shape / name fast-path) before any `GetSymbolInfo`.
- [ ] `context.CancellationToken` threaded into every bind.
- [ ] No LINQ chains, closures, `ToArray()`, or `ToDisplayString()` before the report decision.

### Concurrency

- [ ] `EnableConcurrentExecution` + `ConfigureGeneratedCodeAnalysis` in `Initialize`.
- [ ] No mutable state reachable from callbacks; statics are `readonly` immutable.
- [ ] `Lazy` uses `ExecutionAndPublication` and a non-throwing factory.

### Documentation & size

- [ ] XML docs on public members (CS1591) and on shared internal helpers, using `<see cref>`, `<see langword>`, `<paramref>`, `<c>`, `<exception>`.
- [ ] Methods ≤ 60 lines, cyclomatic ≤ 10, or a justified suppression explaining why the budget doesn't apply.
- [ ] File/type names follow §1; `docs/rules/README.md` updated for any rule change.

---

## When NOT to use this skill

- **Formatting/style trivia** (whitespace, using-order, `this.` prefixes): run
  `dotnet format` — do not hand-review what a tool fixes. Build/format commands live in
  **moq-analyzers-build-and-env**.
- **Roslyn API mechanics** (which context/registration/operation kind to use, span
  math): **roslyn-analyzer-reference**.
- **Moq semantics** (what `Setup`/`Raise`/`ItExpr` actually do, version diffs):
  **moq-api-reference**.
- **Architecture invariants and ADR text** (why netstandard2.0, why Roslyn 4.8):
  **moq-analyzers-architecture-contract**.
- **How to land a change** (branch names, PR evidence, release notes files):
  **moq-analyzers-change-control**; rule lifecycle steps: **moq-analyzers-rule-lifecycle**.
- **Test-writing patterns and validation gates**: **moq-analyzers-validation-and-qa**;
  proving FP fixes: **moq-analyzers-proof-toolkit** / **moq-analyzers-fp-convergence-campaign**.
- **Debugging a live failure**: **moq-analyzers-debugging-playbook**; past incidents:
  **moq-analyzers-failure-archaeology**.
- Other siblings for their domains: **moq-analyzers-config-and-flags**,
  **moq-analyzers-diagnostics-and-tooling**, **moq-analyzers-docs-and-writing**,
  **moq-analyzers-research-frontier**, **moq-analyzers-research-methodology**.

---

## Provenance and maintenance

Re-verify volatile claims with these one-liners (repo root):

- Zero catch blocks in analyzer code: `grep -rn "catch" src/Analyzers src/CodeFixes src/Common --include="*.cs"` (expect comment-only matches).
- PerfDiff catch inventory: `grep -rn "catch" src/tools/PerfDiff --include="*.cs"` (expect 4 sites: Program.cs ×2, BenchmarkFileReader.cs, EtlDiffer.cs).
- Nullable everywhere: `grep -n "Nullable" build/targets/compiler/Compiler.props` (expect `enable`).
- Banned APIs list: `cat src/BannedSymbols.txt`.
- `[NotNullWhen(true)]` sites: `grep -rn "NotNullWhen" src/`.
- Lazy thread-safety mode: `grep -n "ExecutionAndPublication" src/Common/WellKnown/MoqKnownSymbols.cs`.
- MA0051 scoping: `grep -rn "MA0051" src/tools/PerfDiff/.editorconfig tests/.editorconfig src/ --include="*.cs"`.
- SA1600/CS1591 stance: `grep -n "SA1600" .editorconfig`; doc generation: `grep -n "GenerateDocumentationFile" build/targets/codeanalysis/CodeAnalysis.props`.
- Rule registry row count (25 rules as of 2026-07-02): `grep -c "^| \[Moq" docs/rules/README.md`.
- Issue states (open as of 2026-07-02): #1257 (Enum.TryParse/stale-shape), #1259 (name pre-filters + CancellationToken), #1260 (defer report-path allocations) — check `https://github.com/rjmurillo/moq.analyzers/issues/1257` etc.
- Hardening-history commits: `git log --oneline --all | grep -E "ffed678|f9ec6ca|c61a66a|f0161a7|9febdda|3b5ac71|7595080"`.
- Naming rules source: `.github/copilot-instructions.md` ("AI Agent Coding Rules" #3); BCL mandate: same file, "Mission-Critical Quality Standard".
- Cyclomatic ≤10 has no `.editorconfig` pin — if one appears (`grep -rn -i "cyclomatic" .editorconfig build/`), update §8.

- Frontmatter must stay parser-safe (strict YAML: quote the description; no unquoted `#`); validate with any strict YAML parser before committing changes to this file.

Last verified: 2026-07-02 against commit 05135b2.
