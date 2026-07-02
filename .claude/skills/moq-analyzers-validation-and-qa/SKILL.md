---
name: moq-analyzers-validation-and-qa
description: "Defines what counts as test evidence in moq.analyzers — the mission-critical test bar. Load this when writing or reviewing tests for an analyzer or code fix, adding regression tests for a false positive/negative, choosing test taxonomy (positive span markup, negative, doppelganger, malformed-code, Moq-version fan-out), producing PR coverage evidence, or touching golden/certified suites (AllAnalyzersVerifier, PackageTests nuspec snapshots, MoqKnownSymbolsTests, AnalyzerAssemblyCompatibilityTests). Keywords: test markup, {|MoqXXXX:...|}, WithSpan, CompilerDiagnostics.None, ReferenceAssemblyCatalog, WithNamespaces, doppelganger, received/verified, Cobertura, coverage evidence, adversarial tests. Do NOT load for build/CLI setup (moq-analyzers-build-and-env), diagnosing a failing test's root cause (moq-analyzers-debugging-playbook), Roslyn API mechanics (roslyn-analyzer-reference), or the new-rule end-to-end checklist (moq-analyzers-rule-lifecycle)."
---

# Validation and QA: the mission-critical test bar

These analyzers ship into consumers' compilers and IDEs in mission-critical
codebases. A wrong diagnostic annoys thousands of developers per keystroke; a
crash disables the analyzer for the whole session. Tests are the only proof
this project accepts. This skill defines what counts as evidence, the test
taxonomy with worked examples from this repo, and the golden suites you must
not disturb.

**Definitions used throughout:**

- **Analyzer** — a class that inspects C# code inside the compiler and reports
  diagnostics (warnings) like `Moq1203`.
- **False positive (FP)** — analyzer flags correct code. **False negative
  (FN)** — analyzer misses wrong code. Both are priority-2 defects here
  (priority 1 is "never crash").
- **Span** — the exact character range a diagnostic underlines. Spans are
  pinned by tests and are non-negotiable.
- **Markup** — inline test notation `{|Moq1203:...|}` asserting that a
  diagnostic with that ID appears on exactly that text.

Run all tests (repo root; `dotnet` setup is covered by
`moq-analyzers-build-and-env`):

```bash
dotnet test --settings ./build/targets/tests/test.runsettings
```

Run one test class:

```bash
dotnet test --settings ./build/targets/tests/test.runsettings --filter "FullyQualifiedName~MethodSetupShouldSpecifyReturnValue"
```

Suite size (2026-07-02): 3,357 tests in `Moq.Analyzers.Test` (+4 in
`PerfDiff.Tests`). Tests target net8.0 and run against a pinned Roslyn 4.8
test compiler, so test code parses as C# 12 — C# 13 features (e.g. `params`
collections) are NOT parseable inside test sources.

## 1. Evidence hierarchy — compiling test code is the floor

Ranked weakest to strongest. Anything below level 1 is rejected outright.

| Level | Evidence | Status |
|---|---|---|
| 0 | Test code that does not compile | **Immediate failure.** Analyzers only run on parsed valid C#; a CSxxxx error invalidates the scenario. Rule source: `.github/instructions/csharp.instructions.md` ("Only Valid C# Code in All Tests") |
| 1 | Unmarked source through `AnalyzerVerifier` | A genuine **negative assertion** — the framework fails on ANY unexpected diagnostic |
| 2 | `{\|MoqXXXX:...\|}` markup | Asserts diagnostic ID + exact span |
| 3 | `DiagnosticResult` with `.WithSpan().WithArguments()` + severity | Strongest form: ID + severity + absolute span + message arguments |
| 4 | Level 2/3 fanned out across namespaces × Moq versions via `TestDataExtensions` | The project default for data-driven rows |

Hard rules that follow from level 0 (all from
`.github/instructions/csharp.instructions.md` and CONTRIBUTING.md "Moq
Version Compatibility"):

- **Never write a Moq setup for a static, const, readonly, or sealed member
  if that code would not compile.** Example: `mock.Setup(x => x.ConstField = 5)`
  is a compile error — not a test case. (Reading a static field inside a
  lambda, e.g. `Setup(x => SomeClass.StaticField)`, DOES compile and IS a
  legal test row — Moq fails at runtime, which is exactly what Moq1200 tests.)
- Never add tests using Moq APIs absent from the targeted Moq version
  (`SetupAdd`/`SetupRemove`/`.Protected().Setup` do not exist in 4.8.2) —
  they fail at compile time, not analyzer time.
- The single sanctioned escape hatch for deliberately-broken code is
  `CompilerDiagnostics.None` (section 3.6) — used to prove mid-edit
  robustness, always with a comment naming the suppressed CS errors.

Why so strict: the costliest historical failures were plausible-but-wrong
AI-authored changes — code that looked correct, compiled, and shipped a false
positive to angry customers. A test that doesn't compile proves nothing and
burns CI; a test that compiles but asserts the wrong thing is worse.

## 2. Test infrastructure map (verify claims against these files)

All helpers live in `tests/Moq.Analyzers.Test/Helpers/` (2026-07-02):

| File | What it is |
|---|---|
| `Test.cs` | `Test<TAnalyzer, TCodeFixProvider>` wraps Roslyn's `CSharpCodeFixTest`. Injects `global using System; ... global using Moq;` into every test compilation — rows don't need those usings |
| `AnalyzerVerifier.cs` | `AnalyzerVerifier<TAnalyzer>.VerifyAnalyzerAsync(source, group, ...)` — the workhorse. Overloads accept `.editorconfig` content, `CompilerDiagnostics`, or `params DiagnosticResult[]` |
| `CodeFixVerifier.cs` | `CodeFixVerifier<TAnalyzer, TFixer>.VerifyCodeFixAsync(original, fixed, group, ...)` |
| `ReferenceAssemblyCatalog.cs` | String-keyed catalog of reference-assembly sets: `Net80` (no Moq), `Net80WithOldMoq` (Moq 4.8.2), `Net80WithNewMoq` (Moq 4.18.4), `Net80WithNewMoqAndLogging` (4.18.4 + Logging.Abstractions 8.0.0). String keys keep VS Test Explorer rows expanded |
| `TestDataExtensions.cs` | Row combinators: `WithNamespaces()`, `WithMoqReferenceAssemblyGroups()` (both versions), `WithOldMoqReferenceAssemblyGroups()`, `WithNewMoqReferenceAssemblyGroups()` |
| `AllAnalyzersVerifier.cs` | Reflection-enrolled "no diagnostics from ANY analyzer" verifier (section 6.1) |
| `DoppelgangerTestHelper.cs` | Templates for user-defined `Mock<T>`/`MockBehavior` look-alikes (section 3.4) |
| `CompilationHelper.cs` | Raw `SemanticModel`/`SyntaxTree` boilerplate for unit tests of `src/Common/` helpers (not analyzer end-to-end tests) |

`Net80` (no Moq reference) exists to prove the `IsMockReferenced()` early
exit: every analyzer must stay silent when Moq isn't referenced.

## 3. Test taxonomy — one worked example of each, from this repo

### 3.1 Positive test with span markup

From `tests/Moq.Analyzers.Test/MethodSetupShouldSpecifyReturnValueAnalyzerTests.cs`:

```csharp
// Method with return type should specify return value
["""{|Moq1203:new Mock<IFoo>().Setup(x => x.DoSomething("test"))|};"""],
```

The markup asserts Moq1203 appears on exactly
`new Mock<IFoo>().Setup(x => x.DoSomething("test"))` — nothing more, nothing
less. Markup spans are relative to the row's own string, so rows are
position-independent (safe to append).

### 3.2 Negative test — unmarked source IS an assertion

```csharp
// Valid cases - method with return type that does specify return value
["""new Mock<IFoo>().Setup(x => x.DoSomething("test")).Returns(true);"""],
```

No markup = assertion that NO diagnostic fires anywhere in the row.
`CSharpCodeFixTest` fails on any unexpected diagnostic. Never treat unmarked
rows as filler — deleting one deletes a false-positive regression guard.

### 3.3 Strongest form — `DiagnosticResult.WithSpan().WithArguments()`

From `tests/Moq.Analyzers.Test/NoMockOfLoggerAnalyzerTests.cs` (the template
to copy when message arguments matter):

```csharp
await Verifier.VerifyAnalyzerAsync(
        """
        using Moq;
        using Microsoft.Extensions.Logging;

        internal class UnitTest
        {
            private void Test()
            {
                var mock = new Mock<ILogger>();
            }
        }
        """,
        ReferenceAssemblyCatalog.Net80WithNewMoqAndLogging,
        new DiagnosticResult("Moq1004", DiagnosticSeverity.Warning)
            .WithSpan("/0/Test1.cs", 8, 29, 8, 36)
            .WithArguments("NullLogger.Instance"));
```

`"/0/Test1.cs"` is the test-compilation file name (`Test0.cs` is the injected
global usings). CAUTION: these spans are **absolute file coordinates** —
inserting a line above shifts them (section 7).

### 3.4 Doppelganger test — user look-alike types must NOT trigger

A "doppelganger" is a user-defined class literally named `Mock<T>` (with its
own `MockBehavior` enum) that is not Moq. Symbol-based detection (ADR-001)
must ignore it. From `MethodSetupShouldSpecifyReturnValueAnalyzerTests.cs`:

```csharp
[Theory]
[MemberData(nameof(DoppelgangerTestHelper.GetAllCustomMockData), MemberType = typeof(DoppelgangerTestHelper))]
public async Task ShouldPassIfCustomMockClassIsUsed(string mockCode)
{
    await Verifier.VerifyAnalyzerAsync(
        DoppelgangerTestHelper.CreateTestCode(mockCode),
        ReferenceAssemblyCatalog.Net80WithNewMoq);
}
```

`DoppelgangerTestHelper.CreateTestCode` wraps the row in a template declaring
fake `Mock<T>`, `MockBehavior`, `Setup`, `Returns`, `As`, etc. Every new
analyzer needs a doppelganger suite; 10+ existing test classes use this
helper (grep `DoppelgangerTestHelper` to see them).

### 3.5 Code fix test — `[Theory]` + `[MemberData]`, before/after pairs

From `tests/Moq.Analyzers.Test/CallbackSignatureShouldMatchMockedMethodCodeFixTests.cs`:

```csharp
using Verifier = Moq.Analyzers.Test.Helpers.CodeFixVerifier<
    Moq.Analyzers.CallbackSignatureShouldMatchMockedMethodAnalyzer,
    Moq.CodeFixes.CallbackSignatureShouldMatchMockedMethodFixer>;

[
    """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<string>())).Callback(({|Moq1100:int i|}) => { });""",
    """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<string>())).Callback((string i) => { });""",
],
```

Row = (marked original, expected fixed). Identical pairs (no markup) assert
the fixer does NOT touch already-correct code. The
`[Theory]` + `[MemberData]` + `public static IEnumerable<object[]>` pattern
is mandatory (CONTRIBUTING.md).

### 3.6 Malformed-code test — `CompilerDiagnostics.None`

Mid-edit robustness is priority #1 (analyzers run per keystroke on broken
code). The sanctioned pattern, from
`tests/Moq.Analyzers.Test/CallbackSignatureShouldMatchMockedMethodAnalyzerTests.cs`
(`UnresolvableParameterType_ReportsDiagnostic`):

```csharp
// 'NonExistentType' does not resolve, producing TypeKind.Error.
new Mock<IFoo>().Setup(x => x.DoWork("test")).Callback(({|Moq1100:NonExistentType x|}) => { });
...
// CompilerDiagnostics.None suppresses CS0246 from the unresolvable type name.
await AnalyzerVerifier.VerifyAnalyzerAsync(source, referenceAssemblyGroup, CompilerDiagnostics.None);
```

Rules: always comment which CS errors are being suppressed and why; the test
still asserts analyzer behavior (diagnostic or silence), never merely "did
not crash". As of 2026-07-02 only 2 of 10 audited test classes have
malformed-code coverage — the weakest systematic gap (issue #1270 tracks it).

## 4. Mandatory edge axes for Setup/Returns/Raises-family analyzers

**Why this section exists — the Moq1203 saga.** Moq1203
("method setup should specify a return value") shipped, then needed **five
separate FP patches** because each fix missed the next syntactic wrapper:
`ReturnsAsync`/`Callback` fluent chaining (#886) → parentheses around Setup
(#895) → the same parens bug in Moq1100/Moq1206 (#907) → delegate-based
overloads like `ReturnsAsync((MyValue v) => v)` (#919) → user extension
methods wrapping `Returns` (#1086). Full commit-level incident record:
moq-analyzers-failure-archaeology entry 2.

Each escape was a real customer FP. The doctrine: any analyzer that inspects
the Moq fluent chain (`Setup`/`SetupGet`/`SetupSequence` → `Returns`/
`ReturnsAsync`/`Throws`/`Callback`/`Raises`, and `Raise`) must cover ALL of
these axes before merge:

| Axis | Example row (all exist in `MethodSetupShouldSpecifyReturnValueAnalyzerTests.cs` unless noted) |
|---|---|
| Parenthesized wrappers (incl. nested) | `(new Mock<IFoo>().Setup(x => x.GetValue())).Returns(42);` and `_ = (({\|Moq1203:new Mock<IFoo>().Setup(x => x.GetValue())\|}));` |
| Extension-method wrappers | `moq.Setup(x => x.GetValue()).ReturnsFortyTwo();` where `ReturnsFortyTwo` is a user extension returning `IReturns<...>` (Issue1067 data) |
| Fluent chaining, incl. `Callback` in the middle | `...Setup(...).Callback(() => { }).ReturnsAsync(1);` |
| Delegate overloads | `...ReturnsAsync((MyValue val) => val);` |
| Overload-resolution failure | wrong-typed args so Roslyn returns candidates, not a symbol: `...Returns("wrong type");` with `CompilerDiagnostics.None` |
| Variable vs inline mock; variable arguments | `var mock = new Mock<IFoo>(); mock.Setup(...)...` — different operation trees than inline |
| Named arguments | `mock.Verify(times: Times.Once(), expression: x => x.M())` — source order ≠ parameter order. Correct handling exemplar: `src/Analyzers/NoMethodsInPropertySetupAnalyzer.cs` selects by `argument.Parameter?.Ordinal == 0`. Known gap: `MoqVerificationHelpers` indexes `Arguments[0]` (audit 2026-07-02) |
| Malformed / mid-edit code | section 3.6 pattern |
| Both Moq versions | section 5 fan-out; gate 4.18.4-only APIs into the "new" group |

If you fix an FP on one axis, add rows for the OTHER axes in the same PR —
that is how the whack-a-mole ends. Every FP/FN fix ships with an
issue-linked regression test (CONTRIBUTING.md).

## 5. Moq version fan-out — why one logical case = 4 test rows

`TestDataExtensions` combinators each **prepend** a parameter and multiply
rows:

```csharp
return data.WithNamespaces().WithMoqReferenceAssemblyGroups();
```

- `WithNamespaces()` — ×2: prepends `""` and `"namespace MyNamespace;"`
  (analyzers must work in both file-scoped-namespace and no-namespace code).
- `WithMoqReferenceAssemblyGroups()` — ×2: prepends `Net80WithOldMoq`
  (4.8.2) and `Net80WithNewMoq` (4.18.4).

Net effect: **4 executed tests per data row**, and because combinators
prepend, the theory signature is
`(string referenceAssemblyGroup, string @namespace, string mock)` — the
LAST-applied combinator's value arrives FIRST.

Version gating: rows using 4.18.4-only APIs (e.g. `Callback` on non-void
setups via generic `ICallback<TMock, TResult>`, `Mock.Of<T>(predicate,
MockBehavior)`) go in a separate member-data method using
`.WithNewMoqReferenceAssemblyGroups()` (×2 total). Rows valid on both use
`.WithMoqReferenceAssemblyGroups()`. See `CallbackOnlyNewMoqTestData()` in
`MethodSetupShouldSpecifyReturnValueAnalyzerTests.cs` for the gating comment
style — always explain WHY a row is version-gated.

## 6. Certified / golden inventory — suites with special semantics

### 6.1 AllAnalyzersVerifier — reflection enrollment and its namespace trap

`tests/Moq.Analyzers.Test/Helpers/AllAnalyzersVerifier.cs` discovers every
concrete `[DiagnosticAnalyzer]` type and runs a source against ALL of them,
asserting zero diagnostics — the strongest false-positive guard. Used by the
cross-cutting suites (`MiscellaneousPatternsAnalyzerTests`,
`ProtectedMemberBasicPatternsAnalyzerTests`,
`DefaultValueProviderPatternsAnalyzerTests`, `IsRaisesMethodTests`).

**THE TRAP:** discovery filters on namespace **exactly** `Moq.Analyzers`:

```csharp
.Where(type => string.Equals(type.Namespace, "Moq.Analyzers", StringComparison.Ordinal) && ...)
```

An analyzer declared in `Moq.Analyzers.Foo` compiles, ships, and is
**silently dropped** from every all-analyzers no-diagnostics suite — no test
fails. When adding an analyzer, keep `namespace Moq.Analyzers;` (also
required by the architecture contract) and confirm enrollment: temporarily
report a diagnostic on a known-clean pattern and check an
`AllAnalyzersVerifier` suite fails, or count discovered types in a debugger.

### 6.2 PackageTests — golden nuspec snapshots (Verify.Nupkg)

`tests/Moq.Analyzers.Test/PackageTests.cs` snapshots the built NuGet
package: manifest (`.nuspec`) and file listing, via the Verify snapshot
library (`Verify.Nupkg`, initialized in `ModuleInitializer.cs`).
`ScrubNuspec()` strips volatile fields (version, commit) so snapshots are
stable. Golden files live next to the test:

```text
PackageTests.Baseline_main#manifest.verified.nuspec
PackageTests.Baseline_main#contents.verified.txt
PackageTests.Baseline_symbols#manifest.verified.nuspec
PackageTests.Baseline_symbols#contents.verified.txt
```

Flow on mismatch: the test fails and writes a sibling `*.received.*` file;
CI uploads `**/*.received.*` as the `verify-test-results-*` artifact
(`.github/workflows/main.yml`, "Upload *.received.* files" step).

To **intentionally accept** a package change (e.g. a new file ships in the
package): run the tests, diff `*.received.*` against `*.verified.*`, confirm
every difference is intended, copy received content over the corresponding
`.verified.` file, **delete the `.received.` files**, commit the updated
`.verified.` files with justification in the PR. Never commit `*.received.*`
(CONTRIBUTING.md: "Remove any `*.received.*` files before committing").

Known environmental failure (2026-07-02): in sandboxes whose git remote is
not a `https://github.com/<owner>/...` URL, the nuspec `<repository url>`
defeats the scrubber and the 2 `PackageTests.Baseline` tests fail. That is
the environment, not a code defect — do NOT "fix" it by accepting the
received files.

### 6.3 MoqKnownSymbolsTests — symbol pins and phantom symbols

`tests/Moq.Analyzers.Test/Common/MoqKnownSymbolsTests.*.cs` pin, for every
`MoqKnownSymbols` property (the once-per-compilation symbol cache,
`src/Common/WellKnown/MoqKnownSymbols.cs`):

- without Moq referenced → resolves null / empty (`Assert.Null` /
  `IsEmpty`);
- with Moq referenced → resolves non-null with expected name/arity, e.g.
  `IReturns2_WithMoqReference_ReturnsNamedTypeSymbol` asserts arity 2.

**Phantom symbols exist.** `MoqKnownSymbols.IReturns`
(`Moq.Language.IReturns`) and `IReturns1` (`` Moq.Language.IReturns`1 ``)
resolve **null under BOTH shipped Moq versions** — verified against the
4.8.2 and 4.18.4 package assemblies (2026-07-02): the metadata contains only
`` IReturns`2 ``, `` IReturnsGetter`2 ``, `` IReturnsResult`1 ``. Any
analyzer logic keyed on the phantoms is dead code. Similarly
`IRaiseable_WithMoqReference_ReturnsNullForMoq4` pins that `IRaiseable`
post-dates 4.18.4.

**Rule when registering a new symbol in `MoqKnownSymbols`:** add BOTH a
without-Moq null/empty pin AND a with-Moq `Assert.NotNull` (+ name/arity)
test. The with-Moq test is what catches typos in metadata names — the
without-Moq test passes even for phantoms. (The delegate-based
`ReturnsAsync` overloads live in `Moq.GeneratedReturnsExtensions`, which is
NOT tracked in `MoqKnownSymbols` today — root cause of the Moq1203
name-fallback, issue #1243.)

### 6.4 AnalyzerAssemblyCompatibilityTests — the CS8032 guard

`tests/Moq.Analyzers.Test/AnalyzerAssemblyCompatibilityTests.cs` loads each
shipped DLL (`Moq.Analyzers`, `Moq.CodeFixes`, bundled
`Microsoft.CodeAnalysis.AnalyzerUtilities`) and asserts no reference to
`System.Collections.Immutable` / `System.Reflection.Metadata` exceeds
8.0.0.0 — the max a .NET 8 host (VS 2022 17.8+) provides. Exceeding it
reproduces incident #850: the analyzer fails to load with CS8032 in every
consumer IDE (v0.4.1 existed solely to fix this). This test is one of three
enforcement layers (plus an MSBuild target and a 9-way CI load-test matrix).
Never loosen `MaxImmutableVersion`/`MaxMetadataVersion` to make a dependency
bump pass — that defeats the guard; see `moq-analyzers-change-control`.

## 7. Adding tests to an existing analyzer without disturbing pinned spans

1. **Append rows; don't reorder.** Markup rows (`{|...|}`) are
   self-contained — their spans are relative to the row string. Appending to
   the end of a member-data array cannot break existing rows.
2. **Don't edit shared source templates when absolute spans exist.** Tests
   using `DiagnosticResult.WithSpan("/0/Test1.cs", line, col, ...)` (e.g.
   `NoMockOfLoggerAnalyzerTests`) encode absolute coordinates. Adding a
   member to a shared template/fixture ABOVE the flagged line shifts every
   span below it. Extend fixtures by appending at the bottom, or add a new
   dedicated `[Fact]`/`[Theory]` with its own source string.
3. **Fixture members must be referenced.** Dead fixtures mask untested
   scenarios — the audit found `SampleClassWithStaticMembers` (Moq1200) and
   five `IFoo` methods (Moq1100) declared and used by zero rows. Every
   fixture member you add must appear in at least one data row.
4. **Pin known false negatives explicitly.** If current behavior misses a
   case you cannot fix in this PR, add the row asserting today's (wrong)
   behavior with a comment saying "known false negative; pins current
   behavior" — so the row self-explains when a future fix flips it. Worked
   example: void-member setups bind the non-generic
   `Setup(Expression<Action<T>>)` overload, which `IsMoqSetupMethod`
   excludes via its `IsGenericMethod` check
   (`src/Common/ISymbolExtensions.Moq.cs:42`), so they are never analyzed —
   issue #1270 pins this in comments rather than hiding it.
5. **Span test failures are a stop signal.** If your change moves a pinned
   span: stop after the first unexpected span failure and re-derive the
   expected span from the rule's report-location contract; escalate after
   two. Never "fix" a span test by pasting whatever the failure message
   says without understanding why it moved.

## 8. AI-code adversarial protocol — a HARD gate

History: PR #511 (Copilot-authored) introduced Moq1302
(LINQ-to-Mocks validation) with implementation AND tests written by the same
model. Both shared the same blind spot: comparison right-hand sides that are
value expressions (static const, captured local, literal). Result: live
false positives on canonical customer code (issue #1010), fixed in
`4b705e2` (#1017) with a regression suite in `3399297` (#1020) adding
exactly those cases. Lesson: **an AI's tests do not test the AI's blind
spots** — implementation and tests written by one mind (human or model)
share failure modes.

The gate, before merge of any AI-assisted analyzer change:

1. A human or a **second, independent** agent (one that did not see the
   implementation) writes adversarial rows.
2. Minimum adversarial matrix — for every value position the analyzer
   inspects, one row each where that position is a:
   - **literal** (`42`, `"x"`),
   - **local variable** (captured or not),
   - **static member** (`StatusCodes.Status200OK`),
   - **const field**,
   - **method call** (`GetValue()`),
   plus the section 4 wrapper axes if the rule touches the fluent chain.
3. Each adversarial row is verified against the ACTUAL current analyzer
   behavior before being committed with an expectation (run it; don't guess
   the marking).
4. PR evidence names who/what produced the adversarial rows.

"Plausible-but-wrong is the enemy": a green suite whose rows were generated
from the same mental model as the code is plausible evidence, not proof.

## 9. Coverage discipline

- Coverage collection is automatic with the standard test command (the
  runsettings configures the Cobertura collector; only `Moq.Analyzers.dll`
  and `Moq.CodeFixes.dll` are instrumented; property getters/setters and
  test code are excluded — `build/targets/tests/test.runsettings`).
- After `dotnet test`, ReportGenerator (target `GenerateCoverageReport` in
  `build/targets/tests/Tests.targets`) writes:
  - `artifacts/TestResults/coverage/SummaryGithub.md` — paste-ready PR
    summary,
  - `artifacts/TestResults/coverage/Cobertura.xml`,
  - `artifacts/TestResults/coverage/index.html` — per-line HTML.
- **Bar for new analyzer code: 100% block coverage** (project quality gate,
  2026-07-02). Repo-wide, epic #639/#645 targets 95% line / 90% branch with
  100% as the success metric; genuinely-unreachable defensive paths must be
  documented with justification, not silently left red.
- **Coverage evidence is mandatory in every PR** (CONTRIBUTING.md,
  "Validation Evidence Requirements"): include the coverage summary for
  changed code alongside format/build/test output. "Coverage validation is
  a required step before yielding, submitting, or completing any task."
- Coverage is necessary, not sufficient: 100%-covered code with weak
  assertions still ships bugs (that is what the dead fixtures and mutation
  testing epic #904 are about). Coverage tells you what never ran; sections
  3-4 and 8 tell you whether what ran was actually asserted.

## 10. Known thin spots (issue #1270 inventory, filed 2026-07-02, open)

Issue #1270 is the implementation-ready backlog for the audit's test-gap
findings. Ranked thinnest first (do not duplicate its work — check the
issue state before adding rows to these suites):

| Rule | Suite | Gap summary |
|---|---|---|
| Moq1300 | `AsAcceptOnlyInterfaceAnalyzerTests` | 4 rows total; no generic/nested-generic interfaces, chained `As`, delegate/struct/enum args, malformed code |
| Moq1400 | `SetExplicitMockBehaviorAnalyzerTests` | no ctor-args+behavior combos, no `MockRepository.Create`, no `Mock.Of(predicate)`, no malformed code |
| Moq1202 | `RaiseEventArgumentsShouldMatchEventSignatureAnalyzerTests` | no non-generic `EventHandler` rows (the exact gap that hid a live FP on canonical `Raise` usage), no custom delegates, no multi-param `Action` |
| Moq1420 | `RedundantTimesSpecificationAnalyzerTests` | no `Func<Times>` method-group form, no `using static Moq.Times`, no Times-in-local |
| Moq1200 | `SetupShouldBeUsedOnlyForOverridableMembersAnalyzerTests` | dead `SampleClassWithStaticMembers` fixture; no extension-method Setup; no malformed lambda |
| Moq1100 | `CallbackSignatureShouldMatchMockedMethodAnalyzerTests` | 5 dead fixture methods; no method-group/custom-delegate/async-lambda callbacks |

Cross-cutting: malformed-code tests exist in only 2 of 10 audited classes.
Strong templates to copy: `NoSealedClassMocksAnalyzerTests` (exemplary edge
coverage) and `MethodSetupShouldSpecifyReturnValueAnalyzerTests`
(near-exhaustive, post-saga).

## When NOT to use this skill

- Setting up the SDK, building, or fixing PedanticMode/CI-parity build
  breaks → `moq-analyzers-build-and-env`.
- Root-causing WHY a test fails or an analyzer misbehaves →
  `moq-analyzers-debugging-playbook`.
- Roslyn API mechanics (operations vs syntax, symbol comparison) →
  `roslyn-analyzer-reference`; Moq API semantics (what overloads exist in
  4.8.2 vs 4.18.4) → `moq-api-reference`.
- End-to-end checklist for shipping a NEW rule (IDs, release notes, docs,
  benchmarks) → `moq-analyzers-rule-lifecycle`.
- Architecture invariants the tests enforce (ADR-001 symbol detection,
  thread safety) → `moq-analyzers-architecture-contract`.
- History of settled battles (why Moq1203 was kept, string→symbol war) →
  `moq-analyzers-failure-archaeology`.
- The FP-fixing campaign strategy and issue backlog #1241-#1278 →
  `moq-analyzers-fp-convergence-campaign`.
- PR format, evidence packaging, and branch/commit rules →
  `moq-analyzers-change-control`.
- Perf gates and benchmark evidence → `moq-analyzers-proof-toolkit` /
  `moq-analyzers-diagnostics-and-tooling`.

## Provenance and maintenance

Re-verify volatile claims with:

- Test count: `dotnet test --settings ./build/targets/tests/test.runsettings 2>&1 | grep -E "Passed!|Failed!|total"` (3,357 in Moq.Analyzers.Test as of 2026-07-02; 2 PackageTests failures are sandbox-remote-URL artifacts only).
- Helper inventory: `ls tests/Moq.Analyzers.Test/Helpers/` (8 files as of 2026-07-02).
- Catalog keys and Moq pins (4.8.2 / 4.18.4): `grep -n "PackageIdentity" tests/Moq.Analyzers.Test/Helpers/ReferenceAssemblyCatalog.cs`.
- Namespace-trap filter still exact-match: `grep -n '"Moq.Analyzers"' tests/Moq.Analyzers.Test/Helpers/AllAnalyzersVerifier.cs`.
- Phantom symbols still phantom: `grep -n "Moq.Language.IReturns" src/Common/WellKnown/MoqKnownSymbols.cs` then confirm absence in Moq metadata (`dotnet-inspect member "IReturns" --package Moq --all`, or byte-scan the package DLL for a null-terminated `` IReturns` `` type name).
- Golden nuspec files: `ls tests/Moq.Analyzers.Test/PackageTests.Baseline*`.
- CS8032 guard ceilings still 8.0.0.0: `grep -n "MaxImmutableVersion\|MaxMetadataVersion" tests/Moq.Analyzers.Test/AnalyzerAssemblyCompatibilityTests.cs`.
- Issue states: #1270 (thin-spot inventory, open as of 2026-07-02), #639/#645 (coverage epic, open), #904 (mutation testing, open), #1243 (GeneratedReturnsExtensions not in KnownSymbols): `gh issue view <n> --repo rjmurillo/moq.analyzers`.
- Moq1203 saga commits: `git log --oneline --no-walk 6ec810c c270302 894313b 0bef80b 5eec7e1`.
- Coverage outputs: run the test command, then `ls artifacts/TestResults/coverage/`.
- Compile-rule text: `sed -n 11,20p .github/instructions/csharp.instructions.md`.

- Frontmatter stays parser-safe: `python3 -c "import yaml; print(len(yaml.safe_load(open('.claude/skills/moq-analyzers-validation-and-qa/SKILL.md').read().split('---')[1])['description']))"` — expect the full description length, not an error or a truncated count

Last verified: 2026-07-02 against commit 05135b2.
