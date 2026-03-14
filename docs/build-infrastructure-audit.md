# Build Infrastructure Audit

Audit date: 2026-03-14. All findings from actual file contents, no assumptions.

---

## SDK and Global Configuration

### global.json
- **SDK**: 10.0.201, rollForward: latestPatch
- **MSBuild SDK**: `DotNet.ReproducibleBuilds.Isolated` 2.0.2

---

## Import Chain

### Directory.Build.props (evaluation order)
1. `build/targets/artifacts/Artifacts.props`
2. `build/targets/compiler/Compiler.props`
3. `build/targets/reproducible/Reproducible.props` (imports `DotNet.ReproducibleBuilds.Isolated` SDK + `DotNet.ReproducibleBuilds` PackageReference)
4. `build/targets/versioning/Versioning.props`
5. `build/targets/tests/Tests.props`
6. `build/targets/codeanalysis/CodeAnalysis.props`

### Directory.Build.targets (evaluation order)
1. `build/targets/compiler/Compiler.targets`
2. `build/targets/reproducible/Reproducible.targets`
3. `build/targets/versioning/Versioning.targets`
4. `build/targets/packaging/Packaging.targets`
5. `build/targets/tests/Tests.targets`
6. `build/targets/codeanalysis/CodeAnalysis.targets`
7. `build/targets/artifacts/Artifacts.targets`

---

## Package-by-Package Audit

### 1. DotNet.ReproducibleBuilds (2.0.2) -- PackageReference

**Source**: `build/targets/reproducible/Packages.props`, referenced in `Reproducible.props`

**Props set** (`DotNet.ReproducibleBuilds.props`):
- `DebugType` = `embedded` (if not already set)
- **`ContinuousIntegrationBuild` = `true`** -- AUTO-DETECTED from environment variables:
  - `CI == 'true'` (generic, set by GitHub Actions)
  - `GITHUB_ACTIONS == 'true'`
  - `TF_BUILD == 'true'` (Azure Pipelines)
  - `APPVEYOR`, `TRAVIS`, `CIRCLECI`, `CODEBUILD_BUILD_ID+AWS_REGION`, `BUILD_ID+BUILD_URL` (Jenkins), `BUILD_ID+PROJECT_ID` (Google Cloud), `TEAMCITY_VERSION`, `JB_SPACE_API_URL`

**Targets set** (`DotNet.ReproducibleBuilds.targets`):
- `PublishRepositoryUrl` = `true` (if not set)
- `RepositoryBranch` auto-detected from CI env vars (GITHUB_REF, BUILD_SOURCEBRANCH, etc.)
- MSBuild version check: warns if MSBuild < 17.8.0

**Affects**: Both restore and build. Props imported during evaluation; targets during build.

---

### 2. DotNet.ReproducibleBuilds.Isolated (2.0.2) -- MSBuild SDK

**Source**: `global.json` msbuild-sdks, imported via `<Sdk Name="..."/>` in `Reproducible.props`

**Sdk.props set**:
- `AssemblySearchPaths` -- restricted to avoid machine-specific dependencies
- `TargetFrameworkRootPath` = `[UNDEFINED]` outside VS (isolates from OS registry)
- `DisableImplicitNuGetFallbackFolder` = `true`
- `DisableImplicitLibraryPacksFolder` = `true`
- Registers `ValidateGlobalJsonSdkVersion` task

**Sdk.targets set**:
- `_ReproducibleBuildsValidateGlobalJsonSdkVersion` target (BeforeTargets=PrepareForBuild)

**Key difference from non-Isolated**: The Isolated version does NOT set `ContinuousIntegrationBuild` or `Deterministic`. It focuses on build isolation (restricting assembly search paths, disabling fallback folders, validating SDK version). The non-Isolated PackageReference version handles CI detection and reproducibility properties.

**Affects**: Both restore and build (SDK props imported very early).

---

### 3. Nerdbank.GitVersioning (3.9.50)

**Source**: `Directory.Packages.props`, referenced via `Versioning.props`

**Props set** (`Nerdbank.GitVersioning.props`):
- `NBGV_CacheMode` = `MSBuildTargetCaching` (default)
- `NBGV_CachingProjectReference`

**Targets set** (`Nerdbank.GitVersioning.targets`):
- Suppresses SDK-generated version attributes:
  - `GenerateAssemblyInformationalVersionAttribute` = `false`
  - `GenerateAssemblyVersionAttribute` = `false`
  - `GenerateAssemblyFileVersionAttribute` = `false`
- `GetBuildVersion` target: computes `AssemblyVersion`, `FileVersion`, `InformationalVersion`, `PackageVersion`, `BuildVersion` etc. from git history
- `SetCloudBuildVariables` target: sets CI-specific build number variables
- Does NOT set `ContinuousIntegrationBuild`, `Deterministic`, or `TreatWarningsAsErrors`

**Affects**: Build only (version computation).

---

### 4. Polyfill (9.20.0)

**Source**: `build/targets/compiler/Packages.props`, referenced in `Compiler.props` with `PrivateAssets=all`

**What it does**: Ships C# source files as contentFiles that provide polyfills for newer .NET APIs on older target frameworks. Examples include `FilePolyfill.cs`, `InterlockedPolyfill.cs`, `EnvironmentPolyfill.cs`, string interpolation support.

**Targets set** (`Polyfill.targets`):
- Adds `DefineConstants` based on TFM capabilities (e.g., `FEATUREVALUETUPLE`, `FEATUREMEMORY`, `FEATUREASYNCINTERFACES`, `FEATUREHTTP`, etc.)
- Conditionally includes polyfill source files based on target framework
- Sets `PolyPublic`/`PolyUseEmbeddedAttribute` for visibility control

**Does NOT set**: `ContinuousIntegrationBuild`, `Deterministic`, `TreatWarningsAsErrors`

**Affects**: Build only (source compilation).

---

### 5. Code Analysis Packages

All in `build/targets/codeanalysis/Packages.props`:

| Package | Version | Purpose |
|---------|---------|---------|
| Meziantou.Analyzer | 3.0.23 | General C# analyzer |
| Microsoft.CodeAnalysis.Analyzers | 4.14.0 | Roslyn analyzer development rules |
| Microsoft.CodeAnalysis.BannedApiAnalyzers | 4.14.0 | Banned API enforcement |
| Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers | 3.11.0-beta1.26075.3 | Perf-sensitive code analysis |
| Roslynator.Analyzers | 4.15.0 | Extended Roslyn analysis |
| StyleCop.Analyzers | 1.2.0-beta.556 | Style enforcement |
| SonarAnalyzer.CSharp | 10.21.0.135717 | SonarQube rules |
| Microsoft.VisualStudio.Threading.Analyzers | 17.14.15 | Threading best practices |
| ExhaustiveMatching.Analyzer | 0.5.0 | Exhaustive switch/match |
| EffectiveCSharp.Analyzers | 0.2.0 | Effective C# rules |

None of these set `ContinuousIntegrationBuild`. They produce diagnostics that become errors when `TreatWarningsAsErrors=true`.

---

### 6. Test Packages

All in `build/targets/tests/Packages.props`:

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.NET.Test.Sdk | 18.3.0 | Test platform |
| xunit | 2.9.3 | Test framework |
| xunit.runner.visualstudio | 3.1.5 | VS test adapter |
| Meziantou.Xunit.ParallelTestFramework | 2.3.0 | Parallel test execution |
| coverlet.msbuild | 8.0.0 | Code coverage |
| ReportGenerator | 5.5.4 | Coverage reports |
| Microsoft.CodeAnalysis.CSharp.CodeFix.Testing | 1.1.2-beta1.24314.1 | Analyzer test infrastructure |
| Verify.Nupkg | 3.0.1 | NuGet package verification |
| Verify.Xunit | 31.12.5 | Snapshot testing |

---

### 7. Public API / Roslyn Packages

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.CodeAnalysis.CSharp.Workspaces | 4.8 | Roslyn workspace APIs |
| Microsoft.CodeAnalysis.CSharp | 4.8 | Roslyn compiler APIs |
| Microsoft.CodeAnalysis.AnalyzerUtilities | 3.3.4 | Analyzer utilities (pinned low for host compat) |

---

### 8. Other Packages

| Package | Version | Purpose |
|---------|---------|---------|
| BenchmarkDotNet | 0.15.8 | Performance benchmarking |
| GetPackFromProject | 1.0.10 | NuGet pack support |
| System.CommandLine | 2.0.0-beta1.21216.1 | CLI parsing |
| Newtonsoft.Json | 13.0.4 | JSON serialization |
| Perfolizer | 0.6.1 | BenchmarkDotNet dependency |
| Microsoft.Extensions.Logging | 10.0.5 | Logging abstractions |
| Microsoft.Diagnostics.Tracing.TraceEvent | 3.1.30 | ETW tracing |

---

### 9. Transitive Pins

| Package | Version | Reason |
|---------|---------|--------|
| System.Collections.Immutable | 8.0.0 | Host compat ceiling (.NET 8 SDK) |

---

## Critical Property Flow

### ContinuousIntegrationBuild

```
DotNet.ReproducibleBuilds.props (during evaluation)
  -> Detects GITHUB_ACTIONS=true (or CI=true)
  -> Sets ContinuousIntegrationBuild=true

action.yml (CLI override)
  -> /p:ContinuousIntegrationBuild=true on both restore AND build

CodeAnalysis.targets (during build)
  -> PedanticMode = ValueOrDefault(ContinuousIntegrationBuild, 'false')
  -> TreatWarningsAsErrors = PedanticMode
  -> MSBuildTreatWarningsAsErrors = PedanticMode
```

### Deterministic

```
action.yml -> /p:Deterministic=true (build step only, NOT restore)
```

Note: `Deterministic` is NOT set by DotNet.ReproducibleBuilds in v2.0.2. The SDK sets it to `true` by default since .NET SDK 8+.

---

## Critical Answer: Is /p:ContinuousIntegrationBuild=true Redundant?

**YES, the explicit `/p:ContinuousIntegrationBuild=true` in action.yml is redundant for the build step** on GitHub Actions. `DotNet.ReproducibleBuilds` v2.0.2 auto-detects `GITHUB_ACTIONS=true` and sets `ContinuousIntegrationBuild=true` during property evaluation.

**However, the `/p:ContinuousIntegrationBuild=true` on the restore step is NOT redundant.** During `dotnet restore`, NuGet evaluates MSBuild properties to determine conditional PackageReferences and imports. The DotNet.ReproducibleBuilds PackageReference props are only available AFTER restore completes (chicken-and-egg). Passing `/p:ContinuousIntegrationBuild=true` during restore ensures any restore-time conditionals see the correct value.

**Practical recommendation**: Keep both `/p:ContinuousIntegrationBuild=true` flags. The restore flag is necessary. The build flag is defense-in-depth (explicit > implicit) and costs nothing.

**The `/p:Deterministic=true` on the build step is also redundant** for .NET SDK 8+, which defaults `Deterministic=true`. It is defense-in-depth.

---

## Property Summary Table

| Property | Set By | Value | Phase |
|----------|--------|-------|-------|
| ContinuousIntegrationBuild | DotNet.ReproducibleBuilds (auto) | true on CI | Evaluation |
| ContinuousIntegrationBuild | action.yml /p: | true | Restore + Build |
| Deterministic | .NET SDK default | true | Evaluation |
| Deterministic | action.yml /p: | true | Build |
| PedanticMode | CodeAnalysis.targets | = ContinuousIntegrationBuild | Build targets |
| TreatWarningsAsErrors | CodeAnalysis.targets | = PedanticMode | Build targets |
| MSBuildTreatWarningsAsErrors | CodeAnalysis.targets | = PedanticMode | Build targets |
| DebugType | DotNet.ReproducibleBuilds | embedded | Evaluation |
| PublishRepositoryUrl | DotNet.ReproducibleBuilds | true | Build targets |
| RepositoryBranch | DotNet.ReproducibleBuilds | from GITHUB_REF | Build targets |
| DisableImplicitNuGetFallbackFolder | Isolated SDK | true | Evaluation |
| DisableImplicitLibraryPacksFolder | Isolated SDK | true | Evaluation |
| GenerateAssemblyVersionAttribute | NBGV | false | Build targets |
| GenerateAssemblyFileVersionAttribute | NBGV | false | Build targets |
| GenerateAssemblyInformationalVersionAttribute | NBGV | false | Build targets |
| UseSharedCompilation | action.yml /p: | false | Build |
| BuildInParallel | action.yml /p: | false | Build |
