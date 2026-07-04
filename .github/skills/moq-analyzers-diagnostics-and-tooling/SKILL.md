---
name: moq-analyzers-diagnostics-and-tooling
description: "Provides the measurement toolbox for moq.analyzers — load when you need to OBSERVE analyzer behavior or performance instead of guessing. Triggers: \"does this snippet trigger MoqXXXX?\", reproducing a false positive/negative outside the test suite, running/reading benchmarks and the PerfDiff perf gate, interpreting perf-gate failures, producing or reading binlogs, code-coverage files (Cobertura, SummaryGithub.md), SARIF output, unused-dependency checks (snitch), or verifying Moq's real API surface (dotnet-inspect). NOT for: writing/structuring tests or span assertions (moq-analyzers-validation-and-qa), statistical proof methodology built on these tools (moq-analyzers-proof-toolkit), build/SDK setup errors (moq-analyzers-build-and-env), or step-by-step bug diagnosis strategy (moq-analyzers-debugging-playbook)."
---

# Diagnostics and tooling: measure instead of eyeball

This project ships analyzers into mission-critical codebases. The costliest historical
failure mode is code that LOOKS correct but is wrong. The countermeasure is simple:
**never assert what an analyzer does — run it and paste the output.** This skill is the
catalog of instruments that produce that output, with interpretation guides.

All commands are repo-root relative. `dotnet` must be on PATH (in sandboxes:
`export PATH="$HOME/.dotnet:$PATH"`). Verified 2026-07-02 against commit `05135b2`.

## Quick reference

| Question | Instrument | Command (from repo root) |
|---|---|---|
| Does this C# snippet trigger diagnostic X? | Live-DLL harness | `.github/skills/moq-analyzers-diagnostics-and-tooling/scripts/run-analyzer-on-snippet.sh <file.cs> [moq-version]` |
| Did my change slow an analyzer down? | Benchmarks + PerfDiff | `./build/scripts/perf/CIPerf.sh -filter '*(FileCount: 1)'` |
| Why did MSBuild do that? | Binary log (binlog) | `dotnet build /bl:artifacts/logs/local.binlog` |
| What code do my tests exercise? | Coverage (Cobertura) | `dotnet test --settings ./build/targets/tests/test.runsettings` |
| Machine-readable list of every compiler/analyzer diagnostic? | SARIF | `dotnet build <proj> "/p:ErrorLog=out.sarif%2Cversion=2.1"` |
| Any unused NuGet references? | snitch | `dotnet tool restore && dotnet snitch --strict` |
| What does Moq's API ACTUALLY look like in version X? | dotnet-inspect | `dotnet-inspect member "IReturns<TMock, TResult>" --package Moq@4.8.2 --all` |

---

## 1. Live-DLL harness: run the built analyzers on any snippet

The single most valuable capability here. The test suite
(`tests/Moq.Analyzers.Test`) is the authority for regression protection, but when you
need a fast answer to "what does the analyzer report for THIS code, against a REAL Moq
reference?", use the harness script shipped in this skill:

```bash
.github/skills/moq-analyzers-diagnostics-and-tooling/scripts/run-analyzer-on-snippet.sh <snippet.cs> [moq-version]
```

What it does (details in the script header):

1. Builds a throwaway `net8.0` class-library project **outside the repo tree** so the
   repo's `Directory.Build.props`, Central Package Management, and PedanticMode cannot
   interfere with the snippet.
2. References the **real Moq package** (default 4.18.4; pass `4.8.2` for the old-Moq
   axis the test matrix uses). This matters: every analyzer's `Initialize` early-exits
   via `IsMockReferenced()` when Moq is absent, so a snippet without a Moq reference
   produces zero diagnostics no matter how wrong it is.
3. Wires in the same three DLLs the shipped nupkg puts in `analyzers/dotnet/cs`
   (`Moq.Analyzers.dll`, `Moq.CodeFixes.dll`,
   `Microsoft.CodeAnalysis.AnalyzerUtilities.dll`) from
   `artifacts/bin/Moq.Analyzers/<config>/` — i.e., you are testing the **built**
   analyzers, not a description of them. Rebuild first if you changed analyzer code:
   `dotnet build src/Analyzers/Moq.Analyzers.csproj`.
4. Generates an `.editorconfig` that raises every ID found in
   `src/Common/DiagnosticIds.cs` to `warning`, so Info-severity rules (Moq1400,
   Moq1410, ...) appear in build output instead of being silently dropped.
5. Prints every `MoqXXXX` diagnostic with file, **line, column, ID, and message** —
   plus any `CSxxxx` compiler errors so you can distinguish "no diagnostics" from
   "snippet didn't compile".

Environment knobs: `MOQ_ANALYZERS_CONFIG=release` (default `debug`),
`SNIPPET_TFM=<tfm>` (default `net8.0`), `SNIPPET_LANGVERSION=<ver>` (default `latest` — the
host SDK's newest stable C#, so modern-host repros such as the C# 13 `params`-collection
crash of #1241 actually parse; set `12` to mirror the pinned Roslyn 4.8 test compiler),
`SNIPPET_OUTPUTTYPE=Exe` (default unset = class library, correct for declaration-only
repros; set `Exe` when the snippet is top-level statements, else the SDK rejects it with
CS8805 and the harness exits 2 before analyzers can be trusted — but do not set it for a
declaration-only snippet, which would fail CS5001 for lack of a `Main`),
`KEEP_HARNESS=1` (keep the temp project for inspection). Exit codes: 0 = ran; 1 = setup
error; 2 = snippet has compile errors; **3 = an analyzer crashed (AD0001)**;
**4 = an analyzer or a dependency failed to LOAD (CS8032/CS8034)** — the #850 incident class,
where the analyzers never ran at all. Codes 3 and 4 are surfaced separately because Roslyn
reports both as warnings that would otherwise exit the build 0 and read as a clean snippet.

### Worked example (observed output, 2026-07-02)

`scripts/sample-moq1002.cs` in this skill dir is a known-positive: `new Mock<Foo>(1, true)`
where `Foo` only has a `(string)` constructor. Running:

```bash
.github/skills/moq-analyzers-diagnostics-and-tooling/scripts/run-analyzer-on-snippet.sh \
  .github/skills/moq-analyzers-diagnostics-and-tooling/scripts/sample-moq1002.cs
```

produced (paths shortened; ~7 s wall clock including NuGet restore):

```text
--- Moq.Analyzers diagnostics (config=debug, Moq 4.18.4, net8.0, C# latest) ---
Snippet.cs(30,20): warning Moq1400: Explicitly choose a mocking behavior for SnippetSample.Foo instead of relying on the default (Loose) behavior (https://github.com/rjmurillo/moq.analyzers/blob/05135b2.../docs/rules/Moq1400.md)
Snippet.cs(30,20): warning Moq1410: Explicitly set the Strict mocking behavior for 'SnippetSample.Foo' (https://github.com/rjmurillo/moq.analyzers/blob/05135b2.../docs/rules/Moq1410.md)
Snippet.cs(30,33): warning Moq1002: Could not find a matching constructor for type 'SnippetSample.Foo' with arguments (1, true) (https://github.com/rjmurillo/moq.analyzers/blob/05135b2.../docs/rules/Moq1002.md)
```

Read the columns: Moq1002 reports at line 30, column 33 — the **argument list** `(1, true)`,
not the whole `new` expression. Diagnostic spans are character-precise and pinned by
tests in this project; the harness is the fastest way to see where a span actually
lands before you write `{|Moq1002:...|}` markup.

A clean snippet prints `(none)`. A snippet with syntax errors prints the CS errors
first and exits 2 (observed for `new Mock<IFoo>(` with a missing paren). A snippet that
makes an analyzer throw prints an `ANALYZER CRASH (AD0001)` block and exits 3 — this is
the highest-priority outcome (Roslyn disables the crashing analyzer for the session), and
it is the case a naive grep for `MoqXXXX` would silently miss.

### Harness limitations (know these before trusting a result)

| Limitation | Consequence |
|---|---|
| Severities are forced to `warning` | Do not read severity off the output; check the descriptor or `docs/rules/MoqXXXX.md` for the shipped default |
| Code fixes are not exercised | Fixer behavior/crashes need the code-fix test infra (see moq-analyzers-validation-and-qa) |
| Compiles with the host SDK's current Roslyn (SDK 10.x) | Good for "modern host" behavior; the test suite pins Roslyn 4.8 (ADR-003), so parse-level differences (e.g. C# 13 params collections) can differ between harness and suite. Set `SNIPPET_LANGVERSION=12` to approximate the pinned test compiler |
| One file, `net8.0`, LangVersion `latest` by default | Multi-file/multi-TFM repros belong in the test suite; override the language version with `SNIPPET_LANGVERSION` |
| A result here is evidence, not a regression test | Every FP/FN fix still ships with an issue-linked test (repo rule) |

---

## 2. Benchmarks and PerfDiff: the performance gate

Definitions: **BenchmarkDotNet (BDN)** is the .NET micro-benchmarking framework;
benchmarks live in `tests/Moq.Analyzers.Benchmarks` (one file per rule). **PerfDiff**
(`src/tools/PerfDiff`) is this repo's console tool that compares two BDN result sets
and exits non-zero on regression. Its exit code is a **required PR merge gate**
(ADR-008: `docs/architecture/ADR-008-benchmarkdotnet-perfdiff-for-performance-regression-detection.md`).

### Running locally

```bash
# CI-equivalent run (Linux/macOS; Windows: build\scripts\perf\CIPerf.cmd).
# Expands to: PerfCore.ps1 -v diag -diff -ci  — requires pwsh (PowerShell 7).
./build/scripts/perf/CIPerf.sh -filter '*(FileCount: 1)'

# Same thing via PowerShell directly, with all knobs:
./build/scripts/perf/PerfCore.ps1 -v diag -diff -ci -filter '*(FileCount: 1)'
```

- `-filter '*(FileCount: 1)'` is the **PR fast path** — only the 1-file variant of each
  benchmark (each benchmark has `[Params(1, 1_000)] FileCount`). This is exactly what
  the `perf` job in `.github/workflows/main.yml` runs on PRs; the nightly schedule and
  `workflow_dispatch` with `run_performance=true` run the full `'*'`. Budget ~20 min
  for a first local run (baseline + current), much less once the baseline is cached.
- `FORCE_PERF_BASELINE=true` env var forces the baseline to be re-run instead of reusing
  the cached one.
- `sudo` is recommended (BenchmarkDotNet power-plan control); on Linux ETL tracing is
  auto-disabled with a warning (`-etl` is Windows-only).

The `-diff` chain, so you can debug any stage:
`CIPerf.sh` → `PerfCore.ps1` (reads `build/perf/baseline.json` for the baseline SHA)
→ `DiffPerfToBaseline.ps1` (creates a **git worktree** at that SHA, runs baseline
benchmarks there via `RunPerfTests.ps1`, then runs your tree's benchmarks)
→ `ComparePerfResults.ps1` → `dotnet run --project src/tools/PerfDiff -- --baseline
<dir> --results <dir> --failOnRegression`.

### Where the outputs land

| Path | Content |
|---|---|
| `artifacts/performance/perfResults/baseline/results/` | Baseline run (cached across runs for your branch) |
| `artifacts/performance/perfResults/perfTest/results/` | Current run (overwritten each run) |

Per benchmark class, four files prefixed with the fully qualified type
(e.g. `Moq.Analyzers.Benchmarks.Moq1000SealedClassBenchmarks`):
`-report-github.md` (the Markdown table to paste into PRs), `-report.html`,
`-report.csv`, and `-full-compressed.json` — the **raw measurements; the only file
PerfDiff reads**. In CI both trees are uploaded as the `performance` artifact, and the
`report-github.md` tables are appended to the workflow run's step summary.

### Reading the verdict

`BenchmarkComparisonService` (`src/tools/PerfDiff/BDN/BenchmarkComparisonService.cs`)
runs **five strategies; any single one reporting a regression fails the run** (exit 1
under `--failOnRegression`; exit 0 clean; exit 2 cancelled; file errors exit 1).

| Strategy (file in `src/tools/PerfDiff/BDN/Regression/`) | Metric | Regression when (2026-07-02) |
|---|---|---|
| `PercentageRegressionStrategy` | Median ratio | Mann-Whitney significance test says "worse" beyond **35%** threshold |
| `P95RatioRegressionStrategy` | P95 ratio | Mann-Whitney worse beyond **5%** AND absolute P95 delta > **0.5 ms** (dual gate — both must trip) |
| `MeanPercentageRegressionStrategy` | Mean ratio | Mann-Whitney worse beyond **5%** AND absolute mean delta > **0.5 ms** (dual gate) |
| `PercentileRegressionStrategy` | Absolute P95 | Diff run's P95 > **250 ms** budget (baseline not consulted — see defect table) |
| `MeanWallClockRegressionStrategy` | Absolute mean | Diff run's mean > **100 ms** budget (baseline not consulted) |

### Honesty note: PerfDiff has open correctness defects (all OPEN as of 2026-07-02)

Filed from a 2026-07-02 audit; until they land, interpret gate results with these in mind:

| Issue | Defect | How to interpret today |
|---|---|---|
| #1265 | **ETL veto**: `EtlDiffer.TryCompareETL` never sets its `regression` out-param, so if `*.etl.zip` traces exist in both folders, any real BDN regression is dismissed as "noise" → exit 0 | Latent on Linux CI (no ETW traces). Never add ETL traces to gate inputs until fixed; treat any "determined that it was noise" log line as a red flag |
| #1266 | **Silent intersection**: benchmarks missing from one side are dropped without a log; empty intersection → "No regressions" exit 0 | A green perf gate on a PR that touches the benchmark harness proves nothing — manually confirm `perfTest/results` contains the expected `*full-compressed.json` files with non-empty `"Benchmarks"` |
| #1267 | **Baseline-blind budgets**: the 250 ms/100 ms strategies compare the diff run's absolute value to the budget without reading the baseline — an unchanged-but-over-budget benchmark blocks CI forever; a 2× slowdown under budget counts as "better" | A P95/mean budget failure may not be YOUR regression: check whether baseline already exceeded the budget in `baseline/results` |
| #1268 | **Inf-ratio drops**: infinite median ratios (e.g. corrupted baseline `"Median": 0`) are filtered out of the worse-list — the most extreme regression possible is excluded from the verdict | If a baseline JSON looks corrupt, delete `artifacts/performance/perfResults/baseline` and re-run with `FORCE_PERF_BASELINE=true` |
| #1269 | **Input robustness**: empty measurement sets crash unactionably; single-sample sets pass silently; duplicate `FullName` from stale JSONs crash `ToDictionary`; `Operations == 0` poisons ranks | On any raw PerfDiff crash, first suspect stale/partial result files — clean `artifacts/performance` and re-run |

### Authoring a new benchmark (convention, traced from `Moq1000SealedClassBenchmarks.cs`)

One file per rule: `tests/Moq.Analyzers.Benchmarks/Moq{Id}{ShortName}Benchmarks.cs`.
Skeleton contract:

- Class attributes: `[InProcess]`, `[MemoryDiagnoser]`, `[BenchmarkCategory("Moq{Id}")]`.
- `[Params(1, 1_000)] public int FileCount { get; set; }` — the PR fast-path filter
  string `'*(FileCount: 1)'` depends on this exact parameter name.
- `[IterationSetup]` builds `FileCount` source files and calls
  `BenchmarkCSharpCompilationFactory.CreateAsync<TAnalyzer>(sources, referenceAssemblies)`
  (helpers in `tests/Moq.Analyzers.Benchmarks/Helpers/`), getting back a
  `(BaselineCompilation, TestCompilation)` pair — baseline has no analyzer attached.
  Reference assemblies come from `CompilationCreator.GetReferenceAssemblies("Net80WithOldMoq")`
  (same string keys as the test suite's `ReferenceAssemblyCatalog`).
- Two `[Benchmark]` methods: the analyzer one **throws if the diagnostic count ≠
  `FileCount`** (so a silently-broken analyzer fails the benchmark instead of measuring
  nothing), and a `[Benchmark(Baseline = true)]` one that throws if any diagnostic
  appears. Copy the assertion style verbatim.

---

## 3. Binlogs: MSBuild's flight recorder

A **binlog** (`*.binlog`) is a complete structured record of an MSBuild invocation —
every target, task, property, item, and diagnostic — far richer than console output.

- **CI**: the composite action `.github/actions/setup-restore-build/action.yml` builds
  with `/bl:./artifacts/logs/release/build.release.binlog`; the whole `./artifacts/logs`
  tree is uploaded as the **`binlogs`** artifact on every `main.yml` build (even on
  failure). Download it from the workflow run's Artifacts section when diagnosing a
  CI-only build difference.
- **Locally**: add `/bl:` to any build:

  ```bash
  dotnet build /bl:artifacts/logs/local.binlog
  ```

- **Opening one**: the GUI is MSBuild Structured Log Viewer
  (<https://msbuildlog.com>, `msbuildlog.exe` / `StructuredLogViewer.Avalonia`).
  Headless (verified 2026-07-02): replaying a binlog through MSBuild regenerates a full
  text log —

  ```bash
  dotnet msbuild artifacts/logs/local.binlog -flp:'v=diag;logfile=replay.log' -noconlog
  # then grep replay.log for the property/target/diagnostic you care about
  ```

Typical uses here: confirming which `PedanticMode` / `TreatWarningsAsErrors` values a
build actually used (`build/targets/codeanalysis/CodeAnalysis.targets` derives them from
`ContinuousIntegrationBuild` — the reason warnings pass locally but fail CI), and seeing
which analyzer DLLs were passed to csc.

---

## 4. Coverage: producing and reading Cobertura

**Cobertura** is an XML coverage format; **ReportGenerator** is the tool that merges raw
coverage files into human-readable reports.

Produce coverage (this is also just the standard test command):

```bash
dotnet test --settings ./build/targets/tests/test.runsettings
```

The runsettings file configures the Microsoft CodeCoverage collector: **only
`Moq.Analyzers.dll` and `Moq.CodeFixes.dll` are instrumented** (module include list);
property getters/setters, test methods, generated code, and
`[ExcludeFromCodeCoverage]` are excluded. Coverage of PerfDiff or test helpers is
intentionally not measured.

Pipeline (wired in `build/targets/tests/Tests.targets`, runs automatically after tests):

| Stage | Output |
|---|---|
| Collector emits raw files | `artifacts/TestResults/<tfm>/**/*.cobertura.xml` (one per run, GUID/timestamp dirs) |
| `GenerateCoverageReport` target (ReportGenerator, `ReportTypes=MarkdownSummaryGithub;Cobertura;HtmlInline`) | `artifacts/TestResults/coverage/Cobertura.xml` (merged), `SummaryGithub.md`, `index.html` |
| `CleanCoverageReport` target | Wipes the previous report before each run — the report always reflects ONLY the last `dotnet test` invocation |

That last row is the classic trap: run a single test with `--filter` and
`SummaryGithub.md` will show near-zero coverage. That is your filtered run, not a
coverage collapse. For a real number, run the full suite.

Reading: open `artifacts/TestResults/coverage/index.html` for per-class line/branch
detail, or `SummaryGithub.md` for the table CI appends to the workflow step summary
(`main.yml` "Publish coverage summary to GitHub"). CI also uploads
`artifacts/TestResults/coverage/**` as the `.NET Code Coverage Reports (<os>)` artifact
and pushes `Cobertura.xml` to Codacy and Qlty (Linux job only, secret-gated — skipped
on forks).

Quality-gate context: new analyzer code is held to **100% block coverage** (repo rule;
PR template asks for the coverage summary as evidence). Coverage tells you what was
*executed*, not what was *asserted* — for proving assertions bite, see
moq-analyzers-proof-toolkit (mutation-level reasoning) and moq-analyzers-validation-and-qa.

---

## 5. SARIF: machine-readable diagnostics — and an honest gap

**SARIF** (Static Analysis Results Interchange Format) is a JSON format the Roslyn
compiler can emit listing every diagnostic (compiler + analyzers) with rule ID,
severity, and location — ideal for diffing "what does the whole build report before vs.
after my change".

State of the repo (verified 2026-07-02): `main.yml` has an "Upload SARIF files" step
with path `./artifacts/obj/**/*.sarif`, **but nothing in the repo sets the compiler's
`ErrorLog` property, so no SARIF files exist and the artifact is never produced** — a
live 2026-07-02 CI run uploads `performance`, coverage/test reports, `artifacts`,
`packages`, and `binlogs`, and no SARIF artifact. Related unwired tool: SquiggleCop
(`dotnet-squigglecop` 1.0.26 is pinned in `.config/dotnet-tools.json` but has no
baseline file and no invocation anywhere). Treat both as dormant infrastructure, not as
a source of truth. (Candidate improvement, not current behavior: wiring `ErrorLog` +
SquiggleCop baselines would make analyzer-noise drift visible.)

Producing SARIF yourself (verified 2026-07-02; the `%2C` is an escaped comma so MSBuild
doesn't split the property value):

```bash
dotnet build src/Common/Common.csproj "/p:ErrorLog=/tmp/common.sarif%2Cversion=2.1"
python3 -c "import json;print(*sorted({r['ruleId'] for r in json.load(open('/tmp/common.sarif'))['runs'][0]['results']}),sep='\n')"
```

Note: for suppressed/hidden diagnostics the SARIF reflects the build's effective
severities (editorconfig, `NoWarn`), same as console output.

---

## 6. snitch: unused NuGet dependency detector

`snitch` (local tool, 2.0.0, pinned in `.config/dotnet-tools.json`) flags project
references and packages that are declared but unused, or that could move to a parent.
CI runs it in the build job (`main.yml` "Detect unused NuGet dependencies") and
`--strict` makes any finding fail the build — so run it before pushing a change that
adds/removes a `PackageReference`:

```bash
dotnet tool restore
dotnet snitch --strict   # from repo root; exit 0 = clean (observed: quiet output when clean)
```

Remember the repo rule: any NEWLY packed dependency also needs a
`THIRD-PARTY-NOTICES.TXT` entry (see moq-analyzers-change-control).

---

## 7. dotnet-inspect: verify Moq's real API surface

When analyzer logic depends on what Moq actually exposes (overload counts, containing
types, interface shapes), do not trust memory — inspect the package. This is how the
project discovered its "phantom symbols" (MoqKnownSymbols properties for `IReturns`
non-generic variants that don't exist in any Moq version) and the 4.8.2 vs 4.18.4
overload gaps.

```bash
dotnet tool install -g dotnet-inspect   # 0.16.0 verified 2026-07-02; on PATH via ~/.dotnet/tools
```

Verified example queries with observed results (2026-07-02):

```bash
# Members of a type in a SPECIFIC Moq version (--all is required for interface
# members / non-default visibility; without a @version pin you get LATEST Moq —
# 4.20.72 as of 2026-07-02 — which is NOT what this repo tests against):
dotnet-inspect member "IReturns<TMock, TResult>" --package Moq@4.8.2 --all
#   → Returns: 19 overloads (4.18.4 shows 20 — the addition is Returns(InvocationFunc);
#     the Returns(Delegate) catch-all exists in both versions)

# Where does a type live?
dotnet-inspect find "ItExpr" --package Moq@4.18.4
#   → ItExpr | Moq.Protected | class

# Other useful subcommands: type, diff (compare API surfaces across versions),
# extensions (extension methods for a type), implements, depends. --offline uses cache only.
```

Rules of thumb: always pin `@4.8.2` or `@4.18.4` (the two versions the test matrix
covers); when adding a `MoqKnownSymbols` entry, confirm the type/member exists in BOTH
(or document the version gate) and add a resolves-non-null test. Verified Moq surface
facts live in moq-api-reference — treat its tables as canonical; this section only
teaches the tool.

---

## When NOT to use this skill

| If you need... | Load instead |
|---|---|
| Test-suite structure, `{\|MoqXXXX:...\|}` markup, ReferenceAssemblyCatalog, Verify snapshots | moq-analyzers-validation-and-qa |
| Statistical/proof methodology (corpus runs, FP-rate claims) that consumes these tools' output | moq-analyzers-proof-toolkit |
| Build failures, SDK/global.json issues, PedanticMode, hooks, environment setup | moq-analyzers-build-and-env |
| A diagnosis strategy for a bug (which tool to point where, in what order) | moq-analyzers-debugging-playbook |
| Roslyn API concepts (operations, symbols, analyzer lifecycle) | roslyn-analyzer-reference |
| Moq behavior/semantics themselves | moq-api-reference |
| Adding/retiring a rule end-to-end (checklist incl. the benchmark step) | moq-analyzers-rule-lifecycle |
| Severity/editorconfig/flag configuration of shipped rules | moq-analyzers-config-and-flags |
| PR evidence requirements and merge gates as policy | moq-analyzers-change-control |
| Past incidents that explain why these gates exist | moq-analyzers-failure-archaeology |
| Analyzer architecture invariants (ADRs, KnownSymbols, banned APIs) | moq-analyzers-architecture-contract |
| Docs/rule-page authoring | moq-analyzers-docs-and-writing |
| The FP-fixing campaign or open research questions | moq-analyzers-fp-convergence-campaign, moq-analyzers-research-frontier, moq-analyzers-research-methodology |
| BCL/API design standards for public surface | dotnet-api-design-standards |

## Provenance and maintenance

- Harness still works: `.github/skills/moq-analyzers-diagnostics-and-tooling/scripts/run-analyzer-on-snippet.sh .github/skills/moq-analyzers-diagnostics-and-tooling/scripts/sample-moq1002.cs` → expect Moq1002 at (30,33) plus Moq1400/Moq1410 at (30,20).
- Packed analyzer DLL list still 3 files: `grep -n "PackagePath=\"analyzers/dotnet/cs\"" src/Analyzers/Moq.Analyzers.csproj`
- Rule ID list source: `grep -c '"Moq[0-9]' src/Common/DiagnosticIds.cs` (25 IDs as of 2026-07-02; Moq1209 reserved)
- PerfDiff thresholds: `grep -rn 'Threshold.Parse\|ThresholdValueNs' src/tools/PerfDiff/BDN/Regression/` (35%; 5%+0.5ms ×2; 250ms; 100ms as of 2026-07-02)
- Strategy set: `grep -n "new .*Strategy()" src/tools/PerfDiff/BDN/BenchmarkComparisonService.cs` (5 strategies)
- PerfDiff defect issues still open: check <https://github.com/rjmurillo/moq.analyzers/issues/1265> (and 1266–1269) — remove the honesty-note rows as they close
- Perf baseline SHA: `cat build/perf/baseline.json` (sha `0cbc088`, release 0.1.1 as of 2026-07-02)
- PR fast-path filter unchanged: `grep -n "FileCount: 1" .github/workflows/main.yml`
- Binlog path in CI: `grep -n "/bl:" .github/actions/setup-restore-build/action.yml`
- Coverage report types/paths: `grep -n "ReportTypes\|coverage" build/targets/tests/Tests.targets`
- SARIF still unwired (delete §5 gap paragraph if this starts matching): `grep -rn "ErrorLog" --include="*.props" --include="*.targets" --include="*.csproj" .` (no hits as of 2026-07-02); SquiggleCop still unwired: `grep -rln -i squigglecop --exclude-dir=.git --exclude-dir=artifacts .` (only `.config/dotnet-tools.json`)
- Tool pins: `cat .config/dotnet-tools.json` (snitch 2.0.0, squigglecop 1.0.26, reportgenerator 5.5.10 as of 2026-07-02); `dotnet-inspect --version` (0.16.0)
- Latest Moq on nuget.org drifts: re-run `dotnet-inspect member "IReturns<TMock, TResult>" --package Moq --all` and update the "latest = 4.20.72" note
- Benchmark file convention: `ls tests/Moq.Analyzers.Benchmarks/Moq1*Benchmarks.cs`

- Frontmatter stays parser-safe: `python3 -c "import yaml; print(len(yaml.safe_load(open('.github/skills/moq-analyzers-diagnostics-and-tooling/SKILL.md').read().split('---')[1])['description']))"` — expect the full description length, not an error or a truncated count

Last verified: 2026-07-02 against commit 05135b2.
