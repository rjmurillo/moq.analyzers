---
name: moq-analyzers-debugging-playbook
description: Diagnose failures in the moq.analyzers repo by symptom — load this when a test, build, hook, or CI check fails and you need to know why. Triggers include "Expected span ... but got ...", "Expected diagnostic to start on line", AD0001 (analyzer threw an exception), CS8032 (analyzer could not be loaded), a user-reported false positive or false negative, Verify snapshot mismatches / *.received.* files, "builds locally but fails CI", MSB4018 GetBuildVersion, perf gate (PerfDiff) failures, tests running 4x the rows you wrote, or pre-push hook rejection (todo-scanner, format). Do NOT load it for how-to-build/env setup (moq-analyzers-build-and-env), for authoring a new rule end-to-end (moq-analyzers-rule-lifecycle), for what gates a PR must pass (moq-analyzers-change-control), for Roslyn API semantics (roslyn-analyzer-reference), or for systematically burning down the FP backlog (moq-analyzers-fp-convergence-campaign).
---

# Debugging playbook: symptom → triage → fix

This repo ships analyzers that run inside customers' compilers. Priority order
(from `.github/copilot-instructions.md`): (1) no analyzer crashes, (2) no false
positives/negatives, (3) per-keystroke performance, (4) thread safety. The
costliest historical failure mode here is *plausible-but-wrong fixes*: code
nudged until a test passes, without understanding why it failed. This playbook
exists to prevent that.

**Cardinal rule (repo doctrine, `.github/copilot-instructions.md`): never
"fix" a failing test by slightly adjusting the code and re-running.** The fix
must come from a deliberate, correct understanding of the failure. If a build
or tool error message contains a URL, follow it first.

Terms used throughout:

| Term | Meaning |
|---|---|
| AD0001 | Compiler diagnostic: "Analyzer ... threw an exception". Roslyn disables the crashing analyzer for the rest of the session. |
| CS8032 | Compiler warning: an analyzer assembly could not be loaded (usually a dependency version the host doesn't have). |
| FP / FN | False positive (diagnostic fires on correct code) / false negative (diagnostic misses bad code). |
| Span | The exact character range a diagnostic underlines. Tests pin spans with markup like `{\|Moq1002:(1, true)\|}`. |
| Verify | Verify.Xunit/Verify.Nupkg snapshot testing: compares output to a committed `*.verified.*` file; a mismatch writes a `*.received.*` file. |
| PedanticMode | MSBuild property (`build/targets/codeanalysis/CodeAnalysis.targets`) that turns all warnings into errors. Defaults to the value of `ContinuousIntegrationBuild`: **off locally, on in CI**. |
| binlog | MSBuild binary log (`/bl:` flag), openable with the MSBuild Structured Log Viewer; contains full diagnostic text including exception stacks. |
| PerfDiff | In-repo tool (`src/tools/PerfDiff/`) that compares BenchmarkDotNet results against a baseline; its exit code is a required PR merge gate (ADR-008). |
| NBGV | Nerdbank.GitVersioning; computes the package version from `version.json` + git history height. |

## Master triage table

| # | Symptom | First discriminating check | Likely cause | Fix path | Story |
|---|---|---|---|---|---|
| 1 | Test fails: `Expected diagnostic to start on line/column ... was actually ...` (doctrine shorthand: "Expected span ... but got ...") | Is the *test markup* or the *analyzer* wrong? Markup is the pinned contract | Wrong syntax-node navigation in the analyzer | **STOP protocol** (section 1) — never nudge-and-rerun | Repo doctrine, `.github/copilot-instructions.md` |
| 2 | Build output shows AD0001; analyzer silently stops reporting | Get the exception stack from a binlog (`dotnet build -bl:msbuild.binlog`) | Unguarded cast/index/LINQ `Single` on mid-edit or exotic code | Fix root cause + regression test; never add catch blocks | Audit 2026-07-02: crash surfaces in section 2 |
| 3 | CS8032 "analyzer could not be loaded" in a consumer project | Which host? Which dependency version does the shipped DLL reference? | Host-provided dependency (e.g. System.Collections.Immutable) bumped past the .NET 8 host ceiling | Respect version ceilings; see section 3 | Incident #850 (v0.4.0), fixed #888, release v0.4.1 |
| 4 | User reports a diagnostic on correct code (FP) | Reproduce as a failing in-repo test FIRST, then against the built package | Missing syntactic-wrapper handling, or semantic check done by string name | Section 4; classification tree in moq-analyzers-fp-convergence-campaign | Moq1203 saga: 5 patches (section 4) |
| 5 | User reports a missed diagnostic (FN) | Does the pattern even reach the analyzer? (Moq version, generic vs non-generic Setup, target-typed `new`) | Known analysis-scope gap or unresolved symbol | Section 5; never fix with string-name matching (ADR-001) | Known FN inventory in section 5 |
| 6 | `PackageTests.Baseline` fails; `*.received.*` files appear | `git remote get-url origin` — is it a `https://github.com/...` URL? | Sandbox remote URL defeats the nuspec scrubber, OR a real packaging change | Section 6 | 2 sandbox-only failures observed 2026-07-02 |
| 7 | `dotnet build` clean locally, CI build job fails with warnings-as-errors | Rerun with `dotnet build /p:PedanticMode=true` | PedanticMode divergence (warnings pass locally) | Section 7 | By-design divergence in `CodeAnalysis.targets` |
| 8 | `MSB4018: The "Nerdbank.GitVersioning.Tasks.GetBuildVersion" task failed` | `git rev-parse --is-shallow-repository` | Shallow clone; NBGV needs full history | `git fetch --unshallow` | Documented in `.github/copilot-instructions.md` |
| 9 | New analyzer never runs in `AllAnalyzersVerifier` no-diagnostic suites (passes vacuously) | `grep -n "^namespace" src/Analyzers/YourAnalyzer.cs` | Namespace is not exactly `Moq.Analyzers` | Section 9 | Reflection discovery contract |
| 10 | CI `perf` job (required check) fails | Read which strategy fired in the job log; compare baseline vs perfTest reports | Real regression, OR a known PerfDiff tool defect | Section 10 — decision procedure for "tool bug vs real" | Open defects #1265–#1269 (2026-07-02) |
| 11 | A test class shows ~4x the rows you wrote | Count `WithNamespaces().WithMoqReferenceAssemblyGroups()` in the MemberData | Intentional fan-out: 2 namespaces x 2 Moq versions | Not a bug — section 11 | Test infra design |
| 12 | `git push` rejected by hook | Read which Husky task failed (todo-scanner vs build-and-test) | Unlinked TODO, or CI-parity build/test failure | Section 12 | S1135 CI self-lock (`3d4f7ff` → `b1439ab`; the tripwire was a `TODO(#1012)` comment — #1012 itself is an unrelated enhancement) |

---

## 1. Diagnostic span test failure — the STOP protocol

**Exact failure text** (from Microsoft.CodeAnalysis.Testing 1.1.2-beta1.24314.1,
the pinned test framework — verified against the shipped assembly):

```text
Expected diagnostic to start on line "X" was actually on line "Y"
Expected diagnostic to start at column "X" was actually at column "Y"
```

followed by an `Expected diagnostic:` / `Actual diagnostic:` pair showing
`// /0/Test0.cs(line,col,line,col): warning MoqXXXX: ...` for both. Repo docs
refer to this class of failure as `Expected span ... but got ...`.

**The STOP protocol** (verbatim doctrine from `.github/copilot-instructions.md`,
"Diagnostic Spans are Non-Negotiable"):

1. All diagnostic spans MUST be character-precise.
2. A span test failure is a **CRITICAL FAILURE** — it signals a fundamental
   misunderstanding of the syntax tree.
3. If a span test fails **once**: STOP implementation work. Re-derive your
   entire syntax-tree navigation logic from the actual tree (not from memory).
4. If it fails a **second time**: admit failure and request expert human
   guidance. Do not proceed.
5. **Never** nudge offsets, tweak `Location.Create()` arguments, or move test
   markup until the numbers happen to match.

**How to read the expected-vs-actual output.** Line/column are 1-based within
the generated test file (`/0/Test0.cs`), which includes boilerplate the test
helper (`tests/Moq.Analyzers.Test/Helpers/Test.cs`) wraps around your source
string — so absolute numbers rarely match your snippet's line numbers. Compare
the *expected* and *actual* rows to each other, not to your snippet. The test
markup is the contract:

```csharp
// tests/Moq.Analyzers.Test/ConstructorArgumentsShouldMatchAnalyzerTests.PrivateCtors.cs
["""new Mock<NoConstructorClass>{|Moq1002:(MockBehavior.Default)|};"""],
```

Here Moq1002 must be reported on exactly `(MockBehavior.Default)` — the
argument list including parentheses, not the whole expression, not the type.
Span policy varies per rule and is pinned by that rule's tests. Do not move
markup to match your code; changing a shipped rule's span policy is a behavior
change (see moq-analyzers-change-control).

**Run just the failing test** (repo root; needs .NET SDK per `global.json`
plus the .NET 8 runtime for the net8.0 test project):

```bash
dotnet test --settings ./build/targets/tests/test.runsettings \
  --filter "FullyQualifiedName~ConstructorArgumentsShouldMatchAnalyzerTests"
```

**Root-cause method that works here:** dump the actual tree instead of
guessing — in a scratch xunit test or debugger, call
`node.ToFullString()` / `node.GetLocation().GetLineSpan()` on each candidate
node, and read existing analyzers in `src/Analyzers/` that report on the same
node shape. `Location.Create()` needs the syntax tree plus the exact
`TextSpan`; taking `.GetLocation()` of the wrong (parent/child) node is the
usual bug.

## 2. AD0001 — analyzer crash

**What it is.** Any unhandled exception in an analyzer callback surfaces as
compiler diagnostic AD0001 and Roslyn disables that analyzer for the rest of
the compilation/IDE session. `src/Analyzers`, `src/CodeFixes`, and
`src/Common` intentionally contain **zero catch blocks** (verified 2026-07-02)
— exceptions must propagate so crashes are visible. Never fix an AD0001 by
adding a catch; fix the root cause and add a regression test.

**Getting the stack:**

| Context | Command / location |
|---|---|
| Local CLI build | `dotnet build -bl:msbuild.binlog` then open the binlog in MSBuild Structured Log Viewer; or `dotnet build -v:diag \| grep -A 40 AD0001` — the AD0001 message text embeds the exception type, message, and stack |
| CI | The `build` job uploads a `binlogs` artifact (`.github/workflows/main.yml`, "Upload binlogs" step; logs are written to `artifacts/logs/`) |
| Visual Studio | Run `devenv /log`, reproduce, then read `%APPDATA%\Microsoft\VisualStudio\<version>\ActivityLog.xml`; the exception also appears in the in-IDE analyzer diagnostics (light bulb on the AD0001 entry in the Error List) |
| Test suite | The Roslyn test framework fails the test with the analyzer exception inline — no extra steps |

**Known crash surfaces** (line-verified 2026-07-02 at commit 05135b2; from the
54-finding audit, filed in the #1241–#1264 issue cluster):

| Surface | File:line | Trigger |
|---|---|---|
| Unguarded `(IArrayTypeSymbol)` cast | `src/Analyzers/ConstructorArgumentsShouldMatchAnalyzer.cs:383` | C# 13 params collection (`params ReadOnlySpan<T>`) ctor + `new Mock<T>(...)` on a modern Roslyn host → `InvalidCastException` |
| Unguarded `ArgumentList.Arguments[0]` | `src/Common/SemanticModelExtensions.cs:60` | Invoking the Moq1100 Callback code fix on mid-edit `mock.Setup().Callback(...)` → `ArgumentOutOfRangeException` |
| `SingleOrDefault` inside cached `Lazy` | `src/Common/WellKnown/MoqKnownSymbols.cs:643` | Duplicate source-defined member mid-edit → exception is **cached** by the Lazy → persistent AD0001 for the compilation |
| Unbounded recursion | `src/Analyzers/LinqToMocksExpressionShouldBeValidAnalyzer.cs` (binary-operation walker) | Machine-generated `Mock.Of<T>` with thousands of `&&` clauses → stack overflow (process crash, not AD0001) |

If a reported stack matches one of these, link the existing issue rather than
filing a duplicate. Mid-edit (incomplete/unparseable) code is the most common
trigger class overall — reproduce with `CompilerDiagnostics.None` style tests
(see `CallbackSignatureShouldMatchMockedMethodAnalyzerTests` for the pattern).

## 3. CS8032 — analyzer load failure in a consumer host

**What it is.** The shipped `Moq.Analyzers.dll` (netstandard2.0) loads into
whatever Roslyn host compiles the consumer's code. If it references an
assembly version the host doesn't provide, binding fails and the compiler
emits CS8032: the analyzers silently do nothing.

**The #850 story (do not repeat it).** Release v0.4.0 (2025-11-27) shipped
with a transitive pin of System.Collections.Immutable 10.0 (Renovate PR #822)
and a bundled AnalyzerUtilities 4.14 referencing SCI 9.0. .NET 8 SDK hosts
(VS 2022 17.8+) provide SCI **8.0.0.0** only → CS8032 for every consumer on
.NET 8. Fixed by commit `38943ac` (PR #888); release v0.4.1 existed solely to
ship the fix.

**Enforcement now (triple-layered, all must stay in place):**

1. MSBuild gate `ValidateAnalyzerHostCompatibility` in
   `build/targets/packaging/Packaging.targets` — build **errors** if
   System.Collections.Immutable or System.Reflection.Metadata resolve above
   major version 8 for shipped projects.
2. An inline DLL-reference check in the CI `build` job.
3. The `analyzer-load-test` CI job (`.github/workflows/main.yml`): a 9-way
   matrix that installs the freshly packed nupkg into a scratch project and
   compiles — dotnet CLI hosts net8.0/net9.0/net10.0 on ubuntu-24.04-arm, and
   MSBuild/.NET Framework hosts net472/net48/net481 on both windows-2022
   (VS 2022) and windows-2025-vs2026 (matrix as of 2026-07-02).

**Triage a CS8032 report:**

```bash
# What does the shipped DLL actually reference? (any decompiler works; ILSpy CLI shown)
# Or: inspect the nupkg produced by `dotnet build`:
ls artifacts/package/debug/Moq.Analyzers.*.nupkg
```

Check the referenced versions of System.Collections.Immutable /
System.Reflection.Metadata against the host's. Roslyn is pinned to 4.8
(ADR-003), AnalyzerUtilities to 3.3.4 (ADR-004), and Renovate ignores
`Microsoft.CodeAnalysis.*` — if a dependency PR touches any of these, that is
a change-control question (moq-analyzers-change-control), not a debugging one.

## 4. False-positive report triage

A user says rule MoqXXXX fires on correct code. Order of operations:

1. **Reproduce as an in-repo failing test first.** Add the user's snippet as a
   test row with NO markup (unmarked source is a genuine negative assertion —
   any diagnostic fails the test). For "no analyzer should fire" cases use
   `AllAnalyzersVerifier.VerifyAllAnalyzersAsync(source, referenceAssemblyGroup)`.
   Run both Moq versions: the fan-out helpers (section 11) give you 4.8.2 and
   4.18.4 rows automatically.
2. **Reproduce against the live built package** to rule out test-harness
   masking. Build produces a local nupkg at
   `artifacts/package/debug/Moq.Analyzers.<version>.nupkg`; wire it into a
   scratch project via a local-feed `nuget.config` exactly as the
   `analyzer-load-test` job does (see its "Create test project" step in
   `.github/workflows/main.yml` for a copy-pasteable template). The
   moq-analyzers-diagnostics-and-tooling skill covers this live-DLL harness in
   depth.
3. **Test the mandatory wrapper axes.** The Moq1203 FP saga took FIVE patches
   because each fix missed a syntactic wrapper: `6ec810c` (fluent chaining,
   #849) → `c270302` (parenthesized Setup, #887) → `894313b` (extended to
   Moq1100/1206) → `0bef80b` (delegate overloads) → `5eec7e1`
   (extension-method wrapping). Before declaring an FP fixed, add test rows
   for: parentheses around the setup, extension-method wrapping, fluent
   chaining continuation, delegate-based overloads, and both Moq versions.
4. **For operation-based analyzers**, apply the 3-phase walker discipline from
   the Moq1302 LINQ-to-Mocks FP fix (#1010 → fix #1017 + regression suite
   #1020): register by `OperationKind` → guard `operation.Instance == null`
   (static receiver = value expression, skip) → only then validate the member.
5. **Classify before fixing.** Whether the fix is "narrow the check",
   "add a known-symbol", or "escape hatch / config flag" is decided by the
   escape-hatch decision tree in moq-analyzers-fp-convergence-campaign.
6. **Ship the regression test issue-linked** (PR rule: every FP/FN fix carries
   an issue-linked regression test).

Test code must be valid, compiling C# — never write a repro that sets up
static/const members in ways that would not compile (test infra treats
compiler errors as failures unless the test opts into `CompilerDiagnostics.None`).

## 5. False-negative report triage

A user says rule MoqXXXX should have fired and didn't. Discriminating checks
in order:

1. **Does Moq resolve at all?** Every analyzer early-exits via
   `IsMockReferenced()` (`src/Common/WellKnown/MoqKnownSymbolExtensions.cs`)
   when the compilation doesn't reference Moq. In tests, the `Net80` reference
   group has no Moq — analyzers are silent by design there.
2. **Which Moq version?** 4.8.2 lacks `SetupAdd`/`SetupRemove`, generic
   `Protected().Setup` forms, and most `IThrows` overloads; an FN on 4.8.2 may
   be "API doesn't exist there". Verify API ground truth with dotnet-inspect
   (`dotnet tool install -g dotnet-inspect`, then
   `dotnet-inspect member "IReturns<TMock, TResult>" --package Moq --all`).
3. **Known FN inventory** (2026-07-02): non-generic
   `Setup(Expression<Action<T>>)` (void members) is never analyzed because
   `IsMoqSetupMethod` requires `IsGenericMethod` — pinned as commented-out
   test rows, tracked in #1270. Target-typed `new Mock<T>` via
   `Mock<IFoo> m = new(42);` is skipped by Moq1001/Moq1002 (registered syntax
   kind is `ObjectCreationExpression` only — audit finding A-7). Delegate-based
   `ReturnsAsync` lives in `Moq.GeneratedReturnsExtensions`, not tracked in
   `MoqKnownSymbols` (#1243). `MoqKnownSymbols` also has phantom properties
   (non-generic `IReturns` and ``IReturns`1``) that resolve null by design —
   pinned by `MoqKnownSymbolsTests`; always add a resolves-non-null test when
   registering a new symbol.
4. **Write the failing test with markup** (`{|MoqXXXX:...|}`) before touching
   the analyzer, then make it pass.
5. **Forbidden fix:** widening detection with string name comparisons. ADR-001
   requires symbol-based detection; a name check is allowed only as a cheap
   pre-filter *before* an authoritative symbol check. The string→symbol
   migration (#245→#1030) is settled doctrine — see
   moq-analyzers-failure-archaeology.

## 6. Verify snapshot failures (.received. files)

`PackageTests.Baseline` (in `tests/Moq.Analyzers.Test/PackageTests.cs`)
snapshots the nupkg manifest and contents with Verify.Nupkg 3.0.1, scrubbing
volatile fields via `.ScrubNuspec()`. On mismatch, Verify writes
`*.received.nuspec` / `*.received.txt` next to the committed `*.verified.*`
files.

Triage:

1. **Check for the sandbox trap first** (2026-07-02: the 2 known failures in
   sandboxed containers are exactly this): `git remote get-url origin`. The
   nuspec `<repository url>` is derived from the git remote; `ScrubNuspec`
   expects a `https://github.com/<owner>/...` URL. A local-proxy or SSH-alias
   remote defeats the scrubber → snapshot mismatch that is **environmental,
   not a code defect**. Do not "fix" the verified files for this.
2. Otherwise diff intent: `diff tests/Moq.Analyzers.Test/PackageTests.Baseline_main#manifest.verified.nuspec <the .received. file>`.
   If the packaging change is intentional and approved, copy received →
   verified and commit the verified file.
3. **Delete `*.received.*` before committing** (rule at `CONTRIBUTING.md:334`).
   They are failure artifacts; CI uploads them for you on failure as the
   `verify-test-results-<os>` artifact (`main.yml`, "Upload *.received.* files"
   step) — that artifact is how you inspect a CI-only snapshot failure.

## 7. Builds clean locally but fails CI

Root cause is almost always PedanticMode. Definition
(`build/targets/codeanalysis/CodeAnalysis.targets:3-5`):

```xml
<PedanticMode Condition=" '$(PedanticMode)' == '' ">$([MSBuild]::ValueOrDefault('$(ContinuousIntegrationBuild)', 'false'))</PedanticMode>
<TreatWarningsAsErrors>$(PedanticMode)</TreatWarningsAsErrors>
```

So any compiler/analyzer **warning** passes your default local build and fails
CI. Repro locally:

```bash
dotnet build /p:PedanticMode=true
```

Full CI-parity command (what the pre-push hook runs —
`build/scripts/hooks/Invoke-PrePushBuild.ps1`):

```bash
dotnet build Moq.Analyzers.sln --configuration Release --verbosity quiet \
  /p:PedanticMode=true /p:Deterministic=true /p:ContinuousIntegrationBuild=true \
  /p:UseSharedCompilation=false /p:BuildInParallel=false /nodeReuse:false
```

If the build is clean under PedanticMode but CI still fails, check the other
divergences in order: `dotnet format --verify-no-changes` (format drift), the
`linters` job (markdownlint-cli2 / yamllint / actionlint — run the same tools
locally via the pre-commit hook), and OS/arch differences (CI tests run on
ubuntu-24.04-arm and windows).

## 8. MSB4018 — GetBuildVersion task failed

```text
error MSB4018: The "Nerdbank.GitVersioning.Tasks.GetBuildVersion" task failed unexpectedly.
```

NBGV computes the version from git height; a shallow clone breaks it. Fix
(documented in `.github/copilot-instructions.md`, "AI Agent Troubleshooting"):

```bash
git fetch --unshallow
```

Check first with `git rev-parse --is-shallow-repository` (prints `true` when
shallow).

## 9. Analyzer missing from AllAnalyzersVerifier

`tests/Moq.Analyzers.Test/Helpers/AllAnalyzersVerifier.cs` reflection-discovers
analyzers with three conditions (verified in source):

- `type.Namespace` is **exactly** `"Moq.Analyzers"` (ordinal compare — no
  sub-namespaces),
- non-abstract and derives from `DiagnosticAnalyzer`,
- carries `[DiagnosticAnalyzer]`.

A new analyzer in `Moq.Analyzers.Something` compiles, ships, and passes its
own tests — but is **silently excluded** from every "no analyzer fires on this
valid code" suite, so cross-rule FPs go undetected. Symptom: a suite that
should have caught your analyzer's FP stayed green.

Check:

```bash
grep -rn "^namespace" src/Analyzers/*.cs | grep -v "namespace Moq.Analyzers"
```

Any hit is a bug (verified 2026-07-02: the command returns nothing at
commit 05135b2; `Resources.Designer.cs` uses block-scoped
`namespace Moq.Analyzers {`, which is fine).

## 10. Perf gate failure — real regression or tool bug?

**Pipeline** (all repo-root relative): `.github/workflows/main.yml` `perf` job
→ `build/scripts/perf/PerfCore.ps1 -v diag -diff -ci -filter <filter>` →
`DiffPerfToBaseline.ps1` (creates a git worktree at the baseline SHA recorded
in `build/perf/baseline.json`) → `RunPerfTests.ps1` twice (baseline + current)
→ `ComparePerfResults.ps1` → `dotnet run --project src/tools/PerfDiff -- --baseline ... --results ... --failOnRegression`.

**Filters:** PRs run the fast path `-filter '*(FileCount: 1)'`; the nightly
schedule (03:00 UTC) and `workflow_dispatch` with `run_performance=true` run
the full `'*'`. Setting env/input `FORCE_PERF_BASELINE=true`
(workflow input `force_baseline`; consumed at `PerfCore.ps1:83`) forces the
baseline to re-run instead of using the cached result.

**Verdict logic** (`src/tools/PerfDiff/BDN/BenchmarkComparisonService.cs`):
five strategies run over Mann-Whitney-compared benchmark pairs; **any one
firing fails the gate**. The strategy/threshold table AND the per-strategy
defect table (#1265–#1269 — a perf failure may be a tool bug; all open as of
2026-07-02) are canonical in **moq-analyzers-diagnostics-and-tooling §2**;
re-derive raw thresholds from
`grep -rn 'Threshold' src/tools/PerfDiff/BDN/Regression/`. This section owns
only the triage decision: real regression vs. tool bug.

**Decision procedure:**

1. Read the `perf` job log for the strategy line, e.g.
   `test: '<id>' P95 took 260.000 ms; worse than the threshold 250ms`.
2. Download the `performance` artifact; compare the same benchmark in
   `artifacts/performance/perfResults/baseline/results/*-report-github.md` vs
   `.../perfTest/results/*-report-github.md`.
3. If the *baseline* value already exceeds the budget and the diff didn't get
   meaningfully worse → **#1267 tool bug**: note it on the PR, link #1267; do
   not "fix" it by weakening your code or the thresholds.
4. If the failing strategy is ratio-based (35% / 5%) and baseline vs diff
   genuinely differ → treat as a real regression; reproduce locally:

   ```bash
   ./build/scripts/perf/CIPerf.sh -filter "'*(FileCount: 1)'"
   # equivalent to: pwsh build/scripts/perf/PerfCore.ps1 -v diag -diff -ci -filter "'*(FileCount: 1)'"
   ```

5. If the job crashed with a stack trace rather than a verdict → likely #1269;
   check for stale `full-compressed.json` files or a benchmark that produced
   no `Result`-stage measurements.

Baseline updates (`build/perf/baseline.json`) are change-controlled — see
moq-analyzers-change-control.

## 11. Test row count looks 4x — TestDataExtensions fan-out

Not a bug. `tests/Moq.Analyzers.Test/Helpers/TestDataExtensions.cs` multiplies
every `[MemberData]` row:

- `.WithNamespaces()` — x2: bare code and `namespace MyNamespace;`
- `.WithMoqReferenceAssemblyGroups()` — x2: Moq 4.8.2 (`Net80WithOldMoq`) and
  Moq 4.18.4 (`Net80WithNewMoq`)

So one authored row = 4 executed rows (2,000 authored rows ≈ 8,000 executed).
Suite total 2026-07-02: 3,357 tests in Moq.Analyzers.Test.

Debugging value: the group name is the **first** test argument. If only
`Net80WithOldMoq` rows fail, the behavior is Moq-4.8.2-specific (API missing
or shaped differently there — see section 5). Single-version data uses
`.WithOldMoqReferenceAssemblyGroups()` / `.WithNewMoqReferenceAssemblyGroups()`
deliberately; don't "upgrade" it to both without checking the API exists in
both versions.

## 12. Pre-push / pre-commit hook failures

Hooks are Husky.NET tasks defined in `.husky/task-runner.json` (verified
2026-07-02).

**pre-push** runs two tasks:

| Task | What it runs | Failure meaning / fix |
|---|---|---|
| `todo-scanner` | `pwsh build/scripts/todo-scanner/Scan-TodoComments.ps1 -FailOnUnlinked` | A `TODO`/`FIXME`/`HACK`/`UNDONE` comment without an issue link. Fix format: `TODO(#123): description`. Do NOT delete someone else's TODO to pass; file an issue and link it |
| `build-and-test` | `pwsh build/scripts/hooks/Invoke-PrePushBuild.ps1` — the CI-parity Release build (section 7 flags) + full test run | Same triage as sections 1–7. This is why "it built locally" is not evidence — the hook builds with PedanticMode |

History: TODO enforcement is a hook/CI-workflow concern only. Raising SonarQube
S1135 ("track TODOs") to a *build warning* self-locked CI under PedanticMode
(commit `3d4f7ff`, 2026-03-06; reverted the next day by `b1439ab`). The failing
comment linked issue #1012, which tracks an unrelated analyzer enhancement, not
this incident — see moq-analyzers-failure-archaeology §4. Never re-try that.

**pre-commit** tasks (staged files only): large-file detection, `dotnet format
--verify-no-changes`, markdownlint-cli2, yamllint, actionlint, JSON validation
(excludes `*.verified.json`), shellcheck. Run any of them manually with the
same commands from `task-runner.json`. Format failures: run `dotnet format`
and re-stage.

**PowerShell parse errors in hooks on Windows:** `*.ps1` files must be LF, not
CRLF (ADR-010; incident #1081). Check with `git ls-files --eol build/scripts`.

## When NOT to use this skill

| Need | Use instead |
|---|---|
| Install SDKs, first build, environment/PATH problems | moq-analyzers-build-and-env |
| What gates a PR must pass; editing AnalyzerReleases files; dependency pins | moq-analyzers-change-control |
| Author a new rule end-to-end (checklist) | moq-analyzers-rule-lifecycle |
| Roslyn API concepts (IOperation vs syntax, symbols, spans) | roslyn-analyzer-reference |
| Moq API semantics (Setup/Returns/Raise shapes, version diffs) | moq-api-reference |
| Systematic FP backlog burn-down + escape-hatch decision tree | moq-analyzers-fp-convergence-campaign |
| Live-DLL harness scripts and diagnostic tooling in depth | moq-analyzers-diagnostics-and-tooling |
| Past incidents in narrative depth (settled battles) | moq-analyzers-failure-archaeology |
| Writing tests/assertions strategy, QA gates, coverage | moq-analyzers-validation-and-qa |
| .editorconfig severities and analyzer config flags | moq-analyzers-config-and-flags |
| Proving a fix correct (corpora, mutation testing) | moq-analyzers-proof-toolkit |

## Provenance and maintenance

Re-verify before trusting anything volatile:

- STOP protocol wording: `grep -n "Expected span" .github/copilot-instructions.md`
- Span failure message templates: pinned by `Microsoft.CodeAnalysis.CSharp.CodeFix.Testing` version in `build/targets/tests/Packages.props` (1.1.2-beta1.24314.1 on 2026-07-02)
- Crash-surface line numbers: `sed -n 383p src/Analyzers/ConstructorArgumentsShouldMatchAnalyzer.cs`, `sed -n 60p src/Common/SemanticModelExtensions.cs`, `sed -n 643p src/Common/WellKnown/MoqKnownSymbols.cs` — drift when files are edited; the fixes are tracked in issues #1241–#1264
- Host-compat ceilings: `grep -n "MaxSystem" build/targets/packaging/Packaging.targets`
- Load-test matrix shape: `grep -n "tfm:" .github/workflows/main.yml` (9 entries on 2026-07-02)
- PedanticMode default: `sed -n 3,5p build/targets/codeanalysis/CodeAnalysis.targets`
- Perf thresholds: `grep -rn "Threshold.Parse" src/tools/PerfDiff/BDN/Regression/`
- Perf filter fast path: `grep -n "FileCount" .github/workflows/main.yml`
- PerfDiff defect issue states: check <https://github.com/rjmurillo/moq.analyzers/issues/1265> through /1269 (all open on 2026-07-02); once fixed, delete the "tool bug" branch of section 10
- AllAnalyzersVerifier namespace contract: `grep -n '"Moq.Analyzers"' tests/Moq.Analyzers.Test/Helpers/AllAnalyzersVerifier.cs`
- Hook task list: `cat .husky/task-runner.json`
- Test count (3,357): `dotnet test --settings ./build/targets/tests/test.runsettings` summary line
- Received-file rule: `grep -n "received" CONTRIBUTING.md`

Last verified: 2026-07-02 against commit 05135b2.
