---
name: moq-analyzers-config-and-flags
description: Catalogs every configuration axis in moq.analyzers — MSBuild properties (PedanticMode, ContinuousIntegrationBuild, LangVersion, AnalysisMode), analyzer severity layers (.editorconfig, .globalconfig, stylecop.json, GlobalSuppressions.cs, pragmas), BannedSymbols.txt, test.runsettings, CI env vars (FORCE_PERF_BASELINE, RUN_FULL_PERF, DOTNET_ROLL_FORWARD), renovate.json pins, build/perf/baseline.json, version.json/NBGV, and dotnet-tools.json. Load when a build behaves differently locally vs CI, when a warning appears/disappears unexpectedly, when adding/changing any flag or config file, when a diagnostic must be suppressed or re-severitied, when a dependency pin blocks an update, or when perf baseline/env-var semantics are unclear. Do NOT load for build/test command basics (use moq-analyzers-build-and-env), for dependency-pin incident history (moq-analyzers-failure-archaeology), for PR/merge process (moq-analyzers-change-control), or for shipping/retiring a rule (moq-analyzers-rule-lifecycle).
---

# moq.analyzers configuration and flags catalog

Every knob in this repo, what it defaults to, who may change it, and what
catches a bad change. This repo ships a Roslyn analyzer (a plugin that runs
inside consumers' compilers and IDEs), so several "configuration" entries are
actually load-bearing safety pins — changing them can crash customer builds.

All file paths are repo-root relative. All quotes verified against the working
tree on 2026-07-02 (commit `05135b2`).

Change control baseline: `CODEOWNERS` is `* @rjmurillo` — every file below is
owner-reviewed. Nothing here documents a way around that; see
`moq-analyzers-change-control` before touching any axis marked LOAD-BEARING.

## Axis map (read this first)

| # | Axis | File(s) | Blast radius if wrong |
|---|------|---------|----------------------|
| 1 | MSBuild properties | `build/targets/*/*.props`, `build/targets/codeanalysis/CodeAnalysis.targets`, `Directory.Build.props/.targets` | Local build passes, CI fails (or vice versa) |
| 2 | Analyzer severity layers | `.editorconfig` (root + 3 nested), `build/targets/codeanalysis/.globalconfig`, `stylecop.json`, `GlobalSuppressions.cs` | Warnings silently vanish, or CI self-locks |
| 3 | Banned APIs | `src/BannedSymbols.txt` | Wrong Roslyn API patterns re-enter the codebase |
| 4 | Test run settings | `build/targets/tests/test.runsettings`, `Tests.targets`, `xunit.runner.json` | Coverage numbers lie; tests serialize |
| 5 | CI/env variables | `.github/workflows/main.yml`, `build/scripts/hooks/Invoke-PrePushBuild.ps1`, `build/scripts/perf/PerfCore.ps1` | Perf gate runs wrong suite; pre-push breaks |
| 6 | Renovate rules | `renovate.json` | CS8032 crash ships to customers (LOAD-BEARING) |
| 7 | Perf baseline | `build/perf/baseline.json` | Perf gate compares against wrong commit |
| 8 | Versioning | `version.json`, `global.json` | Wrong package version pushed to nuget.org |
| 9 | Pinned CLI tools | `.config/dotnet-tools.json` | Hooks/CI tool steps fail or drift |

---

## Axis 1 — MSBuild properties

MSBuild properties are name/value pairs evaluated during `dotnet build`. This
repo centralizes them in `build/targets/<area>/{*.props,*.targets}` pairs,
imported by root `Directory.Build.props` (props) and `Directory.Build.targets`
(targets), which MSBuild auto-imports into every project.

### PedanticMode — the single most important flag

Exact text of `build/targets/codeanalysis/CodeAnalysis.targets` (entire file,
2026-07-02):

```xml
<Project>
  <PropertyGroup Label="Computed properties">
    <PedanticMode Condition=" '$(PedanticMode)' == '' ">$([MSBuild]::ValueOrDefault('$(ContinuousIntegrationBuild)', 'false'))</PedanticMode>
    <TreatWarningsAsErrors>$(PedanticMode)</TreatWarningsAsErrors>
    <MSBuildTreatWarningsAsErrors>$(PedanticMode)</MSBuildTreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

Semantics:

- `PedanticMode` defaults to whatever `ContinuousIntegrationBuild` is
  (`false` when unset). It drives BOTH `TreatWarningsAsErrors` (C# compiler
  warnings become errors) and `MSBuildTreatWarningsAsErrors` (MSBuild-level
  warnings become errors).
- CI always builds with `/p:ContinuousIntegrationBuild=true` (see
  `.github/actions/setup-restore-build/action.yml`, Build step), so **CI is
  always pedantic**. A plain local `dotnet build` is NOT.
- Consequence: **any warning passes locally and fails CI.** Always run the
  CI-parity build before pushing:

```bash
dotnet build Moq.Analyzers.sln --configuration Release /p:PedanticMode=true /p:Deterministic=true /p:ContinuousIntegrationBuild=true /p:UseSharedCompilation=false /p:BuildInParallel=false /nodeReuse:false
```

(That is verbatim the flag set from `build/scripts/hooks/Invoke-PrePushBuild.ps1`,
which the pre-push git hook runs for you.)

### Full property table

| Property | Value | Set in | Why |
|----------|-------|--------|-----|
| `PedanticMode` | default = `$(ContinuousIntegrationBuild)`, else `false` | `build/targets/codeanalysis/CodeAnalysis.targets:3` | Warnings-as-errors switch (above) |
| `TreatWarningsAsErrors` / `MSBuildTreatWarningsAsErrors` | `$(PedanticMode)` | same file | Escalation |
| `ContinuousIntegrationBuild` / `Deterministic` | not set in repo files; passed as `/p:` by CI composite action and pre-push hook | `.github/actions/setup-restore-build/action.yml`, `build/scripts/hooks/Invoke-PrePushBuild.ps1` | Reproducible builds (DotNet.ReproducibleBuilds 2.0.5, `build/targets/reproducible/`) |
| `ArtifactsPath` | `$(RepoRoot)/artifacts` | `build/targets/artifacts/Artifacts.props` | All bin/obj/test/package output lands under `artifacts/` |
| `ArtifactsTestResultsPath` | `$(ArtifactsPath)/TestResults` | same file | TRX + coverage output root |
| `LangVersion` | `default` | `build/targets/compiler/Compiler.props` | Deliberately floats with the SDK pinned in `global.json`. Do NOT hard-pin a C# version; the shipped assembly still compiles against Roslyn 4.8 (ADR-003) |
| `Nullable` | `enable` | same file | Repo-wide nullable reference types |
| `ImplicitUsings` | `enable` | same file | Global usings on |
| `EnableNETAnalyzers` | `true` | `build/targets/codeanalysis/CodeAnalysis.props` | Built-in CA rules on |
| `EnforceCodeStyleInBuild` | `true` | same file | IDExxxx style rules fail the build (under PedanticMode) |
| `AnalysisMode` | `preview` | same file | Most aggressive built-in analyzer mode, including preview rules |
| `WarningLevel` | `9999` | same file | All current and future compiler warning waves enabled |
| `GenerateDocumentationFile` | `true` | same file | XML docs required (missing-doc warnings become CI errors) |
| `EnforceExtendedAnalyzerRules` | `true`, only for projects `Moq.Analyzers` and `Moq.Analyzers.Test` | same file, conditioned on `$(MSBuildProjectName)` | Opts into analyzer-authoring rules (resolves RS1036); the RS-family rules police analyzer correctness |
| `RepoRoot` | `$(MSBuildThisFileDirectory)` | `Directory.Build.props` | Used by every other path |
| `ManagePackageVersionsCentrally` + `CentralPackageTransitivePinningEnabled` | `true` | `Directory.Packages.props` | Central Package Management (CPM): every package version lives in `Directory.Packages.props` or an imported `build/targets/*/Packages.props`; transitive dependencies are pinned too (ADR-005) |
| `HUSKY` | env var; `0` skips git-hook install | `Directory.Build.targets` `HuskyInstall` target | Hooks auto-install on restore unless CI or `HUSKY=0` |

- Who may change: repo owner via PR. Property changes are infrastructure
  changes — cite the affected `build/targets/` file in the PR.
- Guard: CI-parity build (pre-push hook + `main.yml` build job). There is no
  guard that detects *loosening* (e.g., lowering `WarningLevel`) other than
  code review — treat any diff under `build/targets/` as high-scrutiny.

### The one property trap with an incident behind it (2026-07-02)

Never configure S1135 ("track TODO tags") above `suggestion`. Under
PedanticMode, `S1135 = warning` turns every TODO comment into a CI error and
locks CI against itself (commit `3d4f7ff` did this and was reverted the next
day by `b1439ab`; the tripping build error was an issue-linked `TODO(#1012)`
comment — #1012 itself tracks an unrelated enhancement, not this incident;
see moq-analyzers-failure-archaeology §4). The current, correct setting is
`.editorconfig:420` → `dotnet_diagnostic.S1135.severity = suggestion`.
TODO discipline is enforced by a different mechanism entirely
(`build/scripts/todo-scanner/Scan-TodoComments.ps1`, pre-push).

---

## Axis 2 — Analyzer severity configuration (the precedence story)

"Severity" is how loudly a diagnostic (e.g. `CA1016`, `SA1101`, `Moq1200`)
reports: `error` > `warning` > `suggestion` > `silent` > `none`. Multiple
files can set it; precedence, strongest first:

| Level | Mechanism | This repo's instances |
|-------|-----------|----------------------|
| 1 | In-source: `#pragma warning disable <ID>` / `[SuppressMessage]` (incl. `GlobalSuppressions.cs`) | `src/Analyzers/GlobalSuppressions.cs` and `tests/Moq.Analyzers.Test/GlobalSuppressions.cs` — each suppresses exactly one rule: SA1503 (braces). Inline `[SuppressMessage]` also appears in code with written justifications (e.g. MA0051 in the MockBehavior analyzers) |
| 2 | `.editorconfig` `dotnet_diagnostic.<ID>.severity` — nearest file to the source wins; deeper folders override shallower | Root `.editorconfig` (422 lines, `root = true`) plus three nested overrides: `tests/.editorconfig`, `src/tools/.editorconfig`, `src/tools/PerfDiff/.editorconfig` |
| 3 | Global AnalyzerConfig (`.globalconfig`) — loses to any `.editorconfig` entry for the same ID | `build/targets/codeanalysis/.globalconfig`, wired via `<GlobalAnalyzerConfigFiles>` in `CodeAnalysis.props:17` |
| 4 | The rule's own `DiagnosticDescriptor` default (`defaultSeverity`, `isEnabledByDefault`) | Each analyzer source file in `src/Analyzers/` |

On top of whichever severity wins, `PedanticMode=true` escalates every
surviving `warning` to `error` (Axis 1). There are **no `<NoWarn>` properties
and no .ruleset files** anywhere in this repo (verified 2026-07-02) — do not
introduce them; the layers above are the sanctioned mechanisms.

### What each file is for

- **Root `.editorconfig`** — the main severity ledger plus formatting rules.
  Notable pinned entries (2026-07-02): `SA1633 = silent` (no file headers),
  `SA1101 = silent` (no `this.` prefix), `SA1600 = silent`, `CA1016 = none`
  (line 407 — Codacy false positive, suppressed deliberately per incident
  `58924f7`/`2a7ee34`; do not "fix" it), `MA0040 = error`, `CA2016 = error`
  (CancellationToken forwarding — load-bearing for analyzer responsiveness),
  `S1135 = suggestion` (NEVER raise; see Axis 1 trap), `MA0026 = none`.
  It also carries the ADR-010 section forcing `end_of_line = lf` for
  `*.ps1/psm1/psd1` (a CRLF PowerShell file once broke the pre-push hook,
  issue #1081) and a section protecting Verify snapshot files
  (`*.{received,verified}.*`) from newline/whitespace mangling.
- **`tests/.editorconfig`** (`root = false`, layers on top of root) — relaxes
  XML-doc rules (CS1573/CS1591/CS1712/SA1601 → `suggestion`), `CA1016 = none`,
  `MA0051 = suggestion` (method length), `VSTHRD200 = none` (no Async suffix
  on tests).
- **`src/tools/.editorconfig`** and **`src/tools/PerfDiff/.editorconfig`** —
  relax house style for PerfDiff, which was imported from the dotnet team's
  roslyn repo and is intentionally kept close to upstream.
- **`build/targets/codeanalysis/.globalconfig`** — `is_global=true`; contains
  ONLY the 19 EffectiveCSharp analyzer rules `ECS0100`–`ECS1900`, all set to
  `warning`. Its own header comment states the policy: prefer `.editorconfig`;
  use `.globalconfig` only for diagnostics not tied to a source file.
- **`build/targets/codeanalysis/stylecop.json`** — StyleCop *behavioral*
  settings (not severities): require newline at EOF, System usings first,
  usings outside namespace. Injected into every project as an
  `<AdditionalFiles>` item by `CodeAnalysis.props:16`.
- **Meta-analyzer packages** (what produces all these IDs): 10 packages listed
  in `CodeAnalysis.props` — Meziantou (MAxxxx), Microsoft.CodeAnalysis.Analyzers
  (RSxxxx), Roslynator (RCSxxxx), BannedApiAnalyzers (RS0030),
  PerformanceSensitiveAnalyzers (HAAxxxx), StyleCop (SAxxxx), Sonar (Sxxxx),
  VS Threading (VSTHRDxxx), ExhaustiveMatching, EffectiveCSharp (ECSxxxx).
  Versions pinned in `build/targets/codeanalysis/Packages.props`.

### Configuring THIS project's shipped rules (Moq1xxx) in consumer code

The documented consumer-facing suppression story (template in every
`docs/rules/Moq*.md`, e.g. `docs/rules/Moq1200.md:39-58`):

```csharp
#pragma warning disable Moq1200   // single instance
...
#pragma warning restore Moq1200
```

```ini
# file/folder/project scope, in .editorconfig
[*.{cs,vb}]
dotnet_diagnostic.Moq1200.severity = none
```

Default severities of shipped rules live in each analyzer's
`DiagnosticDescriptor` and in the `docs/rules/README.md` table — changing one
is a rule-lifecycle event (release-notes impact via
`AnalyzerReleases.Unshipped.md`), not a config edit. See
`moq-analyzers-rule-lifecycle`.

- Who may change severity files: owner-reviewed PR. Loosening a severity
  (warning → suggestion/none) needs written justification in the PR body.
- Guard: PedanticMode CI build catches *tightening* mistakes instantly
  (new errors). Nothing but review catches *loosening* — the repo pins a
  local tool for severity-baseline drift (SquiggleCop 1.0.26 in
  `.config/dotnet-tools.json`) but it is **not wired to any build step or CI
  job** (verified 2026-07-02) — drift detection is an open gap, not a working
  guard. Do not claim SquiggleCop protection in PRs.

---

## Axis 3 — BannedSymbols.txt

`Microsoft.CodeAnalysis.BannedApiAnalyzers` reads
`src/BannedSymbols.txt` (wired as `<AdditionalFiles>` in
`src/Directory.Build.props:5`, so it applies to everything under `src/`) and
raises `RS0030` on any use of a listed symbol — which PedanticMode turns into
a build error.

Current bans (2026-07-02) and what to use instead:

| Banned | Use instead | Why |
|--------|-------------|-----|
| `Microsoft.CodeAnalysis.Diagnostic.Create(...)` — all 7 overloads | `DiagnosticExtensions.CreateDiagnostic` (`src/Common/DiagnosticExtensions.cs`) | One consistent, allocation-aware call path for reporting |
| `Compilation.GetTypeByMetadataName(string)` and `GetTypesByMetadataName(string)` | `MoqKnownSymbols` / `KnownSymbols` (`src/Common/WellKnown/`) | Per-compilation cached symbol resolution (ADR-006); ad-hoc metadata-name lookups in callbacks are both slow and a string-matching smell (ADR-001) |

Entry format (documentation-comment ID syntax):
`M:Fully.Qualified.Method(Param.Types);Optional message`.

To add a ban: append a line to `src/BannedSymbols.txt`, build with
PedanticMode, and fix every new RS0030 hit in the same PR. To *remove* a ban
you need an ADR-level justification — both current bans encode settled
architecture doctrine.

- Guard: RS0030 at build time; violations cannot merge (CI pedantic build).

---

## Axis 4 — test.runsettings and test-run knobs

`dotnet test --settings ./build/targets/tests/test.runsettings` is the
canonical invocation (CI and pre-push both use it). Knobs inside
(`build/targets/tests/test.runsettings`, verified 2026-07-02):

| Knob | Value | Effect |
|------|-------|--------|
| `RunConfiguration/MaxCpuCount` | `0` | Use all processors for parallel test execution |
| Coverage `Format` | `cobertura` | Cobertura XML emitted per run |
| `IncludeTestAssembly` | `False` | Test DLLs excluded from coverage |
| `ModulePaths/Include` | `.*Moq\.Analyzers\.dll$` and `.*Moq\.CodeFixes\.dll$` | **Coverage counts ONLY the two shipped assemblies.** If a new shipped assembly is added, add its module here or its coverage is invisible |
| `ModulePaths/Exclude` | test adapters, xunit | noise removal |
| `ExcludeByAttribute` | `Obsolete,GeneratedCodeAttribute,CompilerGeneratedAttribute` (+ `ExcludeFromCodeCoverage` via Attributes) | standard exclusions |
| `SkipAutoProps` | `true` | auto-properties don't count against coverage |
| `Functions/Exclude` | `^get_.*`, `^set_.*`, `^.*Test.*`, `^Microsoft\.Testing.*`, `^System\.Diagnostics.*` | property accessors and test-infra functions excluded |

Post-processing: `build/targets/tests/Tests.targets` writes TRX logs to
`artifacts/TestResults/<tfm>/` and runs ReportGenerator
(`MarkdownSummaryGithub;Cobertura;HtmlInline`) into
`artifacts/TestResults/coverage/`, cleaning stale coverage first
(`CleanCoverageReport` target) so old runs never pollute a report.

xUnit parallelism: `build/targets/tests/xunit.runner.json` sets
`parallelizeAssembly: true` and `parallelizeTestCollections: true`, copied
into test output by `Tests.props`.

- Who may change: owner-reviewed PR. The 100%-block-coverage-for-new-analyzer-
  code gate (CONTRIBUTING.md) depends on the Include list being correct.
- Guard: coverage summary is required PR evidence
  (`moq-analyzers-change-control`); Codacy/Qlty uploads (Axis 5) surface drops.

---

## Axis 5 — CI and environment variables

All verified against `.github/workflows/main.yml` and scripts, 2026-07-02.

| Variable | Where set | Semantics |
|----------|-----------|-----------|
| `RUN_FULL_PERF` | `main.yml:462` (perf job env) | `true` only when (a) the nightly `schedule` run (`cron: '0 3 * * *'`, `main.yml:27`) on `main`, or (b) `workflow_dispatch` with input `run_performance=true`. When true the benchmark filter is `'*'` (full suite, 1,000-file corpus); otherwise the PR fast path `'*(FileCount: 1)'` (`main.yml:511-515`). PRs therefore get a fast, lower-power perf gate; the nightly run is the statistically strong one |
| `FORCE_PERF_BASELINE` | `main.yml:463` from dispatch input `force_baseline`; consumed at `build/scripts/perf/PerfCore.ps1:83` (`$env:FORCE_PERF_BASELINE -eq 'true'`) | Forces baseline benchmarks to re-run even when a cached `*report-full-compressed.json` exists under `artifacts/performance/perfResults/baseline/results`. Set it locally (`FORCE_PERF_BASELINE=true`) when you suspect a stale cached baseline |
| `IS_CODACY_COVERAGE_ALLOWED` / `IS_QLTY_COVERAGE_ALLOWED` | `main.yml:187-188`, computed as `secrets.CODACY_PROJECT_TOKEN != ''` / `secrets.QLTY_COVERAGE_TOKEN != ''` | Secret-presence gates: coverage uploads to Codacy/Qlty run only on Linux test legs AND only when the secret exists (`main.yml:257,267`). Forks without secrets skip cleanly instead of failing — mimic this pattern for any new secret-dependent step |
| `DOTNET_ROLL_FORWARD=LatestMajor` | `build/scripts/hooks/Invoke-PrePushBuild.ps1:15` | Lets tests targeting `net8.0` execute under the installed newer SDK/runtime (repo SDK is 10.0.301) during the pre-push hook. Set the same var manually if `dotnet test` complains about a missing 8.0 runtime and installing .NET 8 isn't an option |
| `HUSKY=0` | consumed by `Directory.Build.targets` `HuskyInstall` target | Skips git-hook installation on restore (also auto-skipped when `ContinuousIntegrationBuild=true` or design-time builds) |
| `RUN_PERF` dispatch inputs | `main.yml:4-15` | `run_performance` (bool) and `force_baseline` (bool) are the only two `workflow_dispatch` inputs |

- Guard: `actionlint` runs pre-commit on workflow files and in the linters CI
  job; workflow changes additionally require `gh act -n` dry-run evidence in
  the PR body (CONTRIBUTING.md).

---

## Axis 6 — renovate.json: the LOAD-BEARING dependency rules

Renovate is the automated dependency updater (`enabledManagers`:
`nuget`, `github-actions`). Several rules exist because violating them ships
a broken analyzer (CS8032 = "analyzer cannot be loaded" in the consumer's
compiler — incident #850, hotfix release v0.4.1). Verbatim highlights,
2026-07-02:

| Rule | Setting | Reason |
|------|---------|--------|
| `ignoreDeps` | `Microsoft.CodeAnalysis.CSharp`, `.CSharp.Workspaces`, `.Common`, `.Workspaces.Common` | Roslyn is pinned to **4.8** (ADR-003: minimum supported host is VS 2022 17.8 / .NET 8 SDK). Renovate must never bump it; raising it is a support-matrix decision |
| `System.Collections.Immutable`, `System.Reflection.Metadata` | `allowedVersions: "<=8.0.0"`, `automerge: false`, label `analyzer-compat` | The shipped DLL must bind against what a .NET 8 host provides (issue #850) |
| `Microsoft.CodeAnalysis.AnalyzerUtilities` | `allowedVersions: "<4.14.0"`, `automerge: false`, label `analyzer-compat` | 4.14.0+ pulls System.Collections.Immutable 9.0.0.0 → CS8032 in .NET 8 hosts (ADR-004) |
| `System.Formats.Asn1` | `automerge: false`, label `analyzer-compat` | Transitive pin for the shipped analyzer; manual review |
| `Perfolizer` | `enabled: false` | BenchmarkDotNet declares the **exact** constraint `[0.6.1]`; independent bumps cause NU1608. Update only when BenchmarkDotNet moves (PR #968) |
| `BenchmarkDotNet` | `automerge: false`, label `benchmark-tooling` | Must be reviewed together with the Perfolizer pin |
| `System.CommandLine`, `System.CommandLine.Rendering` | `enabled: false` | Updates require PerfDiff code changes (issue #914); currently 2.0.3 |
| Automerge policy | dev deps minor/patch, lockfile maintenance, AND production minor/patch (`matchCurrentVersion: "!/^0/"`) all automerge; `platformAutomerge: true` | **Under active policy review: issue #1271 (OPEN, 2026-07-02)** proposes removing unattended automerge for production NuGet deps because the shipped DLL executes inside consumers' compilers. Until decided, do not widen automerge; do not implement #1271's plan without the maintainer decisions it calls out |

Non-shipped projects (PerfDiff, Benchmarks, PerfDiff.Tests) float above the
central pins via `VersionOverride` in their `.csproj` — that is sanctioned
(CONTRIBUTING.md "Dependency Management") because those assemblies never ship
in the NuGet package.

- Who may change: owner only; `analyzer-compat`-labeled bumps require manual
  compatibility review.
- Guards (triple-layered, all verified):
  1. `ValidateAnalyzerHostCompatibility` MSBuild target
     (`build/targets/packaging/Packaging.targets:20`) — build **error** if
     System.Collections.Immutable or System.Reflection.Metadata resolve above
     major version 8 for a shipped project.
  2. `tests/Moq.Analyzers.Test/AnalyzerAssemblyCompatibilityTests.cs`.
  3. The 9-way `analyzer-load-test` CI matrix in `main.yml` (net8/9/10 CLI +
     net472/48/481 MSBuild) which greps output for CS8032 / binding failures.
- Validation for renovate.json edits:
  `npx --package renovate -- renovate-config-validator` (documented in #1271).

---

## Axis 7 — Perf baseline file

`build/perf/baseline.json` (entire file, 2026-07-02):

```json
{
  "release": "0.1.1",
  "label": "v.0.1.1",
  "sha": "0cbc08871f74af6c029536745fb4f4135d6361ea"
}
```

Semantics: the perf CI job (ADR-008; required PR check) checks out `sha` into
a second working tree, runs the benchmark suite there to produce the
*baseline*, runs the same suite on your PR, and PerfDiff
(`src/tools/PerfDiff`) compares the two with `--failOnRegression`. The
pipeline: `PerfCore.ps1 -diff` → `DiffPerfToBaseline.ps1` → `RunPerfTests.ps1`
×2 → `ComparePerfResults.ps1` → PerfDiff. `main.yml` fails fast if the file is
missing ("Get baseline SHA" step).

How/when to advance it:

- Advance by PR that rewrites all three fields to a newer **tagged release**
  (`release` = version, `label` = tag label, `sha` = full commit SHA of that
  tag). History shows exactly this pattern (`git log --oneline --
  build/perf/baseline.json`: `fb1fb8a "Update baseline.json to release
  0.1.1"`). There is no automation for advancing it (verified 2026-07-02).
- Advance when the baseline becomes so old that comparisons measure toolchain
  drift instead of your change, or after an intentional, accepted performance
  change. The current baseline (0.1.1) is many releases old — if you observe
  baseline-build failures in the perf job (e.g., the old SHA needs a
  different SDK; the job installs the baseline's own `global.json` SDK for
  this reason), advancing the baseline is the fix, but it is a maintainer
  decision: it resets the reference point for every future regression check.
- Guards: an invalid `sha` fails the perf job's baseline checkout loudly.
  Note (2026-07-02): the PerfDiff comparison math itself has audited holes
  (empty-benchmark-set false pass, absolute-budget strategies, Inf-ratio
  exclusion — issues #1265–#1269), so do not treat a green perf gate as proof
  when you have touched the benchmark harness. See
  `moq-analyzers-debugging-playbook` for diagnosing perf-gate behavior.

---

## Axis 8 — version.json / Nerdbank.GitVersioning (NBGV)

NBGV computes the package version from git history — no version numbers in
csproj files. `version.json` (verified 2026-07-02):

- `"version": "0.5.0-alpha.{height}"` — `{height}` is the count of commits
  since the version stem last changed, so every commit gets a unique
  prerelease version automatically.
- `publicReleaseRefSpec`: `main`, `v*` branches, `release/vX.Y.Z` branches,
  and `vX.Y.Z[-alpha|beta|rc[.N]]` tags produce *public* (non-suffixed)
  versions; everything else gets a git-hash suffix.
- `nugetPackageVersion.semVer: 2`; `cloudBuild.buildNumber.enabled: true`.

The NBGV MSBuild task is referenced in `build/targets/versioning/Versioning.props`;
the pinned CLI tool is `nbgv 3.10.85` (Axis 9).

- Who may change: owner, and only as part of a release train (bumping the stem
  is "open next version for development"). Release promotion also touches
  `AnalyzerReleases.Shipped.md` — see `moq-analyzers-rule-lifecycle`.
- Guards: `release.yml` verifies the packed nupkg version equals the release
  tag before `dotnet nuget push` (release-event runs). Known gap (audit
  finding D-7, Info): a `workflow_dispatch` release run skips that
  verification.
- Gotcha: NBGV needs full history. `MSB4018` in `GetBuildVersion` → run
  `git fetch --unshallow`.

---

## Axis 9 — .config/dotnet-tools.json (pinned local CLI tools)

Restored by `dotnet tool restore` (CI composite action retries it 3×; the
`HuskyInstall` target runs it locally). All entries have `rollForward: false`
— exact versions only. Verified 2026-07-02:

| Tool | Version | Used by |
|------|---------|---------|
| `nbgv` | 3.10.85 | Versioning (Axis 8) |
| `verify.tool` | 0.7.0 | Reviewing/accepting Verify snapshot files (`dotnet dotnet-verify`) |
| `squigglecop.tool` | 1.0.26 | **TRAP: pinned but NOT wired anywhere** — no baseline YAML, no invocation in any target/workflow (verified 2026-07-02). Intended for analyzer-severity-baseline drift; currently inert |
| `dotnet-reportgenerator-globaltool` | 5.5.10 | Coverage report CLI (the MSBuild ReportGenerator package is pinned to the same 5.5.10 in `build/targets/tests/Packages.props`) |
| `snitch` | 2.0.0 | `dotnet snitch --strict` in the CI build job (`main.yml:102`) — fails on redundant transitive PackageReferences |
| `husky` | 0.9.1 | Git hook runner (pre-commit/pre-push groups in `.husky/task-runner.json`) |

- Guard: CI runs `dotnet tool restore`; a bad version fails every job at
  setup. Version bumps arrive via Renovate (nuget manager covers tool
  manifests) under the Axis 6 policy.

---

## How to add a new configuration axis (checklist)

Use this when introducing any new flag, config file, or env var. The repo's
pattern is: *a paired props/targets folder under `build/targets/`, a default
that is safe locally, an explicit value in CI, and a guard that fails loudly.*

1. [ ] **Placement.** MSBuild property → new or existing
   `build/targets/<area>/{<Area>.props,<Area>.targets}` pair (props for
   values, targets for logic; keep the unused half present with the
   "Intentionally empty" comment, as `Compiler.targets` does). Package version
   → `Directory.Packages.props` or the area's `Packages.props` (CPM, ADR-005;
   never a version attribute in a csproj unless it's a sanctioned
   `VersionOverride` in a non-shipped project). Severity → `.editorconfig`
   (preferred) or `.globalconfig` (only if not tied to a source file — the
   `.globalconfig` header says so).
2. [ ] **Wire the import.** Props into `Directory.Build.props`, targets into
   `Directory.Build.targets`, `Packages.props` into `Directory.Packages.props`
   — follow the existing import lists.
3. [ ] **Choose the default for the *local* case** and make CI set the strict
   value explicitly (the PedanticMode pattern:
   `Condition=" '$(X)' == '' "` fallback + `/p:X=true` in CI/pre-push). Never
   make the local default the strict one if it would block iteration.
4. [ ] **Add a guard.** What fails, and where, if someone sets it wrong?
   Acceptable guards in this repo: an MSBuild `<Error>` target
   (`ValidateAnalyzerHostCompatibility` is the template), a test
   (`AnalyzerAssemblyCompatibilityTests` is the template), or a CI matrix leg.
   "Review will catch it" is not a guard.
5. [ ] **If it's a dependency pin, encode it in renovate.json too** (cap +
   `automerge: false` + label), and validate with
   `npx --package renovate -- renovate-config-validator`.
6. [ ] **Document it**: CONTRIBUTING.md section if contributors must know it;
   `docs/dependency-management.md` for dependency rules; an ADR
   (`docs/architecture/ADR-*.md`) if it encodes an architectural constraint.
7. [ ] **Prove CI-parity**: run the pre-push CI-parity build + test locally
   and paste output as PR evidence (see `moq-analyzers-change-control`).
8. [ ] **If it's an env var in a workflow**: pass untrusted values via `env:`
   indirection (never inline `${{ }}` into `run:`), gate secret-dependent
   steps on secret presence (the `IS_CODACY_COVERAGE_ALLOWED` pattern), and
   run `actionlint` + `gh act -n`.
9. [ ] **Update this skill** and its Provenance section.

---

## When NOT to use this skill

- Build/test/run command basics, SDK install, artifacts layout →
  `moq-analyzers-build-and-env`.
- Why a pin exists (incident narratives: CS8032/#850, S1135 CI self-lock,
  AI config-corruption cascade) → `moq-analyzers-failure-archaeology`.
- Diagnosing a failing CI job or a perf-gate mystery →
  `moq-analyzers-debugging-playbook`; PerfDiff internals →
  `moq-analyzers-diagnostics-and-tooling`.
- PR process, evidence requirements, merge rules →
  `moq-analyzers-change-control`.
- Adding/shipping/retiring a Moq1xxx rule (AnalyzerReleases files, docs
  pages, default severities of shipped rules) →
  `moq-analyzers-rule-lifecycle`.
- Writing analyzers/tests themselves → `roslyn-analyzer-reference`,
  `moq-analyzers-architecture-contract`, `moq-analyzers-validation-and-qa`.
- .NET API design of public surface → `dotnet-api-design-standards`.

## Provenance and maintenance

Re-verify each axis with one command before trusting this document:

- PedanticMode wiring: `cat build/targets/codeanalysis/CodeAnalysis.targets`
- Compiler/analysis props: `cat build/targets/compiler/Compiler.props build/targets/codeanalysis/CodeAnalysis.props`
- Artifacts path: `cat build/targets/artifacts/Artifacts.props`
- CI build flags: `grep -n "dotnet build" .github/actions/setup-restore-build/action.yml build/scripts/hooks/Invoke-PrePushBuild.ps1`
- Severity files inventory: `find . -name ".editorconfig" -o -name ".globalconfig" -o -name "GlobalSuppressions.cs" -o -name "stylecop.json" | grep -v artifacts`
- Notable severities: `grep -n "dotnet_diagnostic" .editorconfig tests/.editorconfig`
- No NoWarn/ruleset crept in: `grep -rn "NoWarn\|\.ruleset" --include="*.props" --include="*.targets" --include="*.csproj" src tests build Directory.Build.props`
- Banned APIs: `cat src/BannedSymbols.txt && grep -n BannedSymbols src/Directory.Build.props`
- Runsettings knobs: `grep -n "MaxCpuCount\|ModulePath\|Format" build/targets/tests/test.runsettings`
- CI env vars: `grep -n "RUN_FULL_PERF\|FORCE_PERF_BASELINE\|IS_CODACY\|IS_QLTY" .github/workflows/main.yml`
- Pre-push roll-forward: `grep -n DOTNET_ROLL_FORWARD build/scripts/hooks/Invoke-PrePushBuild.ps1`
- Renovate pins: `grep -n "allowedVersions\|enabled\|automerge\|ignoreDeps" renovate.json`
- Automerge policy issue still open? `gh issue view 1271 --repo rjmurillo/moq.analyzers --json state`
- Central pins (Roslyn 4.8, AnalyzerUtilities 3.3.4, Perfolizer 0.6.1, System.CommandLine 2.0.3): `grep -n "CodeAnalysis.CSharp\"\|AnalyzerUtilities\|Perfolizer\|System.CommandLine\"" Directory.Packages.props`
- Host-compat guard: `grep -n "ValidateAnalyzerHostCompatibility\|_MaxSystem" build/targets/packaging/Packaging.targets`
- Perf baseline: `cat build/perf/baseline.json && git log --oneline -3 -- build/perf/baseline.json`
- Version stem: `cat version.json global.json`
- Pinned tools (and whether SquiggleCop got wired): `cat .config/dotnet-tools.json && grep -rn squigglecop .github build Directory.Build.targets --include="*.yml" --include="*.targets" --include="*.ps1"`
- Hook tasks: `cat .husky/task-runner.json`

Last verified: 2026-07-02 against commit 05135b2.
