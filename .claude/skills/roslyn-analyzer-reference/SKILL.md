---
name: roslyn-analyzer-reference
description: Teaches Roslyn analyzer fundamentals as they apply to this repo — load this when you know C# but not Roslyn and are about to read or write any file in src/Analyzers, src/CodeFixes, or src/Common; when you see unfamiliar terms like DiagnosticAnalyzer, ISymbol, IOperation, SyntaxNode, SemanticModel, CompilationStartAnalysisContext, AD0001, RS2000, CandidateSymbols, or equivalence key; or when you need to decide between syntax/symbol/operation APIs, understand diagnostic spans and {|MoqXXXX:...|} markup, or reason about host Roslyn versions vs the pinned 4.8 API. Do NOT load for Moq API semantics (moq-api-reference), the step-by-step new-rule process (moq-analyzers-rule-lifecycle), build/CI setup (moq-analyzers-build-and-env), or crash triage on a live failure (moq-analyzers-debugging-playbook).
---

# Roslyn Analyzer Reference (as applied in moq.analyzers)

This is the domain-knowledge pack a C#-fluent engineer is missing before touching
analyzer code. Every example is real code from this repo, verified 2026-07-02
against commit `05135b2`. Read top to bottom the first time; use the tables as a
lookup afterward.

Core mental model in one paragraph: a **Roslyn analyzer** is a plugin DLL that the
C# compiler and the IDE load into *their own process* and call back on every
compilation — including after every keystroke in an editor. It inspects code three
ways (raw syntax, declared symbols, bound operations) and reports **diagnostics**
(squiggles/build warnings) at character-precise locations. It has no `Main`, no
direct callers in this repo, and it must never crash, never lie, and never be slow,
because it runs inside every consumer's build and IDE session.

---

## 1. What a DiagnosticAnalyzer is and how it loads

A `DiagnosticAnalyzer` is a class deriving from
`Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer` and decorated with
`[DiagnosticAnalyzer(LanguageNames.CSharp)]`. The compiler discovers it **by
reflection** over every DLL placed at the NuGet package path `analyzers/dotnet/cs`.
This repo packs exactly three DLLs there (`src/Analyzers/Moq.Analyzers.csproj:43-45`):

```xml
<None Include="$(OutputPath)\Moq.Analyzers.dll" Pack="true" PackagePath="analyzers/dotnet/cs" ... />
<None Include="$(OutputPath)\Moq.CodeFixes.dll" Pack="true" PackagePath="analyzers/dotnet/cs" ... />
<None Include="$(OutputPath)\Microsoft.CodeAnalysis.AnalyzerUtilities.dll" Pack="true" PackagePath="analyzers/dotnet/cs" ... />
```

Consequences you must internalize:

| Fact | Consequence |
|---|---|
| Loaded by reflection, zero direct callers | Analyzer/fixer classes are **never dead code**. Do not delete or rename one because "nothing references it". Public entry points are the attribute + overrides. |
| Runs inside the consumer's compiler/IDE process | An unhandled exception surfaces as compiler warning **AD0001** ("analyzer threw an exception") and Roslyn disables that analyzer for the rest of the session. Priority #1 of this project: no crashes. There are deliberately **zero catch blocks** in `src/Analyzers`, `src/CodeFixes`, `src/Common` — crashes must be prevented, not swallowed (audit-verified 2026-07-02). |
| Test discovery is namespace-sensitive | `tests/Moq.Analyzers.Test/Helpers/AllAnalyzersVerifier.cs` reflection-discovers every `[DiagnosticAnalyzer]` in namespace **exactly `Moq.Analyzers`**. An analyzer in the wrong namespace silently drops out of the "no diagnostics on clean code" suites. |
| Ships as netstandard2.0 compiled against Roslyn 4.8 | See section 6 — the *host* Roslyn is usually newer than the API you compiled against. |

### Execution model

Roslyn instantiates each analyzer **once** and calls `Initialize(AnalysisContext)`
**once per analyzer lifetime** (not per compilation). Everything else is callback
registration:

1. `Initialize` registers a **compilation-start action** — this fires once per
   compilation (per keystroke in the IDE, a new compilation snapshot exists).
2. The compilation-start action does per-compilation setup (resolve Moq symbols)
   and registers **operation/syntax-node actions** — these fire for every matching
   node in every file, **concurrently on multiple threads**.
3. Callbacks call `context.ReportDiagnostic(...)` for findings.

Concurrency rules (repo doctrine, all 24 analyzer classes — covering 25 rule IDs — audited compliant 2026-07-02):

- Every `Initialize` calls `context.EnableConcurrentExecution()` (opt in to parallel
  callbacks; without it analysis is serialized and slow) and
  `context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None)` (skip
  generated code — don't analyze it, don't report in it).
- **No mutable state** reachable from callbacks. Statics are readonly descriptors.
  Per-compilation caching uses `Lazy<T>(LazyThreadSafetyMode.ExecutionAndPublication)`
  or `WellKnownTypeProvider`'s `ConcurrentDictionary`.
- Beware: `ExecutionAndPublication` **caches exceptions** too — a throwing `Lazy`
  factory poisons the value for the whole compilation (a live risk noted in the
  2026-07-02 audit for `MoqKnownSymbols.CreateLazySingleField`'s `SingleOrDefault`).

---

## 2. The three representations: SyntaxNode, ISymbol, IOperation

Roslyn exposes the same code at three levels. Choosing the wrong one is the root
cause of most historical false positives in this repo.

### 2.1 SyntaxNode — the raw text shape

The parse tree. It exists even for garbage text and knows nothing about meaning.
`new Mock<IFoo>(1)` and `new Moq.Mock<IFoo>(1)` are *different* syntax; a user class
named `Mock` produces *identical* syntax to Moq's. Syntax is:

- Always available, even mid-keystroke (nodes may be incomplete/`IsMissing`).
- The only place with token-level precision — you need it to pick a **diagnostic
  span** or to rewrite code in a fixer.
- Never sufficient alone for a semantic decision (ADR-001,
  `docs/architecture/ADR-001-symbol-based-detection-over-string-matching.md`:
  string/name matching is banned for semantics; a name check is allowed only as a
  cheap pre-filter *before* an authoritative symbol check).

### 2.2 ISymbol — declared identity

What the declaration *is*: `IMethodSymbol`, `INamedTypeSymbol`, `IPropertySymbol`,
`IEventSymbol`, `IFieldSymbol`, `IParameterSymbol`. Symbols answer "is this call
*really* `Moq.Mock<T>.Setup`?" regardless of aliases, `using` directives, or
qualification.

Two non-negotiable mechanics:

**Metadata names carry generic arity with a backtick.** Symbols are looked up by
CLR metadata name, where `` `N `` = number of generic parameters
(`src/Common/WellKnown/MoqKnownSymbols.cs:156,176`):

```csharp
internal INamedTypeSymbol? Mock  => TypeProvider.GetOrCreateTypeByMetadataName("Moq.Mock");
internal INamedTypeSymbol? Mock1 => TypeProvider.GetOrCreateTypeByMetadataName("Moq.Mock`1");
```

`"Moq.Mock"` (the static class) and ``"Moq.Mock`1"`` (the generic `Mock<T>`) are
**different types**. Forgetting the backtick resolves null and silently disables
your check. When you register a new symbol, add a resolves-non-null test — this
repo has "phantom" properties that resolve null because the exact types/members
don't exist in Moq (best-known cases: `IReturns`, ``IReturns`1``; the full
10-entry reflection-verified table is in moq-api-reference §PHANTOM SYMBOLS
doctrine), pinned by `MoqKnownSymbolsTests`.

**Compare symbols with `SymbolEqualityComparer`, never `==` or name strings.**
And compare **original definitions**: `Mock<IFoo>.Setup` is a *constructed* symbol;
the cached lookup holds the *open generic*. The repo's canonical helper
(`src/Common/ISymbolExtensions.cs:34-69`) handles this plus reduced extension
methods:

```csharp
if (symbol is IMethodSymbol method)
{
    if (symbolEqualityComparer.Equals(method.OriginalDefinition, other)) return true;
    // Reduced extension methods (m.Ext() vs Ext(m)) have a different
    // OriginalDefinition than the static form. Check the unreduced form.
    return method.ReducedFrom != null
        && symbolEqualityComparer.Equals(method.ReducedFrom.OriginalDefinition, other);
}
```

Use `symbol.IsInstanceOf(knownSymbols.Mock1Setup)` — do not hand-roll comparisons.
Also: `Compilation.GetTypeByMetadataName` is **banned** (`src/BannedSymbols.txt`);
go through `MoqKnownSymbols`/`KnownSymbols`, which cache per compilation.

### 2.3 IOperation — bound semantics

The semantic tree: what the code *does* after binding. An `IInvocationOperation`
carries `.TargetMethod` (an `IMethodSymbol`), `.Arguments` **in parameter order**
(source order for syntax `ArgumentList.Arguments` is NOT parameter order when named
arguments are used), `.Instance`, resolved conversions, and constant values.

ADR-007 (`docs/architecture/ADR-007-prefer-registeroperationaction-over-registersyntaxnodeaction.md`)
makes `RegisterOperationAction` the default **because syntax variations collapse**:
parenthesized calls, named vs positional arguments, expression vs statement
context, target-typed `new` vs explicit `new Mock<T>()` all produce the same
operation. Every historical Moq1203 false-positive patch (5 rounds: chaining,
parenthesization, delegate overloads, extension-method wrapping) was fighting
syntax variation that `IOperation` absorbs for free. Cautionary counterexample:
`ConstructorArgumentsShouldMatchAnalyzer` still registers
`SyntaxKind.ObjectCreationExpression` only, so `Mock<IFoo> m = new(42);`
(`ImplicitObjectCreationExpression`, a different SyntaxKind) is silently never
analyzed — a known false negative (audit A-7, 2026-07-02).

### 2.4 Decision table

| You need to… | Use | How in this repo |
|---|---|---|
| Decide "is this Moq's X?" | `ISymbol` | `symbol.IsInstanceOf(knownSymbols.…)` via `MoqKnownSymbols` |
| Analyze a call/creation/assignment | `IOperation` | `RegisterOperationAction(cb, OperationKind.Invocation)` etc. |
| Pick the exact squiggle location | `SyntaxNode` | `operation.Syntax` then drill to the token (see §3) |
| Rewrite code (code fix) | `SyntaxNode` | `DocumentEditor`/`SyntaxGenerator` in `src/CodeFixes/` |
| Cheap pre-filter before binding | Syntax name text | Allowed ONLY before an authoritative symbol check (e.g. `SemanticModelExtensions.IsMoqFluentInvocation`) |
| Handle attribute/trivia/declaration shapes with no IOperation form | `SyntaxNode` action | Document in the analyzer why IOperation was insufficient (ADR-007 IMP-002) |
| Get an operation from a syntax node (fixer side) | both | `semanticModel.GetOperation(node, cancellationToken)` |

Bridging: `operation.Syntax` goes down (operation → syntax);
`semanticModel.GetSymbolInfo(node)` / `GetOperation(node)` go up (syntax →
symbol/operation). Always pass the context's `CancellationToken` to semantic-model
calls — per-keystroke analysis is cancelled constantly, and ignoring the token
wastes the user's CPU (several missing-token sites are tracked from the 2026-07-02
audit).

---

## 3. Diagnostic spans and Location

A diagnostic's `Location` is a character-precise `TextSpan` in a file — it is the
squiggle the user sees, and in this repo it is **pinned by tests and
non-negotiable**. Report on the token that best tells the user what to change, not
lazily on the whole statement.

Two real span choices:

- **Moq1200** reports the whole setup invocation — the entire expression is wrong:
  `SetupShouldBeUsedOnlyForOverridableMembersAnalyzer.cs:64` uses
  `invocationOperation.Syntax.CreateDiagnostic(...)`.
- **Moq1300** reports only the offending type argument inside `As<T>()` —
  `AsShouldBeUsedOnlyForInterfaceAnalyzer.cs:89-96` drills from the operation's
  syntax down to the `GenericNameSyntax` named `As` and takes
  `TypeArgumentList.Arguments.FirstOrDefault()?.GetLocation()` (with a fallback to
  the whole invocation). Note the `"As"` string here picks a *location*, not a
  semantic decision — the symbol check already happened. That distinction is what
  keeps it ADR-001-compliant.

**Never call `Diagnostic.Create` directly** — it is banned (`src/BannedSymbols.txt`,
enforced as RS0030). Use the wrapper `src/Common/DiagnosticExtensions.cs`, which
has overloads for `SyntaxNode`, `Location`, and `IOperation` and normalizes
non-source locations to `Location.None`:

```csharp
Diagnostic diagnostic = invocationOperation.Syntax.CreateDiagnostic(Rule, mockedMemberSymbol.ToDisplayString());
context.ReportDiagnostic(diagnostic);
```

### Markup testing pins spans

Tests express expected diagnostics inline with `{|MoqXXXX:...|}` markup
(Microsoft.CodeAnalysis.Testing). The markup asserts **both** the rule ID **and**
that the span covers exactly the enclosed text; any diagnostic anywhere else in the
source fails the test, so unmarked test code is a genuine negative assertion. Real
row from `tests/Moq.Analyzers.Test/SetupShouldBeUsedOnlyForOverridableMembersAnalyzerTests.cs:13`:

```csharp
["""{|Moq1200:new Mock<SampleClass>().Setup(x => x.Property)|};"""],
```

The strongest form (used when message arguments matter) is
`DiagnosticResult.WithSpan(...).WithArguments(...)` — see
`NoMockOfLoggerAnalyzerTests`. Repo rule of engagement: if a span test fails once,
STOP and re-check your span logic; do not "fix" the test to match the code.

---

## 4. Worked example: one real analyzer, every line explained

Read `src/Analyzers/SetupShouldBeUsedOnlyForOverridableMembersAnalyzer.cs`
(101 lines, Moq1200: "Setup should be used only for overridable members"). This is
the house pattern; 20+ analyzers follow the same skeleton.

**Lines 9-10 — declaration.** `[DiagnosticAnalyzer(LanguageNames.CSharp)]` +
`public class ... : DiagnosticAnalyzer`. The attribute is the loader hook (§1).

**Lines 12-24 — the descriptor.** A `DiagnosticDescriptor` is the rule's static
identity card: ID (`DiagnosticIds.SetupOnlyUsedForOverridableMembers` = `"Moq1200"`,
all IDs centralized in `src/Common/DiagnosticIds.cs`), title, message *format
string* (`'{0}' is not overridable` — args supplied at report time), category from
`DiagnosticCategory` (drives release tracking, §7), default severity, and
`helpLinkUri` built from `ThisAssembly.GitCommitId` (Nerdbank.GitVersioning) so the
squiggle links to the docs page for the exact shipped commit:

```csharp
helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.SetupOnlyUsedForOverridableMembers}.md"
```

**Line 27 — `SupportedDiagnostics`.** Roslyn only routes diagnostics whose
descriptors appear here; report an unlisted descriptor and Roslyn throws (→ AD0001).

**Lines 30-36 — `Initialize`.** The mandatory trio:

```csharp
context.EnableConcurrentExecution();
context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
context.RegisterCompilationStartAction(RegisterCompilationStartAction);
```

**Lines 38-50 — compilation start (the ADR-006 pattern).** Runs once per
compilation:

```csharp
MoqKnownSymbols knownSymbols = new(context.Compilation);   // resolve+cache Moq symbols ONCE
if (!knownSymbols.IsMockReferenced())                      // Moq.Mock / Mock`1 / MockRepository all null?
{
    return;                                                // → register NOTHING; near-zero cost for non-Moq projects
}
context.RegisterOperationAction(
    ctx => AnalyzeInvocation(ctx, knownSymbols),           // lambda captures the per-compilation cache
    OperationKind.Invocation);
```

`IsMockReferenced()` (`src/Common/WellKnown/MoqKnownSymbolExtensions.cs:5-8`) is the
early-exit gate every analyzer must have: if the compilation doesn't reference Moq,
the analyzer costs one symbol lookup per compilation and nothing per node. The
lambda-capture of `knownSymbols` is how per-compilation state reaches per-node
callbacks *without any mutable fields* — this is the entire thread-safety story.

**Lines 52-66 — the per-node callback.** Runs concurrently for **every invocation
in the compilation**, so it must reject non-matches as cheaply as possible:

```csharp
if (context.Operation is not IInvocationOperation invocationOperation) return;   // defensive kind check
if (!IsSetupOnNonOverridableMember(invocationOperation, knownSymbols, out ISymbol? mockedMemberSymbol)) return;
Diagnostic diagnostic = invocationOperation.Syntax.CreateDiagnostic(Rule, mockedMemberSymbol.ToDisplayString());
context.ReportDiagnostic(diagnostic);
```

Note the shape: *all* analysis in a `bool Try...` helper; the report path computes
the (allocating) `ToDisplayString()` only after the decision. Keep expensive
formatting off the common no-diagnostic path.

**Lines 75-100 — the decision helper.** Symbol checks all the way down:

```csharp
IMethodSymbol targetMethod = invocationOperation.TargetMethod;
if (!targetMethod.IsMoqSetupMethod(knownSymbols) && !targetMethod.IsMoqEventSetupMethod(knownSymbols))
    return false;                                          // not Moq's Setup/SetupAdd/SetupRemove → bail
ISymbol? candidate = MoqVerificationHelpers.TryGetMockedMemberSymbol(invocationOperation);
if (candidate is null                                      // couldn't extract member (mid-edit, non-lambda arg…)
    || candidate.ContainingType?.TypeKind == TypeKind.Interface   // interface members always mockable
    || candidate.IsOverridableOrAllowedMockMember(knownSymbols))  // virtual/abstract/override-not-sealed etc.
    return false;
mockedMemberSymbol = candidate;
return true;
```

`IsMoqSetupMethod` (`src/Common/ISymbolExtensions.Moq.cs:40-43`) =
`symbol.IsInstanceOf(knownSymbols.Mock1Setup) && symbol is IMethodSymbol { IsGenericMethod: true }`
— identity against the cached open-generic `Mock<T>.Setup`, per §2.2. (The
`IsGenericMethod` requirement means the non-generic void-member `Setup` overload is
never analyzed — a known false negative tracked in issue #1270, open as of
2026-07-02. Semantics have edges; check the issue tracker before "fixing" one.) Extraction of the mocked
member walks the lambda argument via `IOperation`
(`src/Common/MoqVerificationHelpers.cs:58-70`), guarding `Arguments.Length == 0`
first, unwrapping `IDelegateCreationOperation` around the lambda. Overridability is
`ISymbolExtensions.IsOverridable` (`src/Common/ISymbolExtensions.cs:111-120`):
statics never; interface members always; otherwise `!IsSealed && (IsVirtual ||
IsAbstract || (IsOverride && !IsSealed))`.

---

## 5. Malformed-code reality: your input is mid-keystroke

The IDE re-analyzes on every keystroke. Most compilations you see are **broken**:
half-typed identifiers, missing arguments, unresolved types. Roslyn still gives you
trees — full of traps.

What binding gives you on broken code:

| API result | Meaning | Trap |
|---|---|---|
| `GetSymbolInfo(node).Symbol == null` with `CandidateSymbols` non-empty | Binding found candidates but couldn't commit (see `CandidateReason`, e.g. `OverloadResolutionFailure` for `mock.Setup(x => x.M)` with an untyped lambda) | Treating null as "not Moq" loses real Moq calls mid-edit (false negatives) |
| `ITypeSymbol.TypeKind == TypeKind.Error` / `IErrorTypeSymbol` | The type didn't resolve (`mock.As<IServ` mid-keystroke) | Error types satisfy patterns like `TypeKind: not TypeKind.Interface` → flashing false positives |
| Syntax lists shorter than "guaranteed" | `mock.Setup().Callback(...)` parses fine with zero Setup arguments | `Arguments[0]` → `ArgumentOutOfRangeException` → AD0001 |
| `ConstantValue.HasValue == false` | No constant on unresolved expressions | Reading `.Value` unguarded |

Repo-verified defensive patterns:

**Candidate fallback** — `SemanticModelExtensions.FindSetupMethodFromCallbackInvocation`
(`src/Common/SemanticModelExtensions.cs:30-47`) walks a fluent chain and, when
`Symbol` is null, accepts `CandidateSymbols` under an explicit, commented condition:

```csharp
if (resolvedSymbol is null)
{
    if (symbolInfo.CandidateReason == CandidateReason.OverloadResolutionFailure
        && symbolInfo.CandidateSymbols.Any(s => s.IsMoqSetupMethod(knownSymbols)))
    {
        return invocation;   // safe: downstream re-resolves from the lambda body independently
    }
    expression = method.Expression;   // keep walking the chain
    continue;
}
```

Use candidates only with a documented reason for why acting on an uncommitted
symbol is safe.

**Error-type guard** — `ConstructorArgumentsShouldMatchAnalyzer.cs:414`:
`if (mockedClass is IErrorTypeSymbol) return;`. Counterexample in the same
codebase: `AsShouldBeUsedOnlyForInterfaceAnalyzer` lacks this guard, so Moq1300
(Error severity!) flashes on incomplete `As<...>` — live finding A-8 from the
2026-07-02 audit. The guard is the rule; the gap is the bug.

**Index guards** — `MoqVerificationHelpers.TryGetMockedMemberSymbol` checks
`Arguments.Length == 0` before indexing (`MoqVerificationHelpers.cs:60-63`).
Counterexample: `SemanticModelExtensions.cs:60` indexes
`ArgumentList.Arguments[0]` unguarded and is crash-reachable through the Moq1100
code fix on `mock.Setup().Callback(...)` — audit finding A-2 (High), tracked
in the #1241-#1258 issue range.

Checklist before you ship any analyzer/fixer change:

- [ ] Every list index preceded by a `Count`/`Length` check or `is { Arguments.Count: > 0 }` pattern.
- [ ] Every `ITypeSymbol` consumed for a *report* decision guarded against `TypeKind.Error` (and usually `TypeKind.TypeParameter`).
- [ ] Null `Symbol` handled explicitly: bail (usual), or candidate fallback with a comment justifying safety.
- [ ] No null-forgiving `!` on values Roslyn can legitimately return null for (repo-banned after hardening issues #997/#998/#1000/#1027).
- [ ] A test row with deliberately incomplete code (see `CallbackSignatureShouldMatchMockedMethodAnalyzerTests`, which uses `CompilerDiagnostics.None` to allow non-compiling sources). Malformed-code rows are the weakest systematic test gap in this repo (audit Pass C) — add them.

---

## 6. Host-version matrix: compiled against Roslyn 4.8, runs in newer hosts

ADR-003 pins `Microsoft.CodeAnalysis.CSharp` to **4.8** and ADR-002 targets
**netstandard2.0** so the shipped DLL loads in VS 2022 17.8+ and .NET 8 SDK hosts
(violating this caused CS8032 "analyzer failed to load" — incident #850, now
triple-enforced in CI). But 4.8 is only the *API view you compile against*. At
runtime the host supplies **its** Roslyn — often years newer — and newer C# code
flows through your old-API view.

| | Version | Implication |
|---|---|---|
| Compile-time API | Roslyn 4.8 (C# 12 era) | You cannot *name* newer APIs (`IsParamsCollection` doesn't exist in 4.8) |
| Runtime host | Whatever the consumer runs (VS 17.12+, .NET 9/10 SDK…) | Symbols describing C# 13/14 constructs reach your code through 4.8-shaped interfaces |
| Test compiler | Pinned Roslyn 4.8 → parses up to C# 12 (2026-07-02) | You **cannot write C# 13+ constructs in test sources**; newer-language behavior is untestable in-suite and must be reasoned about (or tested out-of-band) |

The cautionary example (audit finding A-1, Critical, issue #1241): C# 13 *params
collections* (`params ReadOnlySpan<T>`, `params IEnumerable<T>`) report
`IParameterSymbol.IsParams == true` on modern hosts — same as classic params
arrays — but their `Type` is **not** `IArrayTypeSymbol`. So this 4.8-era line
(`src/Analyzers/ConstructorArgumentsShouldMatchAnalyzer.cs:383`):

```csharp
ITypeSymbol paramsElementType = ((IArrayTypeSymbol)paramsParameter.Type).ElementType;
```

throws `InvalidCastException` (→ AD0001, analyzer disabled) the moment a consumer
on a Roslyn 4.10+ host (VS 17.10+ / .NET 9+ SDK) mocks a class with a
params-collection constructor. The code was correct when written; the *host*
moved. Rules to generalize:

- Never hard-cast symbol interfaces; pattern-match and bail. And the **bail
  direction matters**: per the analysis in issue #1241, this particular helper must
  return `true` ("assume the arguments match, report nothing") — returning `false`
  would flow into a Moq1002 report and convert the crash into a false positive on
  code the compiler accepts. When you cannot verify, suppress; a false negative on
  an exotic construct is acceptable, a crash or FP is not.
- Treat "the set of things Roslyn can hand me" as open-ended: new `OperationKind`s,
  new `TypeKind`s, new symbol shapes appear in hosts without your recompiling.
  `switch` statements over Roslyn enums need safe defaults, not exhaustive assumptions.
- When a bug report mentions a C# version above 12, suspect a host-newer-than-API
  mismatch first, and remember you cannot reproduce it in the in-repo test suite.

---

## 7. RS-family rules: the analyzers that police this analyzer

This repo's build loads meta-analyzers over its own code
(`build/targets/codeanalysis/CodeAnalysis.props`): `Microsoft.CodeAnalysis.Analyzers`
5.3.0 and `Microsoft.CodeAnalysis.BannedApiAnalyzers` 4.14.0. Under
`/p:PedanticMode=true` (CI parity) their warnings are errors. The ones you will hit:

| Rule | What it demands | Repo mechanics |
|---|---|---|
| RS2000/RS2001 (release tracking) | Every rule ID must have a row in `AnalyzerReleases.Shipped.md` or `AnalyzerReleases.Unshipped.md`, matching the descriptor's category/severity | Files live in `src/Analyzers/`; the NuGet package auto-adds them as AdditionalFiles when they exist next to the csproj (verified in the package .targets). New rule → add a `### New Rules` row to **Unshipped**. Changed severity/category → per `copilot-instructions.md:553` also recorded in **Unshipped** (repo convention: as-if-new row; the live file currently uses a `### Changed Rules` section — see moq-analyzers-rule-lifecycle Part 3 for how to handle the discrepancy). `Changed`/`Removed` sections belong in **Shipped** only at release promotion; NEVER hand-edit **Shipped** otherwise. Missing row **fails the build**. |
| RS2008 | Release tracking must be enabled for any project declaring descriptors | Already satisfied by the two files existing — don't delete them |
| RS1025 "Configure generated code analysis" / RS1026 "Enable concurrent execution" | The mandatory `Initialize` trio of §4 | Copy the house skeleton; all 24 analyzer classes (25 rule IDs) comply |
| RS0030 "Do not use banned APIs" | Nothing in `src/BannedSymbols.txt` | `Diagnostic.Create` → use `DiagnosticExtensions.CreateDiagnostic`; `Compilation.GetTypeByMetadataName` → use `KnownSymbols`/`MoqKnownSymbols`. The single sanctioned suppression is inside the wrapper itself (`DiagnosticExtensions.cs:42`) |

If the build fails with an RS diagnostic you don't recognize, it is almost always
one of these four rows — fix the cause, never suppress (change control:
suppressions need justification and review; see moq-analyzers-change-control).

---

## 8. Code-fix model basics

A **code fix** is the lightbulb action attached to a diagnostic. It lives in a
separate assembly (`src/CodeFixes/Moq.CodeFixes.csproj`) because fixers reference
Workspaces APIs that the command-line compiler doesn't load. Anatomy, from
`src/CodeFixes/VerifyOverridableMembersFixer.cs`:

```csharp
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(VerifyOverridableMembersFixer))]  // MEF export = loader hook (again: never dead code)
[Shared]
public sealed class VerifyOverridableMembersFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticIds.VerifyOnlyUsedForOverridableMembers);   // which MoqXXXX it handles
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;  // enables Fix All in document/project/solution
    public override async Task RegisterCodeFixesAsync(CodeFixContext context) { ... }
}
```

`RegisterCodeFixesAsync` re-derives everything from the diagnostic's span:
`root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true)`, then
`semanticModel.GetOperation(node)` to get back to `IOperation` land, then registers
a `CodeAction.Create(title, createChangedDocumentOrSolution, equivalenceKey)`.

**Equivalence key**: the third `CodeAction.Create` argument. Roslyn uses it to
group "the same fix" across many diagnostics for Fix All — every registration of
the same logical fix must pass the same stable string. House style: `nameof(TheFixer)`
(`VerifyOverridableMembersFixer.cs:73`) or a shared title constant
(`CallbackSignatureShouldMatchMockedMethodFixer.cs:53,71`). A missing/unstable key
breaks batch fixing silently.

Fixer-specific hazards (the analyzer cannot shield you):

- Fixers run **on user gesture** against whatever stale/broken code sits under the
  diagnostic. Re-validate every assumption; return the original document/solution
  when preconditions fail (see the `modifiers.IsVirtual` no-op bail at
  `VerifyOverridableMembersFixer.cs:102-105`). Audit finding A-2 (crash via the
  Moq1100 fixer) exists precisely because a helper trusted analyzer-side guarantees.
- Fixed code must **compile**. Emitting `static virtual` or `virtual` in a sealed
  class is a fixer bug even though the transform "succeeded" (audit A-11).
- Rewrites go through `SyntaxGenerator`/`DocumentEditor` (language-agnostic,
  trivia-preserving), not string surgery.

**Test pattern (mandatory house style)**: `[Theory]` + `[MemberData]` backed by
`public static IEnumerable<object[]>`, each row = (before-source-with-markup,
expected-after-source). From `tests/Moq.Analyzers.Test/CallbackSignatureShouldMatchMockedMethodCodeFixTests.cs:39-41`:

```csharp
[
    """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<string>())).Callback(({|Moq1100:int i|}) => { });""",
    """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<string>())).Callback((string i) => { });""",
],
```

Rows where before == after assert the fixer leaves correct code alone. The
`CodeFixVerifier<TAnalyzer, TFixer>` helper (in `tests/Moq.Analyzers.Test/Helpers/`)
runs analyzer + fixer + re-analysis; `Test.cs` injects global usings including
`Moq` into every test source.

---

## When NOT to use this skill

| If you need… | Load instead |
|---|---|
| Moq's own API semantics (Setup/Returns/Raise overloads, 4.8.2 vs 4.18.4 differences, It vs ItExpr) | **moq-api-reference** |
| The step-by-step checklist for adding/changing/shipping a rule (DiagnosticIds, docs, release notes, benchmarks) | **moq-analyzers-rule-lifecycle** |
| Building, running tests, PedanticMode, environment/SDK setup | **moq-analyzers-build-and-env** |
| Triage of a live crash/FP/CI failure | **moq-analyzers-debugging-playbook** |
| Why past decisions are settled (string-vs-symbol war, Moq1203 saga, CS8032) | **moq-analyzers-failure-archaeology** |
| The ADR contracts themselves and enforcement | **moq-analyzers-architecture-contract** |
| Test-quality/validation standards, coverage, spans discipline in depth | **moq-analyzers-validation-and-qa** |
| .editorconfig/severity/flags configuration surface | **moq-analyzers-config-and-flags** |
| BCL/API-design (Framework Design Guidelines) standards | **dotnet-api-design-standards** |
| PR/change-control process | **moq-analyzers-change-control** |
| The FP-convergence campaign backlog (#1241-#1278) | **moq-analyzers-fp-convergence-campaign** |

Other siblings for specialized work: moq-analyzers-diagnostics-and-tooling,
moq-analyzers-docs-and-writing, moq-analyzers-proof-toolkit,
moq-analyzers-research-frontier, moq-analyzers-research-methodology.

## Provenance and maintenance

- Walkthrough file unchanged? `git log --oneline -1 -- src/Analyzers/SetupShouldBeUsedOnlyForOverridableMembersAnalyzer.cs` (line refs valid at 101 lines: `wc -l` it).
- Roslyn pin still 4.8? `grep 'Microsoft.CodeAnalysis.CSharp"' Directory.Packages.props`
- Meta-analyzer versions (5.3.0 / 4.14.0)? `grep -r PackageVersion build/targets/codeanalysis/Packages.props`
- Banned APIs list? `cat src/BannedSymbols.txt`
- Packaging path lines (csproj:43-45)? `grep -n 'analyzers/dotnet/cs' src/Analyzers/Moq.Analyzers.csproj`
- Params-collection crash still unfixed at `ConstructorArgumentsShouldMatchAnalyzer.cs:383`? `grep -n 'IArrayTypeSymbol' src/Analyzers/ConstructorArgumentsShouldMatchAnalyzer.cs` and check issue #1241 state (`gh issue view 1241`).
- Unguarded `Arguments[0]` still at `SemanticModelExtensions.cs:60`? `grep -n 'Arguments\[0\]' src/Common/SemanticModelExtensions.cs`
- Moq1300 error-type guard still missing? `grep -n 'TypeKind' src/Analyzers/AsShouldBeUsedOnlyForInterfaceAnalyzer.cs`
- All analyzers still compliant with the Initialize trio? `grep -L 'EnableConcurrentExecution' src/Analyzers/*Analyzer*.cs` — expected output today is exactly `SetExplicitMockBehaviorAnalyzer.cs` and `SetStrictMockBehaviorAnalyzer.cs`, which inherit the trio from `MockBehaviorDiagnosticAnalyzerBase`; anything else is a violation.
- Test-compiler C# ceiling (currently C# 12 via Roslyn 4.8) and test count (3,357 in Moq.Analyzers.Test): re-run `dotnet test --settings ./build/targets/tests/test.runsettings` and check LangVersion behavior notes in moq-analyzers-build-and-env.
- AnalyzerReleases auto-include mechanism: `grep -n 'AnalyzerReleases' ~/.nuget/packages/microsoft.codeanalysis.analyzers/*/buildTransitive/*.targets`
- Rule count/ID table: `cat src/Common/DiagnosticIds.cs` (25 rules, Moq1000-Moq1600, Moq1209 reserved).

Last verified: 2026-07-02 against commit 05135b2.
