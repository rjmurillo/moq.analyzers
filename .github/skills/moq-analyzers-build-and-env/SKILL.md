---
name: moq-analyzers-build-and-env
description: Recreates the moq.analyzers development environment from scratch and gets to a green build and test run. Load this when setting up a fresh clone or container, installing the .NET SDK/runtime, fixing "SDK not found" / MSB4018 GetBuildVersion / "framework 'Microsoft.NETCore.App' version '8.0.0' was not found" errors, restoring pinned dotnet tools, installing or debugging git hooks (Husky.NET pre-commit/pre-push), running the first build or test, understanding artifacts/ layout, or diagnosing why a build passes locally but fails CI (PedanticMode). Do NOT load for CI workflow internals or perf-gate debugging (moq-analyzers-diagnostics-and-tooling), analyzer test authoring or QA gates (moq-analyzers-validation-and-qa), rule authoring (moq-analyzers-rule-lifecycle), or PR/commit process rules (moq-analyzers-change-control).
---

# Build and Environment Setup: from bare machine to green build+test

Goal state, verified 2026-07-02 on commit 05135b2:

- `dotnet build` → 0 warnings, 0 errors (~13 s incremental, longer first run).
- `dotnet test --settings ./build/targets/tests/test.runsettings` → 3,357 tests in
  Moq.Analyzers.Test + 4 in PerfDiff.Tests. All pass on a normal GitHub clone
  (2 environment-only failures in sandboxes — see "PackageTests failures" below).

All commands are repo-root relative. This repo builds a **Roslyn analyzer** — a plugin
DLL that runs inside other people's C# compilers and IDEs — so the build has unusual
constraints (pinned old Roslyn, netstandard2.0, host-compatibility checks). You do not
need to understand those to get a green build; they are covered by
`moq-analyzers-architecture-contract`.

## Quick reference

| Task | Command |
| --- | --- |
| Build (lenient, local) | `dotnet build` |
| Build (CI-parity, warnings = errors) | `dotnet build /p:PedanticMode=true` |
| Full test run | `dotnet test --settings ./build/targets/tests/test.runsettings` |
| Single test class | `dotnet test --settings ./build/targets/tests/test.runsettings --filter "FullyQualifiedName~CallbackSignature"` |
| Format check | `dotnet format --verify-no-changes` |
| Restore pinned local tools | `dotnet tool restore` |
| Attach git hooks manually | `dotnet husky install` (after `dotnet tool restore`) |
| Check SDK visible | `dotnet --list-sdks` (need 10.0.3xx) |
| Check runtimes visible | `dotnet --list-runtimes` (need Microsoft.NETCore.App 8.0.x) |
| Fix MSB4018 GetBuildVersion | `git fetch --unshallow` |

## 1. Prerequisites

You need exactly three things before the first build:

1. **git** with **full history** (not a shallow clone — see section 2).
2. **.NET SDK 10.0.301** (or a later 10.0.3xx patch). Pinned by `global.json`
   (`"version": "10.0.301", "rollForward": "latestPatch"` — a newer patch in the same
   feature band satisfies it; SDK 10.0.4xx or 11.x does not, `latestPatch` stays within
   10.0.3xx).
3. **.NET 8 runtime** in the same dotnet root. The shipped analyzer targets
   netstandard2.0, but the test projects (`tests/Moq.Analyzers.Test`,
   `tests/PerfDiff.Tests`) and the PerfDiff tool target **net8.0**. The SDK 10 install
   does NOT include an 8.0 runtime; without it, `dotnet test` fails with
   "The framework 'Microsoft.NETCore.App', version '8.0.0' ... was not found".

### Install on Linux / macOS

```bash
curl -sSL -o dotnet-install.sh https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --version 10.0.301          # SDK, installs to $HOME/.dotnet
./dotnet-install.sh --runtime dotnet --channel 8.0   # .NET 8 runtime, same root
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$HOME/.dotnet:$PATH"               # add to your shell profile
```

### Install on Windows (PowerShell)

```powershell
Invoke-WebRequest -Uri https://dot.net/v1/dotnet-install.ps1 -OutFile dotnet-install.ps1
./dotnet-install.ps1 -Version 10.0.301
./dotnet-install.ps1 -Runtime dotnet -Channel 8.0
# Installs to %LOCALAPPDATA%\Microsoft\dotnet by default; put that on PATH,
# or use the official SDK installer from https://dotnet.microsoft.com/download
```

### Verify

```bash
dotnet --list-sdks       # expect: 10.0.301 (or newer 10.0.3xx)
dotnet --list-runtimes   # expect a line: Microsoft.NETCore.App 8.0.x
```

Verified in this environment 2026-07-02: SDK `10.0.301`, runtimes
`Microsoft.NETCore.App 8.0.28` and `10.0.9`.

**Escape hatch if you cannot install the 8.0 runtime:** `DOTNET_ROLL_FORWARD=LatestMajor`
lets net8.0 test hosts run on the 10.x runtime. The pre-push hook
(`build/scripts/hooks/Invoke-PrePushBuild.ps1`) sets this itself. Prefer installing the
real 8.0 runtime: CI runs tests on an actual 8.0 runtime, and running on 10.x is not
what ships.

### Optional but required-for-hooks tools

The git hooks (section 4) shell out to external linters. Missing tools **fail the
hook** — they are not skipped. Per `CONTRIBUTING.md` "Getting Started":

```bash
npm install -g markdownlint-cli2      # Markdown lint
pipx install yamllint                 # YAML lint
# shellcheck: apt install shellcheck / brew install shellcheck
# actionlint: go install github.com/rhysd/actionlint/cmd/actionlint@latest
# pwsh (PowerShell 7+): https://aka.ms/powershell — required, hooks are .ps1
# python3 — used by the JSON-validation hook
```

## 2. Clone — the shallow-clone trap (MSB4018)

```bash
git clone https://github.com/rjmurillo/moq.analyzers.git
cd moq.analyzers
```

This repo versions itself with **Nerdbank.GitVersioning (NBGV)** — an MSBuild task that
computes the package version (`0.5.0-alpha.{height}` per `version.json`) from the
**number of commits** since the version was set. A shallow clone (`--depth`, the default
in many CI/agent sandboxes) does not contain that history, and the build fails with:

```text
error MSB4018: The "Nerdbank.GitVersioning.Tasks.GetBuildVersion" task failed unexpectedly.
```

Fix (documented in `.github/copilot-instructions.md` and used by
`.github/workflows/copilot-setup-steps.yml`):

```bash
git fetch --unshallow    # errors harmlessly if the clone is already full
```

Do this immediately after any clone you did not create yourself. `git rev-list --count HEAD`
should return several hundred (988 at commit 05135b2, 2026-07-02).

## 3. Restore pinned local tools

```bash
dotnet tool restore      # ~40 s cold, instant when cached
```

This installs the **local tool manifest** `.config/dotnet-tools.json` — version-pinned
CLI tools scoped to this repo (invoked as `dotnet <command>`). Contents as of 2026-07-02:

| Tool | Version | Command | Purpose |
| --- | --- | --- | --- |
| `husky` | 0.9.1 | `dotnet husky` | Husky.NET — installs/runs the git hooks defined in `.husky/task-runner.json` |
| `nbgv` | 3.10.85 | `dotnet nbgv` | Nerdbank.GitVersioning CLI — inspect/compute the version (`dotnet nbgv get-version`) |
| `verify.tool` | 0.7.0 | `dotnet dotnet-verify` | Reviews/accepts Verify snapshot files (`*.verified.*` vs `*.received.*`) used by PackageTests |
| `squigglecop.tool` | 1.0.26 | `dotnet dotnet-squigglecop` | SARIF diagnostic-baseline tool. **Pinned but NOT wired into any build step or CI job** (no baseline file, no invocation anywhere in the repo as of 2026-07-02) — do not expect it to run |
| `dotnet-reportgenerator-globaltool` | 5.5.10 | `dotnet reportgenerator` | Converts Cobertura coverage to HTML/Markdown; also invoked automatically by MSBuild after every `dotnet test` (`build/targets/tests/Tests.targets`, target `GenerateCoverageReport`) |
| `snitch` | 2.0.0 | `dotnet snitch` | Detects redundant transitive package references; CI runs `dotnet snitch --strict` in the build job |

You rarely invoke these directly; `dotnet build` and CI drive them. But `dotnet tool restore`
must succeed before hooks can attach.

## 4. Git hooks (Husky.NET)

**Husky.NET** is a .NET port of the JS "husky" tool: it points `core.hooksPath` at
`.husky/` so git runs the scripts there. Hook tasks are declared in
`.husky/task-runner.json`; `.husky/pre-commit` and `.husky/pre-push` each run
`dotnet husky run --group <group>`.

**Attachment is automatic:** the `HuskyInstall` target in `Directory.Build.targets` runs
`dotnet tool restore` + `dotnet husky install` after the first `dotnet restore`/`dotnet build`,
unless `ContinuousIntegrationBuild=true`, a design-time build, or `HUSKY=0` is set. A stamp
file `.husky/_/install.stamp` prevents re-runs. To attach manually or re-attach:

```bash
dotnet tool restore
dotnet husky install
git config core.hooksPath   # should print: .husky
```

(If `git config core.hooksPath` prints nothing, hooks are NOT attached — common in
sandboxes where the first build ran with CI flags.)

### What the hooks run and how long they take

| Group | Task | What it checks | Typical duration |
| --- | --- | --- | --- |
| pre-commit | `large-file-detection` | Staged files ≤ 500 KB and source files ≤ 1000 lines (`build/scripts/hooks/Test-LargeFiles.ps1`; extensions checked: .cs .ps1 .sh .yaml .yml .json .xml .md) | seconds |
| pre-commit | `dotnet-format` | `dotnet format Moq.Analyzers.sln --verify-no-changes` on staged .cs files | seconds–1 min |
| pre-commit | `markdownlint` | `markdownlint-cli2` on staged .md | seconds |
| pre-commit | `yamllint` | `yamllint --config-file .yamllint.yml` on staged .yml/.yaml | seconds |
| pre-commit | `actionlint` | `actionlint` on staged workflow files | seconds |
| pre-commit | `json-validate` | `build/scripts/hooks/Test-JsonValid.ps1` (calls python) on staged .json, excluding `*.verified.json` | seconds |
| pre-commit | `shellcheck` | `shellcheck` on staged .sh/.bash | seconds |
| pre-push | `todo-scanner` | `build/scripts/todo-scanner/Scan-TodoComments.ps1 -FailOnUnlinked` — every TODO/FIXME/HACK must reference an issue like `TODO(#123)` | seconds |
| pre-push | `build-and-test` | `build/scripts/hooks/Invoke-PrePushBuild.ps1` — full **Release** build with CI flags + **full test suite** | **several minutes** (Release rebuild + full suite; the test phase alone measured 2 m 13 s in this environment, 2026-07-02) |

Budget for the pre-push hook: it is deliberately the same gate as CI. Pushing a branch
is not instant in this repo. `git commit --no-verify` / `git push --no-verify` bypasses
hooks — CONTRIBUTING.md permits this **only for WIP commits**; CI enforces everything
anyway, so bypassing just moves the failure later.

## 5. First build

```bash
dotnet build
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)` for all projects, plus the nupkg
pack (`artifacts/package/debug/Moq.Analyzers.<version>.nupkg`). ~13 s incremental on a
pre-built tree; a cold build (restore + full compile) takes a few minutes.

### CI-parity build — why `dotnet build` alone is NOT enough before a PR

```bash
dotnet build /p:PedanticMode=true
```

`PedanticMode` is defined in `build/targets/codeanalysis/CodeAnalysis.targets`:

```xml
<PedanticMode Condition=" '$(PedanticMode)' == '' ">$([MSBuild]::ValueOrDefault('$(ContinuousIntegrationBuild)', 'false'))</PedanticMode>
<TreatWarningsAsErrors>$(PedanticMode)</TreatWarningsAsErrors>
<MSBuildTreatWarningsAsErrors>$(PedanticMode)</MSBuildTreatWarningsAsErrors>
```

Translation: locally PedanticMode defaults to **false** (warnings are warnings); in CI
`ContinuousIntegrationBuild=true` makes it **true** (every warning is an error). This is
the #1 "works on my machine, fails CI" cause in this repo. **Any warning in your local
build output will fail CI.**

The exact CI/pre-push invocation (from `build/scripts/hooks/Invoke-PrePushBuild.ps1`,
mirroring `.github/actions/setup-restore-build/action.yml`):

```bash
dotnet build Moq.Analyzers.sln --configuration Release --verbosity quiet \
  /p:PedanticMode=true /p:Deterministic=true /p:ContinuousIntegrationBuild=true \
  /p:UseSharedCompilation=false /p:BuildInParallel=false /nodeReuse:false
```

Warning: `/p:ContinuousIntegrationBuild=true` also suppresses the Husky auto-install
target — if your very first build used CI flags, attach hooks manually (section 4).

The build also runs `ValidateAnalyzerHostCompatibility`
(`build/targets/packaging/Packaging.targets`), which fails the pack if any bundled
dependency needs assemblies newer than a .NET 8 SDK host provides (the CS8032 incident,
issue #850). If you hit it, you touched dependency pins — stop and read
`moq-analyzers-architecture-contract` and `docs/dependency-management.md`.

## 6. First test run

```bash
dotnet test --settings ./build/targets/tests/test.runsettings
```

The `--settings` file matters: `build/targets/tests/test.runsettings` enables parallel
execution (`MaxCpuCount=0`) and Cobertura code coverage limited to the two shipping
assemblies (`Moq.Analyzers.dll`, `Moq.CodeFixes.dll`). After the run, an MSBuild target
automatically generates a coverage report at `artifacts/TestResults/coverage/`
(`SummaryGithub.md`, `Cobertura.xml`, `index.html`).

**Expected counts (2026-07-02, commit 05135b2):**

| Project | Tests | Notes |
| --- | --- | --- |
| tests/Moq.Analyzers.Test | **3,357** | analyzer + code-fix + package tests, net8.0 |
| tests/PerfDiff.Tests | **4** | tests for the perf-comparison tool |
| tests/Moq.Analyzers.Benchmarks | 0 | BenchmarkDotNet project, not a test project |

Counts grow with every rule/test PR — treat a *lower* count than an earlier run as a
red flag (a test project failed to build or an analyzer fell out of discovery), not a
higher one.

Run one class or one test while iterating:

```bash
dotnet test --settings ./build/targets/tests/test.runsettings \
  --filter "FullyQualifiedName~ConstructorArgumentsShouldMatchAnalyzerTests"
```

CI runs the same suite as
`dotnet test --no-build --configuration Release --settings ./build/targets/tests/test.runsettings`
after the CI-parity build (`.github/workflows/main.yml`, test job, ubuntu-24.04-arm +
windows-latest matrix).

### The 2 PackageTests failures in sandboxes (not a code bug)

`PackageTests.Baseline` (2 theory cases: main + symbols nupkg) is a **Verify snapshot
test**: it unpacks the built nupkg and diffs its manifest/contents against committed
`*.verified.*` files, scrubbing volatile fields with `ScrubNuspec()`. The scrubber
expects the nuspec `<repository url>` to be a `https://github.com/<owner>/...` URL —
which it derives from your git remote.

In sandboxes/proxies where `git remote get-url origin` is not a github.com URL (e.g. a
local proxy URL), the scrub misses and both cases fail with a snapshot mismatch. So:

- **3,355/3,357 + 4/4 passing with exactly these 2 failures = green** in such an
  environment (state this in any evidence you record).
- On a normal GitHub clone, all 3,357 must pass.
- Check with: `git remote get-url origin`.

These failures produce `*.received.*` files next to the `*.verified.*` baselines.
**Never commit `*.received.*` files** — they are failure artifacts, are NOT
git-ignored (verified 2026-07-02: no `received` pattern in `.gitignore`), and CI uploads
them as artifacts on failure for inspection (`main.yml` "Upload *.received.* files"
step). Delete them before staging: `git clean -n` first, then remove. If a snapshot
change is *intended* (you changed the package contents deliberately), review with
`dotnet dotnet-verify review` or manually move received → verified.

## 7. Artifacts layout

Everything build-generated goes under `artifacts/` (configured in
`build/targets/artifacts/Artifacts.props`; the whole tree is disposable —
`rm -rf artifacts` forces a clean build):

```text
artifacts/
├── bin/<Project>/debug|release/      # compiled outputs (e.g. bin/Moq.Analyzers/debug/netstandard2.0/)
├── obj/<Project>/                    # intermediate objects, restore caches
├── package/debug|release/            # Moq.Analyzers.<version>.nupkg + .symbols.nupkg (pack runs on every build)
├── TestResults/
│   ├── net8.0/                       # TRX logs + raw .cobertura.xml per run
│   ├── coverage/                     # generated report: SummaryGithub.md, Cobertura.xml, index.html
│   └── coveragehistory/              # ReportGenerator history (CI restores this across runs)
└── logs/release/build.release.binlog # MSBuild binary log (CI builds only; pass /bl:... to get one locally)
```

The version embedded in the nupkg name (e.g. `0.5.0-alpha.280.g05135b2010`) comes from
NBGV; check it with `dotnet nbgv get-version`.

## 8. IDE notes

- Solution file: `Moq.Analyzers.sln` (repo root). Works in VS 2022+, Rider, VS Code with
  C# Dev Kit. `Moq.Analyzers.lutconfig` configures VS Live Unit Testing.
- **Two different C# language versions are in play — do not confuse them:**
  1. *Repo source code* compiles with `<LangVersion>default</LangVersion>`
     (`build/targets/compiler/Compiler.props`) — i.e. the newest C# of SDK 10 — even in
     the netstandard2.0 analyzer projects (the Polyfill package backfills missing BCL APIs).
  2. *Test-case code* — the C# source **strings inside tests** — is compiled by the
     test harness using the pinned **Roslyn 4.8** compiler
     (`Microsoft.CodeAnalysis.CSharp 4.8` in `Directory.Packages.props`, per ADR-003).
     Roslyn 4.8 tops out at **C# 12**. C# 13+ syntax (e.g. `params ReadOnlySpan<T>`
     params collections) in a test snippet will not parse, no matter what the SDK
     supports. Plan test fixtures accordingly.
- First IDE open triggers a restore, which triggers Husky hook install (section 4).
- Line endings are governed by `.gitattributes` — do not fight it from the IDE
  (see traps table).

## 9. Known traps

| Trap | Symptom | Cause / rule | Fix |
| --- | --- | --- | --- |
| Shallow clone | `MSB4018 ... GetBuildVersion task failed` | NBGV needs full commit history to compute version height | `git fetch --unshallow` |
| Missing .NET 8 runtime | `dotnet test`: "framework 'Microsoft.NETCore.App', version '8.0.0' was not found" | Tests target net8.0; SDK 10 ships no 8.0 runtime | `./dotnet-install.sh --runtime dotnet --channel 8.0` (or `DOTNET_ROLL_FORWARD=LatestMajor` as a stopgap) |
| Warnings pass locally, fail CI | Green local build, red CI build job | `PedanticMode` defaults off locally, on in CI (`TreatWarningsAsErrors`) | Always run `dotnet build /p:PedanticMode=true` before pushing; pre-push hook does this for you |
| CRLF PowerShell scripts | Pre-push hook dies with a PowerShell **parse error** (block comment `<# #>` unterminated) | ADR-010 (`docs/architecture/ADR-010-eol-lf-for-powershell-files.md`, incident #1081): `*.ps1/psm1/psd1` MUST be LF; enforced via `.gitattributes` | Never override to CRLF; if a checkout predates the rule: `git rm --cached -r . && git reset --hard` re-normalizes |
| `*.received.*` files staged | Verify failure artifacts show up in `git status` | Not git-ignored by design (CI uploads them on failure) | Delete before commit; only commit `*.verified.*` when a snapshot change is intentional and reviewed |
| Large file / long file rejected | pre-commit fails on `large-file-detection` | Limits: 500 KB per file, 1000 lines per source file (`Test-LargeFiles.ps1` defaults) | Split the file; do not raise the limits to sneak a commit through |
| Adding a NuGet feed / package restore fails with NU1100-style "not found" | Restore can't find a package you added | `nuget.config` uses `<clear />` + **strict packageSourceMapping**: nuget.org gets `*`; two Azure DevOps feeds (`dotnet5`, `dotnet-tools`) are mapped ONLY to `System.CommandLine.Rendering` and the `Microsoft.CodeAnalysis.*.Testing*` packages | New packages must come from nuget.org, or you must extend the mapping explicitly (dependency change = change control; see `moq-analyzers-change-control`) |
| Bumping Roslyn/AnalyzerUtilities/Immutable pins | `ValidateAnalyzerHostCompatibility` build failure, or CS8032 for consumers | ADR-003/ADR-004 pins; comments in `Directory.Packages.props` explain the .NET 8 host ceiling | Don't. Read `moq-analyzers-architecture-contract` first |
| Hooks never attached | `git config core.hooksPath` empty; commits skip all checks | First build ran with `ContinuousIntegrationBuild=true`, or `HUSKY=0` | `dotnet tool restore && dotnet husky install` |
| Offline / proxied network | `dotnet restore` or `dotnet tool restore` hangs or 403s | Feeds are nuget.org + pkgs.dev.azure.com (both HTTPS); corporate proxies must allow both hosts and any proxy CA must be trusted by the machine | Configure `HTTPS_PROXY`/CA per your environment; do NOT add mirror feeds to `nuget.config` to work around it |
| Stale coverage numbers | `artifacts/TestResults/coverage` looks wrong | Each test run deletes and regenerates the report (`CleanCoverageReport` target) — a **filtered** run reports coverage for that filter only | Re-run the full suite before quoting coverage |

## 10. Verified end-to-end sequence (copy-paste)

The full path from bare Linux machine to green, verified 2026-07-02:

```bash
# 1. SDK + runtime
curl -sSL -o dotnet-install.sh https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --version 10.0.301
./dotnet-install.sh --runtime dotnet --channel 8.0
export DOTNET_ROOT="$HOME/.dotnet" PATH="$HOME/.dotnet:$PATH"
dotnet --list-sdks && dotnet --list-runtimes

# 2. Clone with full history
git clone https://github.com/rjmurillo/moq.analyzers.git
cd moq.analyzers
git fetch --unshallow || true

# 3. Tools + hooks
dotnet tool restore
dotnet husky install
git config core.hooksPath        # expect: .husky

# 4. Build (CI-parity so you see what CI sees)
dotnet build /p:PedanticMode=true

# 5. Test
dotnet test --settings ./build/targets/tests/test.runsettings
# ~2.5 min; expect 3,357 + 4 passing (minus the 2 PackageTests cases iff your
# origin remote is not a github.com URL — check: git remote get-url origin)
```

## When NOT to use this skill

- **CI workflow internals, binlogs, SARIF, perf-gate (PerfDiff) debugging** →
  `moq-analyzers-diagnostics-and-tooling`.
- **A build/test failure caused by analyzer code you just wrote** (span mismatches,
  RS2000 release-tracking errors, AD0001 crashes) → `moq-analyzers-debugging-playbook`.
- **Writing or reviewing tests, coverage gates, snapshot-test authoring** →
  `moq-analyzers-validation-and-qa`.
- **Adding/changing a rule end-to-end** (IDs, docs, AnalyzerReleases.md) →
  `moq-analyzers-rule-lifecycle`.
- **Commit/branch/PR/evidence requirements** → `moq-analyzers-change-control`.
- **Why the dependency pins exist and what must never change** →
  `moq-analyzers-architecture-contract`.
- **Roslyn API concepts** (what an analyzer even is, IOperation, symbols) →
  `roslyn-analyzer-reference`.
- **Config knobs of the shipped analyzers** (.editorconfig options, severities) →
  `moq-analyzers-config-and-flags`.

## Provenance and maintenance

Re-verify each volatile claim with one command before trusting it after 2026-07-02:

- SDK pin: `cat global.json` (10.0.301, rollForward latestPatch)
- Tool list + versions: `cat .config/dotnet-tools.json`
- SquiggleCop still unwired: `grep -rn -i squigglecop --include='*.yml' --include='*.targets' --include='*.props' --include='*.ps1' .` (empty = still unwired)
- Hook tasks: `cat .husky/task-runner.json`
- Pre-push flags: `cat build/scripts/hooks/Invoke-PrePushBuild.ps1`
- Large-file limits: `head -25 build/scripts/hooks/Test-LargeFiles.ps1`
- PedanticMode wiring: `sed -n 1,8p build/targets/codeanalysis/CodeAnalysis.targets`
- Test counts: `dotnet test --settings ./build/targets/tests/test.runsettings` (3,357 + 4 on 2026-07-02; grows over time)
- PackageTests scrubber behavior: `sed -n 28,44p tests/Moq.Analyzers.Test/PackageTests.cs` and `git remote get-url origin`
- Roslyn/AnalyzerUtilities pins: `grep -n 'CodeAnalysis' Directory.Packages.props`
- LF rule for ps1: `grep -n 'ps1' .gitattributes` and `docs/architecture/ADR-010-eol-lf-for-powershell-files.md`
- NuGet source mapping: `cat nuget.config`
- Version stem / NBGV: `cat version.json`; `dotnet nbgv get-version`
- Unshallow guidance still documented: `grep -n unshallow .github/copilot-instructions.md`
- CI parity commands: `sed -n 25,35p .github/actions/setup-restore-build/action.yml` and `grep -n 'dotnet test' .github/workflows/main.yml`

Last verified: 2026-07-02 against commit 05135b2.
