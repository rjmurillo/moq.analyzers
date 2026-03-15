# Project Overview: Moq.Analyzers

## Purpose

Roslyn analyzer NuGet package for the Moq mocking framework. Detects common mistakes in Moq usage at compile time and offers code fixes. Originally a port of a ReSharper extension.

## Tech Stack

- **Language**: C# 14, .NET 10 SDK (pinned via global.json; see global.json for the exact version)
- **Analyzer target**: .NET Standard 2.0 (runs inside compiler hosts)
- **Roslyn SDK**: Microsoft.CodeAnalysis 4.8 (supports VS 2022 17.8+)
- **Build**: SDK-style projects, Central Package Management (Directory.Packages.props)
- **Versioning**: Nerdbank.GitVersioning (version.json)
- **Reproducible builds**: DotNet.ReproducibleBuilds.Isolated
- **Testing**: xUnit with Roslyn test infrastructure
- **Benchmarks**: BenchmarkDotNet
- **Perf tooling**: PerfDiff CLI tool (src/tools/PerfDiff), uses System.CommandLine 2.0.3

## Counts (verified 2026-03-15)

- **24 DiagnosticAnalyzer classes** in src/Analyzers/
- **5 CodeFixProvider classes** in src/CodeFixes/
- **7 .csproj projects** in Moq.Analyzers.sln

## Solution Structure

```text
Moq.Analyzers.sln
  src/
    Analyzers/       - 24 DiagnosticAnalyzer implementations
    CodeFixes/       - 5 CodeFixProvider implementations
    Common/          - Shared helpers, extension methods, well-known types
      WellKnown/     - MoqKnownSymbols.cs, KnownSymbols.cs, MoqKnownSymbolExtensions.cs
  tests/
    Moq.Analyzers.Test/       - Unit tests for analyzers and code fixes
    Moq.Analyzers.Benchmarks/ - Performance benchmarks (baselines in build/perf/)
    PerfDiff.Tests/           - Tests for PerfDiff tool
  build/
    targets/         - MSBuild imports (compiler, code analysis, tests, versioning, packaging)
    perf/            - Performance baseline data (compared by PerfDiff in CI)
    scripts/         - Build/CI helper scripts (Perf.sh, Perf.cmd)
  docs/
    rules/           - Per-rule documentation (Moq1XXX.md)
    dependency-management.md
```

## Extension Methods in src/Common/ (verified 2026-03-15)

ArrayExtensions, DiagnosticExtensions, EnumerableExtensions, EventSyntaxExtensions,
IMethodSymbolExtensions, IOperationExtensions, ISymbolExtensions, ITypeSymbolExtensions,
InvocationExpressionSyntaxExtensions, NamedTypeSymbolExtensions, SemanticModelExtensions,
SyntaxNodeExtensions

## Key Design Principles

- Symbol-based detection over string matching (type-safe, no false matches)
- Analyzers run on every keystroke in IDEs; performance is critical
- Zero tolerance for false positives or false negatives
- Defensive coding: null checks, edge cases, thorough error handling
- All changes require tests proving correctness, not just coverage
