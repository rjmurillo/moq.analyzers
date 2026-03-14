# Dependency Management

This project uses **Renovate** as the sole dependency update bot. Renovate manages both NuGet packages and GitHub Actions. Dependabot configuration has been removed. GitHub may still open Dependabot security alert PRs automatically.

## Package Categories

Dependencies fall into distinct categories with different upgrade policies.

### Shipped (in analyzer nupkg) - EXTREME CAUTION

These packages are bundled in the analyzer NuGet package and run inside the **user's** compiler host (Visual Studio, dotnet CLI). Version constraints are critical because users may run older SDKs.

| Package                                  | Central Pin                  | Constraint                                                        |
| ---------------------------------------- | ---------------------------- | ----------------------------------------------------------------- |
| Microsoft.CodeAnalysis.CSharp            | 4.8                          | Minimum supported VS/SDK version                                  |
| Microsoft.CodeAnalysis.CSharp.Workspaces | 4.8                          | Same as above                                                     |
| Microsoft.CodeAnalysis.AnalyzerUtilities | 3.3.4                        | Must reference SCI <= 8.0.0.0                                     |
| System.Collections.Immutable             | 8.0.0                        | Must not exceed .NET 8 SDK host assembly version                  |
| System.Formats.Asn1                      | 10.0.0                       | Transitive pin in shipped section; flagged for host compat review |
| System.Reflection.Metadata               | (transitive, no central pin) | Must not exceed .NET 8 SDK host assembly version                  |

**Why this matters:** In v0.4.0, a transitive dependency bump pushed SCI to 10.0.0.0, causing CS8032 assembly load failures for every user on .NET 8 SDK. See [issue #850](https://github.com/rjmurillo/moq.analyzers/issues/850).

**Upgrade policy:**

- `Microsoft.CodeAnalysis.*` core packages are **ignored** in the Renovate config (`ignoreDeps`). Renovate does not manage these packages. Update manually when raising the minimum supported SDK version.
- Renovate caps `System.Collections.Immutable` and `System.Reflection.Metadata` at `<=8.0.0` via `allowedVersions`. `AnalyzerAssemblyCompatibilityTests` enforces the same bound (`MaxImmutableVersion = 8.0.0.0`). Renovate will not propose versions above this cap.
- Renovate caps `AnalyzerUtilities` at `<4.14.0`. Version 4.14.0+ pulls `System.Collections.Immutable` 9.0.0.0 transitively.
- `System.Formats.Asn1` has `automerge: false` and the `analyzer-compat` label for manual review.
- The `ValidateAnalyzerHostCompatibility` MSBuild target blocks the build if shipped assemblies exceed host bounds. `AnalyzerAssemblyCompatibilityTests` provides test-level verification.

### Build-time Code Analysis - SAFE

These run only during builds and are not shipped. Updates do not affect end users. See [`build/targets/codeanalysis/Packages.props`](../build/targets/codeanalysis/Packages.props) for the full list.

Representative packages:

| Package                          | Location                                  |
| -------------------------------- | ----------------------------------------- |
| Meziantou.Analyzer               | build/targets/codeanalysis/Packages.props |
| SonarAnalyzer.CSharp             | build/targets/codeanalysis/Packages.props |
| Roslynator.Analyzers             | build/targets/codeanalysis/Packages.props |
| StyleCop.Analyzers               | build/targets/codeanalysis/Packages.props |
| Microsoft.CodeAnalysis.Analyzers | build/targets/codeanalysis/Packages.props |

**Upgrade policy:** Automerge minor/patch. Major versions may introduce new warnings that break the build (warnings are errors). Review new rules before merging major bumps.

### Test Framework - SAFE

Test-only dependencies with no shipped impact. See [`build/targets/tests/Packages.props`](../build/targets/tests/Packages.props) for the full list.

Representative packages:

| Package                | Location                             |
| ---------------------- | ------------------------------------ |
| Verify.Xunit           | build/targets/tests/Packages.props   |
| xunit                  | build/targets/tests/Packages.props   |
| Microsoft.NET.Test.Sdk | build/targets/tests/Packages.props   |
| coverlet.msbuild       | build/targets/tests/Packages.props   |

**Upgrade policy:** Automerge minor/patch. CI validates compatibility.

### Benchmark Tooling - COORDINATED

BenchmarkDotNet and Perfolizer have intertwined version requirements. BenchmarkDotNet declares a minimum Perfolizer version and a minimum Microsoft.CodeAnalysis.CSharp version.

| Package         | Central Pin              | Notes                                   |
| --------------- | ------------------------ | --------------------------------------- |
| BenchmarkDotNet | Directory.Packages.props | Transitive dep on Perfolizer and Roslyn |
| Perfolizer      | Directory.Packages.props | Used directly by PerfDiff tool          |

**Upgrade policy:**

- **Perfolizer is disabled in Renovate.** BenchmarkDotNet declares an exact version constraint on Perfolizer (e.g., `[0.6.1]`). Updating Perfolizer alone produces NuGet warning NU1608 and risks PerfDiff runtime failures. Update Perfolizer only when BenchmarkDotNet targets a newer version.
- **BenchmarkDotNet** has `automerge: false` and the `benchmark-tooling` label. Updates require manual verification that the new Perfolizer transitive pin is compatible.
- The benchmark project uses `VersionOverride` for packages whose central pins are constrained by shipped analyzer compatibility (e.g., `System.Collections.Immutable`).

### PerfDiff Tool - SPECIAL HANDLING

The PerfDiff tool (`src/tools/PerfDiff/`) uses System.CommandLine, which had breaking API changes between beta and stable releases. The `IConsole` interface was removed in 2.0.3.

| Package                      | Status                                                    |
| ---------------------------- | --------------------------------------------------------- |
| System.CommandLine           | **Disabled** in Renovate until PerfDiff is rewritten      |
| System.CommandLine.Rendering | **Disabled** (folded into main package in stable release) |

**Why disabled:** The perf CI check builds PerfDiff on-demand. It is excluded from the normal build/test matrix. Updates that break PerfDiff only surface as `perf` check failures, which are a required status check.

### Build Infrastructure - MODERATE CAUTION

| Package                            | Location                                     | Notes                    |
| ---------------------------------- | -------------------------------------------- | ------------------------ |
| Polyfill                           | build/targets/compiler/Packages.props        | Compiler polyfills       |
| DotNet.ReproducibleBuilds          | build/targets/reproducible/Packages.props    | Build reproducibility    |
| DotNet.ReproducibleBuilds.Isolated | global.json (msbuild-sdks)                   | MSBuild SDK isolation    |
| Nerdbank.GitVersioning             | Directory.Packages.props                     | Version calculation      |

**Upgrade policy:** Automerge minor/patch for stable versions. ReproducibleBuilds and Isolated should be updated together (same release cadence).

## Configuration Files

| File                             | Purpose                                                              |
| -------------------------------- | -------------------------------------------------------------------- |
| `renovate.json`                  | Renovate bot configuration (NuGet packages and GitHub Actions)       |
| `Directory.Packages.props`       | Central package version management                                   |
| `build/targets/*/Packages.props` | Category-specific package versions                                   |

## VersionOverride Pattern

Non-shipped projects (benchmarks, PerfDiff) that need higher versions of centrally pinned packages use `VersionOverride` in their `.csproj`:

```xml
<!-- Benchmarks are not shipped. Allow higher SCI for TraceEvent dependency. -->
<PackageReference Include="System.Collections.Immutable" VersionOverride="10.0.0" />
```

This allows the central pin (8.0.0) to protect shipped analyzer DLLs while letting tools use newer versions.

**Interaction with Renovate:** Renovate `allowedVersions` caps apply globally by package name. They only affect upgrade proposals. Renovate does not propose downgrades. A `<=8.0.0` cap on `System.Collections.Immutable` does not affect `VersionOverride` entries at 10.x. Manage those overrides manually.

## Workflow

A consolidated workflow (`.github/workflows/dependabot-approve-and-auto-merge.yml`) handles auto-approval for dependency update PRs.

- **Renovate** (NuGet and GitHub Actions): The workflow approves the PR. Auto-merge is controlled by Renovate via `platformAutomerge: true` and per-package `automerge` rules in `renovate.json`. Packages with `automerge: false` (e.g., `analyzer-compat`, `benchmark-tooling`) require manual merge after review.
- **Security alerts**: GitHub may open security alert PRs regardless of bot configuration. These are a repo-level setting, not controlled by any config file. The workflow approves and enables auto-merge for non-major security updates.
