# Dependency Management

This project uses **Renovate** as the primary dependency update bot. Dependabot is retained only for GitHub Actions updates.

## Package Categories

Dependencies fall into distinct categories with different upgrade policies.

### Shipped (in analyzer nupkg) - EXTREME CAUTION

These packages are bundled in the analyzer NuGet package and run inside the **user's** compiler host (Visual Studio, dotnet CLI). Version constraints are critical because users may run older SDKs.

| Package | Central Pin | Constraint |
|---------|------------|------------|
| Microsoft.CodeAnalysis.CSharp | 4.8 | Minimum supported VS/SDK version |
| Microsoft.CodeAnalysis.CSharp.Workspaces | 4.8 | Same as above |
| Microsoft.CodeAnalysis.AnalyzerUtilities | 3.3.4 | Must reference SCI <= 8.0.0.0 |
| System.Collections.Immutable | 8.0.0 | Must not exceed .NET 8 SDK host assembly version |
| System.Reflection.Metadata | (transitive) | Must not exceed .NET 8 SDK host assembly version |

**Why this matters:** In v0.4.0, a transitive dependency bump pushed SCI to 10.0.0.0, causing CS8032 assembly load failures for every user on .NET 8 SDK. See [issue #850](https://github.com/rjmurillo/moq.analyzers/issues/850).

**Upgrade policy:**
- `Microsoft.CodeAnalysis.*` core packages are **ignored** in both Renovate and Dependabot configs. Update manually when raising the minimum supported SDK version.
- `System.Collections.Immutable`, `System.Reflection.Metadata`, and `AnalyzerUtilities` have `automerge: false` and the `analyzer-compat` label. Every update requires manual validation that assembly versions stay within host bounds.
- The `ValidateAnalyzerHostCompatibility` MSBuild target and `AnalyzerAssemblyCompatibilityTests` enforce this at build and test time.

### Build-time Code Analysis - SAFE

These run only during builds and are not shipped. Updates do not affect end users.

| Package | Location |
|---------|----------|
| Meziantou.Analyzer | build/targets/codeanalysis/Packages.props |
| SonarAnalyzer.CSharp | build/targets/codeanalysis/Packages.props |
| Roslynator.Analyzers | build/targets/codeanalysis/Packages.props |
| StyleCop.Analyzers | build/targets/codeanalysis/Packages.props |
| Microsoft.CodeAnalysis.Analyzers | build/targets/codeanalysis/Packages.props |

**Upgrade policy:** Automerge minor/patch. Major versions may introduce new warnings that break the build (warnings are errors). Review new rules before merging major bumps.

### Test Framework - SAFE

Test-only dependencies with no shipped impact.

| Package | Location |
|---------|----------|
| Verify.Xunit | build/targets/tests/Packages.props |
| xunit | build/targets/tests/Packages.props |
| Microsoft.NET.Test.Sdk | build/targets/tests/Packages.props |
| coverlet.msbuild | build/targets/tests/Packages.props |

**Upgrade policy:** Automerge minor/patch. CI validates compatibility.

### Benchmark Tooling - COORDINATED

BenchmarkDotNet and Perfolizer have intertwined version requirements. BenchmarkDotNet declares a minimum Perfolizer version and a minimum Microsoft.CodeAnalysis.CSharp version.

| Package | Central Pin | Notes |
|---------|------------|-------|
| BenchmarkDotNet | Directory.Packages.props | Transitive dep on Perfolizer and Roslyn |
| Perfolizer | Directory.Packages.props | Used directly by PerfDiff tool |

**Upgrade policy:** Grouped in Renovate as `benchmark-tooling` with `automerge: false`. Both packages must be updated together. The benchmark project uses `VersionOverride` for packages whose central pins are constrained by shipped analyzer compatibility (e.g., `System.Collections.Immutable`, `Microsoft.CodeAnalysis.CSharp`).

### PerfDiff Tool - SPECIAL HANDLING

The PerfDiff tool (`src/tools/PerfDiff/`) uses System.CommandLine, which had breaking API changes between beta and stable releases. The `IConsole` interface was removed in 2.0.3.

| Package | Status |
|---------|--------|
| System.CommandLine | **Disabled** in Renovate until PerfDiff is rewritten |
| System.CommandLine.Rendering | **Disabled** (removed in stable release) |

**Why disabled:** The perf CI check builds PerfDiff on-demand. It is excluded from the normal build/test matrix. Updates that break PerfDiff only surface as `perf` check failures, which are a required status check.

### Build Infrastructure - MODERATE CAUTION

| Package | Location | Notes |
|---------|----------|-------|
| Polyfill | build/targets/compiler/Packages.props | Compiler polyfills |
| DotNet.ReproducibleBuilds | build/targets/reproducible/Packages.props | Build reproducibility |
| DotNet.ReproducibleBuilds.Isolated | global.json (msbuild-sdks) | MSBuild SDK isolation |
| Nerdbank.GitVersioning | Directory.Packages.props | Version calculation |

**Upgrade policy:** Automerge minor/patch for stable versions. ReproducibleBuilds and Isolated should be updated together (same release cadence).

## Configuration Files

| File | Purpose |
|------|---------|
| `renovate.json` | Renovate bot configuration (primary dependency bot) |
| `.github/dependabot.yml` | Dependabot configuration (GitHub Actions only) |
| `Directory.Packages.props` | Central package version management |
| `build/targets/*/Packages.props` | Category-specific package versions |

## VersionOverride Pattern

Non-shipped projects (benchmarks, PerfDiff) that need higher versions of centrally pinned packages use `VersionOverride` in their `.csproj`:

```xml
<!-- Benchmarks are not shipped. Allow higher SCI for TraceEvent dependency. -->
<PackageReference Include="System.Collections.Immutable" VersionOverride="10.0.0" />
```

This allows the central pin (8.0.0) to protect shipped analyzer DLLs while letting tools use newer versions.

## Workflow

A single consolidated workflow (`.github/workflows/dependabot-approve-and-auto-merge.yml`) handles auto-approval and auto-merge for both Dependabot and Renovate PRs. The workflow skips auto-merge for Renovate PRs labeled `analyzer-compat`, `benchmark-tooling`, or `major`, which Renovate applies based on `packageRules` in `renovate.json`. This ensures packages requiring manual review are never auto-merged.
