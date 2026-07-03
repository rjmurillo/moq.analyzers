---
name: moq-analyzers-architecture-contract
description: Explains the load-bearing design decisions of moq.analyzers — all 10 ADRs as a decision/enforcement table, the non-negotiable invariants (Initialize pattern, MoqKnownSymbols lifecycle, symbol-based detection, netstandard2.0 + Roslyn 4.8 API ceiling, crash-safety ordering), the component map (25 rules, Common helpers, PerfDiff), and the plainly-stated known-weak points with their tracking issues. Load this BEFORE designing any change to src/ — new rule, helper refactor, dependency bump, detection-strategy change — or when asking "why is it built this way?", "what breaks if I...?", "which ADR covers...?", or "is this a known bug?". Do NOT load for step-by-step build/test commands (moq-analyzers-build-and-env), debugging a failing test or crash (moq-analyzers-debugging-playbook), Roslyn API how-tos (roslyn-analyzer-reference), Moq API semantics (moq-api-reference), or the PR/release process (moq-analyzers-change-control).
---

# moq.analyzers Architecture Contract

This skill is the "why it holds" document. It distills the 10 Architecture Decision
Records (ADRs — short documents in `docs/architecture/` recording a decision, its
context, and consequences), the invariants every analyzer must satisfy, the component
map, and the known-weak points as of 2026-07-02.

**Context for zero-context readers.** This repo ships a *Roslyn analyzer*: a plugin
DLL that the C# compiler and IDEs load and run against user code on every build and
every keystroke, reporting *diagnostics* (warnings like `Moq1002` with a precise
source location, called a *span*). Because the code executes inside other people's
compilers — including mission-critical codebases — the failure hierarchy is:

1. **An analyzer crash is the worst outcome.** A thrown exception surfaces as
   compiler warning `AD0001` and Roslyn disables the analyzer for the session.
   Verbatim rule from `.github/copilot-instructions.md` (line 38): *"An analyzer
   crash is worse than a missed diagnostic."*
2. **A false positive (FP — flagging correct code) is next worst.** It trains users
   to suppress or uninstall the analyzer.
3. **A false negative (FN — missing real misuse) is bad but tolerable** as a
   conservative trade when the alternative is 1 or 2.
4. **Per-keystroke performance is a measured contract** (ADR-008 perf gate).
5. **Thread safety is assumed by Roslyn** — callbacks run concurrently.

Every decision below exists to serve that ordering. When two rules conflict,
resolve in that order (e.g., bail out silently rather than guess; #1241's fix
returns "matches" rather than risk a crash *or* an FP).

## ADR decision table

All ADRs live in `docs/architecture/ADR-00X-*.md`, status Accepted (2026-07-02).
"Enforcement point" = the mechanism that catches a violation; "convention" means
only code review catches it.

| ADR | Decision | Why | What breaks if violated | Enforcement point |
|---|---|---|---|---|
| 001 | Symbol-based detection only. Type/method identity via `ISymbol` + `SymbolEqualityComparer.Default` + `MoqKnownSymbols`; never string names for semantic decisions | A user type named `Mock`, aliased usings, or fully-qualified names defeat string matching → FPs/FNs | FP/FN whack-a-mole (the Moq1203 saga took 5 patches); doppelganger types trigger falsely | `src/BannedSymbols.txt` bans `Compilation.GetTypeByMetadataName` ("Use KnownSymbols"); `DoppelgangerTestHelper` test suites; code review |
| 002 | Analyzer + CodeFix assemblies target `netstandard2.0` (a TFM — target framework moniker — loadable by every host) | The DLL must load in Visual Studio, `dotnet build`, Rider — each with its own runtime | Assembly fails to load in some hosts; user sees no diagnostics and no clear error | `<TargetFramework>` in `src/*/*.csproj`; 9-way `analyzer-load-test` CI matrix (`.github/workflows/main.yml:283`) |
| 003 | Pin Roslyn SDK (`Microsoft.CodeAnalysis.CSharp[.Workspaces]`) to **4.8** | 4.8 = VS 2022 17.8 (Nov 2023), the broadest deployed baseline; newer pins exclude users on older IDEs | Compiling against newer APIs silently raises the minimum host; APIs like `IsParamsCollection` don't exist in 4.8 (see #1241) | `Directory.Packages.props:22` (`Version="4.8"`); `renovate.json` ignores `Microsoft.CodeAnalysis.*` |
| 004 | Cap `Microsoft.CodeAnalysis.AnalyzerUtilities` at **3.3.4** (< 4.14) | 4.14+ depends on `System.Collections.Immutable 9.0.0.0`, absent in .NET 8 SDK hosts → analyzer fails to instantiate | **CS8032** ("analyzer cannot be created") for every .NET 8 SDK user — the exact incident #850, fixed by #888, forced release v0.4.1 | `Directory.Packages.props:32` + explanatory comment; `ValidateAnalyzerHostCompatibility` MSBuild target (`build/targets/packaging/Packaging.targets:20`); CI DLL-reference check in the build job; `analyzer-load-test` matrix greps for CS8032 (`main.yml:412,438`) |
| 005 | NuGet Central Package Management with `CentralPackageTransitivePinningEnabled=true` | Any transitive dependency drifting above what hosts provide causes silent load failures | Transitive upgrade sneaks a v9 assembly into the nupkg → CS8032 class of failure returns | `Directory.Packages.props:4`; csproj files carry no `Version=` attributes; same host-compat target as ADR-004 |
| 006 | WellKnown-types pattern: `MoqKnownSymbols` (in `src/Common/WellKnown/`) resolves every Moq symbol **once per `CompilationStartAnalysisContext`**; analyzers receive the instance as a parameter | `GetTypeByMetadataName` per-operation = thousands of redundant lookups per keystroke | IDE lag; per-keystroke perf gate regressions | `src/BannedSymbols.txt` (GetTypeByMetadataName ban); `MoqKnownSymbolsTests.Caching.cs`; audit 2026-07-02 verified all analyzers comply |
| 007 | Prefer `RegisterOperationAction` (semantic `IOperation` tree) over `RegisterSyntaxNodeAction` (raw syntax) | `IOperation` abstracts syntax variations (named args, parenthesization, target-typed `new`) — one callback covers all shapes | Syntax-kind registration misses variants: `Mock<IFoo> m = new(42);` is invisible to Moq1001/1002 today (audit A-7) because `ImplicitObjectCreationExpression` is a different `SyntaxKind` | **Convention only** — not mechanically enforced; several analyzers still use syntax actions (audit A-12/L4). Treat as the default for new code |
| 008 | BenchmarkDotNet benchmarks + custom PerfDiff CLI gate performance regressions; baseline in `build/perf/baseline.json` | Analyzers run per keystroke; reviewers cannot see a milliseconds regression in a diff | Unmeasured regressions accumulate as editor lag | `perf` CI job (`main.yml:456`, required check) → `build/scripts/perf/ComparePerfResults.ps1` → PerfDiff `--failOnRegression`. **Gate has known verdict defects — see weak points #1265–#1269** |
| 009 | xUnit + `Microsoft.CodeAnalysis.Testing` verifiers; inline sources with `{\|Moq1002:...\|}` markup asserting diagnostic ID **and exact span** | The markup makes expected output visible at the test site; unmarked code is a genuine negative assertion | Weaker harnesses can't catch span drift or unexpected extra diagnostics | Test helpers in `tests/Moq.Analyzers.Test/Helpers/`; every analyzer suite uses them |
| 010 | `*.ps1`/`*.psm1`/`*.psd1` get `eol=lf` in `.gitattributes` (global rule, not path-scoped) | CRLF corrupts PowerShell block comments when hooks run from Git Bash — incident #1081 blocked all pushes | Pre-push hook parse errors; contributors bypass hooks with `--no-verify` | `.gitattributes`; repo targets pwsh 7+ only |

## Invariants (with code anchors)

Verified against source at commit 05135b2 (2026-07-02). Each is a MUST for any
new or modified analyzer.

### 1. The Initialize triple + IsMockReferenced early exit

Every analyzer's `Initialize` enables concurrent execution, skips generated code,
and its compilation-start callback bails out when Moq isn't referenced:

```csharp
// Canonical form — src/Analyzers/SetupShouldBeUsedOnlyForOverridableMembersAnalyzer.cs:30-45
public override void Initialize(AnalysisContext context)
{
    context.EnableConcurrentExecution();
    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.RegisterCompilationStartAction(RegisterCompilationStartAction);
}

private static void RegisterCompilationStartAction(CompilationStartAnalysisContext context)
{
    MoqKnownSymbols knownSymbols = new(context.Compilation);
    if (!knownSymbols.IsMockReferenced()) { return; }
    // register operation/syntax actions, passing knownSymbols through
}
```

- `IsMockReferenced()` is `src/Common/WellKnown/MoqKnownSymbolExtensions.cs:5-8`:
  true iff `Mock`, `Mock<T>`, or `MockRepository` resolves. This is the cheap gate
  that makes the analyzers free in non-Moq projects.
- Grep caveat: `SetExplicitMockBehaviorAnalyzer` and `SetStrictMockBehaviorAnalyzer`
  inherit the triple from `MockBehaviorDiagnosticAnalyzerBase` (lines 17-18, 171),
  so a naive grep finds 23 files, not 24 — that is compliant, not a violation.

### 2. MoqKnownSymbols exactly once per compilation start (ADR-006)

- Construct `new MoqKnownSymbols(context.Compilation)` **only** inside
  `RegisterCompilationStartAction`; pass it into every callback.
- Never call `GetTypeByMetadataName` in a callback (banned, `src/BannedSymbols.txt`).
- Internally it caches via `Lazy<>(LazyThreadSafetyMode.ExecutionAndPublication)`
  and AnalyzerUtilities' `WellKnownTypeProvider` (per-compilation ConcurrentDictionary).
  Beware: `ExecutionAndPublication` **caches exceptions** — see weak point #1250.
- When adding a symbol property, add a resolves-non-null test — the registry
  already contains phantom entries (see weak points).

### 3. No mutable state reachable from callbacks

Roslyn invokes callbacks concurrently (`EnableConcurrentExecution`). All statics in
`src/` are readonly descriptors/strings; the 2026-07-02 audit verified zero mutable
instance/static state reachable from any registered callback. Keep it that way:
per-compilation state lives in objects created at compilation start (like
`MoqKnownSymbols`), never in fields of the analyzer class.

### 4. SymbolEqualityComparer always; symbol checks are authoritative

- Compare symbols with `SymbolEqualityComparer.Default`, never `==` on `ISymbol`
  and never on names.
- A string-name **fast path** is allowed *only* as a cheap pre-filter *before* an
  authoritative symbol check (sanctioned pattern:
  `SemanticModelExtensions.IsMoqFluentInvocation`). A name check that *decides*
  is an ADR-001 violation (live example: audit A-6 in
  `MethodSetupShouldSpecifyReturnValueAnalyzer.cs` — the fallback fires even when
  resolution succeeded).
- Warning: enum constant comparison via boxed `ConstantValue.Value == ...` is
  reference equality of boxes and only works by accident (audit A-10,
  `SetExplicitMockBehaviorAnalyzer.cs:48`). Use `Equals(...)` with a
  `HasValue` check.

### 5. No null-forgiving operator (`!`) on uncertain values

Project doctrine from the null-hardening incident cluster (#997/#998/#1000/#1027);
enforced by code review plus `<Nullable>enable</Nullable>`
(`build/targets/compiler/Compiler.props:6`) with warnings-as-errors under
`PedanticMode` (`build/targets/codeanalysis/CodeAnalysis.targets:3-5`, CI-default).
`!` is acceptable only where a local guard makes it provably non-null; prefer
`[NotNullWhen(true)]` on Try-pattern out-params so no suppression is needed.

### 6. Diagnostic spans are character-precise, non-negotiable

From `.github/copilot-instructions.md:510-513` (verbatim policy): all spans MUST be
character-precise; a span test failure is a CRITICAL failure — **stop after the
first failure**, re-evaluate the syntax-tree navigation; after a second failure,
escalate to a human. Report on the specific token (which token varies by rule and
is pinned by the `{|MoqXXXX:...|}` test markup), not lazily on the whole invocation.

### 7. API ceiling: netstandard2.0 + Roslyn 4.8, but hosts run newer Roslyn

The most misunderstood invariant. Two directions:

- **Compile-time ceiling:** you may only *call* Roslyn 4.8 APIs
  (`Directory.Packages.props:22`). `IParameterSymbol.IsParamsCollection`,
  C# 13 syntax kinds, newer `OperationKind`s — unavailable.
- **Runtime floor is unbounded above:** the DLL executes inside whatever Roslyn the
  consumer's IDE/SDK hosts (VS 17.12+, .NET 9/10 SDK). Newer hosts feed your 4.8-era
  code *symbols for language features 4.8 never knew* — e.g. `IsParams == true` for
  a C# 13 params collection whose `Type` is **not** `IArrayTypeSymbol` (crash #1241).
  Defensive pattern-match every cast; never assume the input language version.
- Host-compat floor on dependencies: shipped assemblies must load in a **.NET 8
  SDK host** — hence ADR-004's cap and `System.Collections.Immutable`/
  `System.Reflection.Metadata` ≤ 8.0.0 (`Directory.Packages.props:52`). Violation
  = CS8032 (incident #850), triple-enforced (MSBuild target, CI DLL check,
  load-test matrix).
- Test-suite consequence: the pinned 4.8 test compiler parses C# 12 max — C# 13
  constructs (params collections) **cannot be written in test sources**, so some
  crash guards are scenario-untestable in-suite (documented honestly in #1241).

### 8. Crash-safety ordering in code

- Zero `catch` blocks exist in `src/Analyzers`, `src/CodeFixes`, `src/Common`
  (audit-verified) — exceptions propagate as AD0001 by design; do not add
  swallowing catches. Crash-safety comes from *guards*, not catches.
- When a guard cannot verify something, bail toward **no diagnostic** (silence),
  never toward a guess.
- Code-fix providers must tolerate stale diagnostics (the document may have changed
  since the diagnostic was computed) — the fixer path is where #1242 crashes.
- `Diagnostic.Create` is banned (`src/BannedSymbols.txt`); use
  `DiagnosticExtensions.CreateDiagnostic` from `src/Common/DiagnosticExtensions.cs`.

## Component map

### src/Analyzers — 24 analyzer classes, 25 rule IDs (2026-07-02)

IDs are declared in `src/Common/DiagnosticIds.cs`. **Moq1209 is intentionally
reserved and unassigned** (comment in that file). `ConstructorArgumentsShouldMatchAnalyzer`
emits two IDs, which is why classes (24) ≠ rules (25). ID ranges:
1000–1099 Usage, 1100–1199 Correctness, 1200s Correctness, 1300s Usage,
1400–1599 Best Practice, 1600+ Usage/protected (range table:
`docs/rules/README.md:43`; per-rule docs: `docs/rules/Moq{Id}.md`, 25 files).

| ID | Analyzer file (src/Analyzers/) | Checks |
|---|---|---|
| Moq1000 | NoSealedClassMocksAnalyzer.cs | No `Mock<T>` of sealed classes |
| Moq1001 | ConstructorArgumentsShouldMatchAnalyzer.cs | Interface mocks take no ctor args |
| Moq1002 | ConstructorArgumentsShouldMatchAnalyzer.cs | Class-mock ctor args match a real constructor |
| Moq1003 | InternalTypeMustHaveInternalsVisibleToAnalyzer.cs | Internal mocked types need IVT to DynamicProxyGenAssembly2 |
| Moq1004 | NoMockOfLoggerAnalyzer.cs | Don't mock `ILogger` |
| Moq1100 | CallbackSignatureShouldMatchMockedMethodAnalyzer.cs | `.Callback` lambda matches mocked signature |
| Moq1101 | NoMethodsInPropertySetupAnalyzer.cs | `SetupGet/Set` not used on methods |
| Moq1200 | SetupShouldBeUsedOnlyForOverridableMembersAnalyzer.cs | Setup targets overridable members |
| Moq1201 | SetupShouldNotIncludeAsyncResultAnalyzer.cs | No `.Result` in async Setup |
| Moq1202 | RaiseEventArgumentsShouldMatchEventSignatureAnalyzer.cs | `Raise` args match event delegate |
| Moq1203 | MethodSetupShouldSpecifyReturnValueAnalyzer.cs | Non-void setups specify a return value |
| Moq1204 | RaisesEventArgumentsShouldMatchEventSignatureAnalyzer.cs | `.Raises` args match event delegate |
| Moq1205 | EventSetupHandlerShouldMatchEventTypeAnalyzer.cs | `SetupAdd/Remove` handler type matches |
| Moq1206 | ReturnsAsyncShouldBeUsedForAsyncMethodsAnalyzer.cs | Async methods use `ReturnsAsync` |
| Moq1207 | SetupSequenceShouldBeUsedOnlyForOverridableMembersAnalyzer.cs | SetupSequence targets overridable members |
| Moq1208 | ReturnsDelegateShouldReturnTaskAnalyzer.cs | Delegate passed to `Returns` on async method returns Task |
| Moq1210 | VerifyShouldBeUsedOnlyForOverridableMembersAnalyzer.cs | Verify targets overridable members |
| Moq1300 | AsShouldBeUsedOnlyForInterfaceAnalyzer.cs | `.As<T>` only with interfaces |
| Moq1301 | MockGetShouldNotTakeLiteralsAnalyzer.cs | `Mock.Get` not called on literals |
| Moq1302 | LinqToMocksExpressionShouldBeValidAnalyzer.cs | `Mock.Of<T>` expression validity |
| Moq1400 | SetExplicitMockBehaviorAnalyzer.cs | Set an explicit `MockBehavior` |
| Moq1410 | SetStrictMockBehaviorAnalyzer.cs | Prefer `MockBehavior.Strict` |
| Moq1420 | RedundantTimesSpecificationAnalyzer.cs | Redundant `Times` args in Verify |
| Moq1500 | MockRepositoryVerifyAnalyzer.cs | `MockRepository.Verify()` is called |
| Moq1600 | ProtectedSetupShouldUseItExprAnalyzer.cs | Protected setups use `ItExpr`, not `It` |

Shared base in the same folder: `MockBehaviorDiagnosticAnalyzerBase.cs`
(Moq1400/Moq1410). Release tracking: `AnalyzerReleases.Shipped.md` /
`AnalyzerReleases.Unshipped.md` (RS2000-family analyzers fail the build if a new
rule isn't listed in Unshipped; never edit Shipped outside release promotion).

### src/CodeFixes — Moq.CodeFixes.csproj (netstandard2.0)

Fixers: `CallbackSignatureShouldMatchMockedMethodFixer` (Moq1100),
`ReturnsDelegateShouldReturnTaskFixer` (Moq1208), `SetExplicitMockBehaviorFixer` /
`SetStrictMockBehaviorFixer` (Moq1400/1410, funneling into shared
`SetExplicitMockBehaviorCodeAction`), `VerifyOverridableMembersFixer` (Moq1210).
Helpers: `CodeFixContextExtensions`, `SyntaxGeneratorExtensions`, `BehaviorType`.

### src/Common — shared helpers, compiled into both assemblies via Common.projitems

- **`WellKnown/` is the single symbol registry**: `KnownSymbols.cs` (BCL types),
  `MoqKnownSymbols.cs` (every Moq type/method group, ~640 lines),
  `MoqKnownSymbolExtensions.cs` (`IsMockReferenced`). All Moq symbol resolution
  goes through here — nowhere else.
- Detection helpers: `ISymbolExtensions.Moq.cs` (`IsMoqSetupMethod` etc.),
  `MockDetectionHelpers`, `MoqVerificationHelpers`, `SemanticModelExtensions`,
  `EventSyntaxExtensions` (shared by Moq1202/1204 — see weak point #1248).
- Infrastructure: `DiagnosticExtensions.CreateDiagnostic` (the only sanctioned way
  to create diagnostics), `DiagnosticCategory`, `DiagnosticIds`,
  `FilteredArgumentList` (zero-allocation argument filtering),
  `EnumerableExtensions.DefaultIfNotSingle` (crash-safe "exactly one" resolution).

### src/tools/PerfDiff — net8.0 CLI, the ADR-008 gate

`Program.cs`/`DiffCommand.cs` (System.CommandLine 2.0.3, pinned),
`PerfDiff.cs` (verdict orchestration), `BDN/` (BenchmarkDotNet JSON ingestion +
`Regression/` strategies), `ETL/` (trace overweight report). Invoked by
`build/scripts/perf/ComparePerfResults.ps1` with `--failOnRegression`.
Its verdict logic has multiple confirmed defects — see weak points.

### tests/

- `tests/Moq.Analyzers.Test` — 3,357 tests (2026-07-02). Helpers in `Helpers/`:
  `Test.cs`, `AnalyzerVerifier`/`CodeFixVerifier`, `ReferenceAssemblyCatalog`
  (keys: `Net80` = no Moq, `Net80WithOldMoq` = 4.8.2, `Net80WithNewMoq` = 4.18.4),
  `TestDataExtensions` (`WithNamespaces() × With*MoqReferenceAssemblyGroups()`
  row fan-out), `AllAnalyzersVerifier` (reflection-discovers analyzers **only in
  namespace `Moq.Analyzers`** — a wrong namespace silently drops an analyzer from
  no-diagnostic suites), `DoppelgangerTestHelper`. PackageTests snapshot the nupkg
  via Verify.Xunit.
- `tests/Moq.Analyzers.Benchmarks` — per-rule `Moq{Id}Benchmarks.cs` convention.
- `tests/PerfDiff.Tests` — 4 tests (2026-07-02), logging-registration only; it does
  not yet reference PerfDiff itself (planned by #1265–#1269).

## Known-weak points (2026-07-02 audit — stated plainly)

These are confirmed, filed defects. Do not rediscover them; do not silently
work around them; reference the issue.

| Weak point | Severity | Where | Tracking |
|---|---|---|---|
| **Params-collection crash**: unguarded `(IArrayTypeSymbol)paramsParameter.Type` throws `InvalidCastException` (→ AD0001) on C# 13 `params ReadOnlySpan<T>` ctors in modern hosts | Critical | `src/Analyzers/ConstructorArgumentsShouldMatchAnalyzer.cs:383` | [#1241](https://github.com/rjmurillo/moq.analyzers/issues/1241) (open, full fix plan in issue) |
| **Unguarded `Arguments[0]`**: Moq1100 lightbulb on mid-edit `mock.Setup().Callback(...)` throws `ArgumentOutOfRangeException` during fix computation (analyzer path is shielded; fixer path is not) | High | `src/Common/SemanticModelExtensions.cs:60` via `src/CodeFixes/CallbackSignatureShouldMatchMockedMethodFixer.cs` | [#1242](https://github.com/rjmurillo/moq.analyzers/issues/1242) |
| **Event-delegate extraction FPs**: canonical `mock.Raise(m => m.Closed += null, EventArgs.Empty)` on non-generic `EventHandler` is flagged Moq1202/1204 ("too few arguments" — Moq supplies the sender); unresolved delegate types are conflated with zero-parameter delegates → FPs on mid-edit code | High | `src/Common/EventSyntaxExtensions.cs` (`GetEventParameterTypes`) | [#1248](https://github.com/rjmurillo/moq.analyzers/issues/1248) |
| **Lazy poisoning**: `SingleOrDefault()` inside `CreateLazySingleField` — duplicate source-defined `MockBehavior` members (mid-edit CS0102) throw, and `ExecutionAndPublication` caches the exception for the compilation lifetime → persistent AD0001 | Medium | `src/Common/WellKnown/MoqKnownSymbols.cs:643` | [#1250](https://github.com/rjmurillo/moq.analyzers/issues/1250) |
| **PerfDiff verdict defects** (5): ETL comparison never sets its regression out-param, so ETL presence vetoes any real BDN regression to "noise"/exit 0 (#1265); mismatched/empty benchmark sets silently intersected — a broken harness passes green (#1266); absolute-budget strategies never consult the baseline — an unchanged over-budget benchmark permanently blocks CI (#1267); infinite median ratios (the worst regressions) filtered out of the verdict (#1268); empty/single-sample sets, duplicate FullNames, zero-Operations, null-deserialized files crash or distort (#1269) | High (gate integrity) | `src/tools/PerfDiff/ETL/EtlDiffer.cs`, `BDN/BenchmarkDotNetDiffer.cs`, `BDN/Regression/*.cs` | [#1265](https://github.com/rjmurillo/moq.analyzers/issues/1265)–[#1269](https://github.com/rjmurillo/moq.analyzers/issues/1269) |
| **void-Setup FN**: `IsMoqSetupMethod` requires `IsGenericMethod: true` (`src/Common/ISymbolExtensions.Moq.cs:42`), so the non-generic `Setup(Expression<Action<T>>)` overload (void members) is never analyzed — Moq1100/Moq1200-family miss all void-member setups | Known FN (by design until revisited) | `src/Common/ISymbolExtensions.Moq.cs:40-43` | [#1270](https://github.com/rjmurillo/moq.analyzers/issues/1270) pins current behavior with commented data rows; changing it is a separate enhancement |
| **Phantom symbols in the registry**: `MoqKnownSymbols.IReturns` (line 293) and `IReturns1` (line 298) point at `Moq.Language.IReturns`/``IReturns`1``, which **do not exist in any Moq version** — they always resolve null. Real Returns methods live on ``IReturns`2``. These two are only the best-known cases of a 10-entry phantom set — full reflection-verified table in moq-api-reference §PHANTOM SYMBOLS doctrine | Trap, not a bug (dependent method groups are empty, harmless) | `src/Common/WellKnown/MoqKnownSymbols.cs:293,298`; pinned by `tests/Moq.Analyzers.Test/Common/MoqKnownSymbolsTests.ReturnsAndThrows.cs` (`..._ReturnsNull`) | No issue; lesson: **every new KnownSymbols property needs a resolves-non-null test** |
| **SquiggleCop installed but unwired**: pinned local tool v1.0.26 in `.config/dotnet-tools.json` but zero invocations anywhere (no baseline YAML, no CI step, no MSBuild hook) — diagnostic-severity drift is unmanaged | Trap (looks enforced, isn't) | `.config/dotnet-tools.json` | No tracking issue found (UNVERIFIED whether one exists); verified unwired by repo-wide grep 2026-07-02 |
| **Moq1500 limitation**: `repo.VerifyAll()` does not suppress Moq1500 (only `Verify()` counts); repository passed to a helper that verifies also FPs | Known limitation | `src/Analyzers/MockRepositoryVerifyAnalyzer.cs` | [#986](https://github.com/rjmurillo/moq.analyzers/issues/986), pinned by `TODO(#986)` test comments |

Full audit context: 54 findings (1 Critical, 6 High, 16 Medium, 17 Low, 14 Info)
filed as issues **#1241–#1278** on 2026-07-02, each with an implementation-ready
plan (#1244/#1246/#1247/#1249/#1252/#1254 closed as duplicates). Before fixing
anything in `src/`, check whether an issue in that range already owns it — the
issues contain verified fix plans and coordinate with sibling fixes.

## Design lessons baked into the architecture (do not re-fight)

- **Syntactic wrappers are a mandatory test axis.** The Moq1203 FP saga took five
  patches (#886→#1086) because parentheses, fluent chaining, delegate overloads,
  and extension-method wrapping were each discovered separately. Any invocation
  detector must be tested against all wrapper shapes.
- **Fallback removal needs symbol-coverage proof.** The string→symbol migration
  (#245→#1030) includes a documented failed attempt (commit 5172cf3) before the
  successful one (35d363d, which had to add `IRaise`1` to the registry first).
  Removing a fallback without proving the symbol registry covers every case
  reintroduces FNs.
- **Operation-walker discipline** (from the Moq1302 LINQ-to-Mocks FPs, #1010):
  register by `OperationKind` → guard `operation.Instance` null (static access =
  value expression, skip) → then validate the member. RHS of comparisons are
  value expressions.
- **Two analyzers, one helper**: Moq1202/Moq1204 are deliberately separate rule
  IDs but share `EventSyntaxExtensions` — fix bugs in the helper once, test
  through both rules.

## When NOT to use this skill

| Task | Use instead |
|---|---|
| Build, test, format, hook commands; SDK/env setup | moq-analyzers-build-and-env |
| Debugging a failing test, span mismatch, or AD0001 | moq-analyzers-debugging-playbook |
| Full incident history and dead-branch archaeology | moq-analyzers-failure-archaeology |
| Roslyn API usage (IOperation, symbols, registration) how-tos | roslyn-analyzer-reference |
| Moq API shapes, overloads, version differences | moq-api-reference |
| Adding/shipping a rule end-to-end (checklist) | moq-analyzers-rule-lifecycle |
| PR evidence requirements, branch/commit rules, releases | moq-analyzers-change-control |
| .editorconfig/severity/flags configuration | moq-analyzers-config-and-flags |
| Test-quality standards, coverage, validation strategy | moq-analyzers-validation-and-qa |
| BCL/API design standards for public surface | dotnet-api-design-standards |
| The FP-convergence campaign plan (#1241–#1278 execution) | moq-analyzers-fp-convergence-campaign |
| Proving zero-FP claims, corpora, mutation testing | moq-analyzers-proof-toolkit |
| Docs authoring (rule docs, README tables) | moq-analyzers-docs-and-writing |
| Diagnosing tooling (binlogs, SARIF, dotnet-inspect) | moq-analyzers-diagnostics-and-tooling |
| Open research directions / methodology | moq-analyzers-research-frontier, moq-analyzers-research-methodology |

This skill tells you *why the walls are where they are*; the siblings tell you
how to work inside them.

## Provenance and maintenance

Re-verify each volatile claim before relying on it:

- ADR set (10 files, statuses): `ls docs/architecture/` and check `status:` frontmatter.
- Rule count/IDs (25, Moq1209 skipped): `grep -c 'internal const string' src/Common/DiagnosticIds.cs` (expect 25) and read the Moq1209 comment.
- Analyzer class count (24): `grep -l '\[DiagnosticAnalyzer' src/Analyzers/*.cs | wc -l`.
- Initialize invariant coverage: `grep -L 'EnableConcurrentExecution' $(grep -l '\[DiagnosticAnalyzer' src/Analyzers/*.cs)` — only the two MockBehavior analyzers (base class provides it) should appear.
- Roslyn pin / AnalyzerUtilities cap / transitive pinning: `grep -n 'CodeAnalysis.CSharp"\|AnalyzerUtilities\|TransitivePinning' Directory.Packages.props`.
- Host-compat target: `grep -n 'ValidateAnalyzerHostCompatibility' build/targets/packaging/Packaging.targets .github/workflows/main.yml`.
- BannedSymbols entries: `cat src/BannedSymbols.txt`; wiring: `grep -rn 'BannedSymbols' src/Directory.Build.props`.
- Weak-point anchors: `sed -n '383p' src/Analyzers/ConstructorArgumentsShouldMatchAnalyzer.cs` (cast), `sed -n '60p' src/Common/SemanticModelExtensions.cs` (Arguments[0]), `grep -n 'SingleOrDefault' src/Common/WellKnown/MoqKnownSymbols.cs`, `grep -n 'IsGenericMethod' src/Common/ISymbolExtensions.Moq.cs` — if an anchor no longer matches, the fix likely landed; check the issue state.
- Issue states: `gh issue view 1241 1242 1248 1250 1265 1266 1267 1268 1269 1270 986 --json state` (or one at a time) — remove fixed items from the weak-points table.
- Phantom symbols: `grep -n 'Moq.Language.IReturns"' src/Common/WellKnown/MoqKnownSymbols.cs` and the `_ReturnsNull` tests in `tests/Moq.Analyzers.Test/Common/MoqKnownSymbolsTests.ReturnsAndThrows.cs`.
- SquiggleCop wiring: `grep -rni squigglecop --include='*.yml' --include='*.props' --include='*.targets' .github build` (empty ⇒ still unwired) and version in `.config/dotnet-tools.json`.
- Test counts: `dotnet test --settings ./build/targets/tests/test.runsettings` (3,357 + 4 as of 2026-07-02; 2 PackageTests fail only in sandboxes with non-github.com git remotes).
- Perf gate wiring: `grep -n 'failOnRegression' build/scripts/perf/ComparePerfResults.ps1` and the `perf:` job in `.github/workflows/main.yml`.

Last verified: 2026-07-02 against commit 05135b2.
