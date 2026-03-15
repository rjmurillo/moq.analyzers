---
title: "ADR-002: Target .NET Standard 2.0 for Analyzer Assemblies"
status: "Accepted"
date: "2026-03-15"
authors: "moq.analyzers maintainers"
tags: ["architecture", "decision", "tfm", "compatibility"]
supersedes: ""
superseded_by: ""
---

## Status

Accepted

## Context

Roslyn analyzers execute inside the host compiler process. Hosts include Visual Studio, `dotnet build`, the .NET CLI, and JetBrains Rider. Each host provides its own runtime and a fixed set of available assemblies. The analyzer assembly must load successfully in every supported host.

If an analyzer targets a higher target framework moniker (TFM) than the host supports, the assembly fails to load. The user sees no diagnostics and may see build errors. The user cannot work around this without removing the analyzer package.

.NET Standard 2.0 is the lowest common denominator for all current Roslyn host environments. It guarantees the analyzer loads in any host that ships with Visual Studio 2022 or any .NET SDK from .NET Core 3.1 onward.

## Decision

Analyzer projects (`src/Analyzers/`) and code fix projects (`src/CodeFixes/`) target `netstandard2.0`. No analyzer or code fix assembly targets a framework-specific TFM such as `net8.0` or `net9.0`.

## Consequences

### Positive

- **POS-001**: Analyzer loads in all supported host environments without compatibility failures.
- **POS-002**: Consistent with the standard practice for all published Roslyn analyzers in the .NET ecosystem.
- **POS-003**: NuGet package structure is straightforward: a single `analyzers/dotnet/cs/` folder with no TFM branching.

### Negative

- **NEG-001**: .NET 5+ APIs (e.g., `System.HashCode`, many `Span<T>` overloads) are unavailable in analyzer code.
- **NEG-002**: Must polyfill or avoid modern language features that depend on runtime types not in .NET Standard 2.0.
- **NEG-003**: Test and benchmark projects may target higher TFMs, creating a cognitive split between what is available in tests versus analyzers.

## Alternatives Considered

### Target net8.0

- **ALT-001**: **Description**: Target `net8.0` to access modern .NET APIs and language features.
- **ALT-002**: **Rejection Reason**: Not loadable in Visual Studio or Rider host processes that do not run on .NET 8. Causes silent analyzer failures in common IDE environments.

### Target net9.0

- **ALT-003**: **Description**: Target `net9.0` for the latest runtime improvements.
- **ALT-004**: **Rejection Reason**: Even more restrictive than `net8.0`. No current production IDE host runs .NET 9. Would prevent the analyzer from loading in any currently supported host.

## Implementation Notes

- **IMP-001**: All `src/Analyzers/` and `src/CodeFixes/` `.csproj` files declare `<TargetFramework>netstandard2.0</TargetFramework>`.
- **IMP-002**: Helper and tool projects outside the analyzer assembly (e.g., `src/tools/PerfDiff/`) may target higher TFMs since they do not run inside the compiler host.
- **IMP-003**: When adding new code, verify all APIs used are available in the .NET Standard 2.0 surface area before committing.

## References

- **REF-001**: Microsoft documentation: "How analyzers run in the compiler pipeline"
- **REF-002**: ADR-003 -- Pin Roslyn SDK to Microsoft.CodeAnalysis 4.8
- **REF-003**: ADR-004 -- Cap Microsoft.CodeAnalysis.AnalyzerUtilities at 3.3.4
