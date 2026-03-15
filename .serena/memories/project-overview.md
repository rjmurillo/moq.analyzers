# Project Overview: Moq.Analyzers

## Purpose

Roslyn analyzer NuGet package for the Moq mocking framework. Detects common mistakes in Moq usage at compile time and offers code fixes. Originally a port of a ReSharper extension.

## Tech Stack

- **Language**: C# 13, .NET 9 SDK (pinned via global.json, currently 10.0.201)
- **Analyzer target**: .NET Standard 2.0 (runs inside compiler hosts)
- **Roslyn SDK**: Microsoft.CodeAnalysis 4.8 (supports VS 2022 17.8+)
- **Build**: SDK-style projects, Central Package Management (Directory.Packages.props)
- **Versioning**: Nerdbank.GitVersioning (version.json)
- **Reproducible builds**: DotNet.ReproducibleBuilds.Isolated
- **Testing**: xUnit with Roslyn test infrastructure
- **Benchmarks**: BenchmarkDotNet
- **Perf tooling**: PerfDiff CLI tool (src/tools/PerfDiff)

## Solution Structure

```text
Moq.Analyzers.sln
  src/
    Analyzers/       - DiagnosticAnalyzer implementations (~20 analyzers)
    CodeFixes/       - CodeFixProvider implementations (~7 fixers)
    Common/          - Shared helpers, extension methods, well-known types
  tests/
    Moq.Analyzers.Test/       - Unit tests for analyzers and code fixes
    Moq.Analyzers.Benchmarks/ - Performance benchmarks
    PerfDiff.Tests/            - Tests for PerfDiff tool
  build/
    targets/         - MSBuild imports (compiler, code analysis, tests, versioning, packaging, etc.)
    perf/            - Performance baseline data
    scripts/         - Build/CI helper scripts
  docs/
    rules/           - Per-rule documentation
    dependency-management.md
```

## Key Design Principles

- Symbol-based detection over string matching (type-safe, no false matches)
- Analyzers run on every keystroke in IDEs; performance is critical
- Zero tolerance for false positives or false negatives
- Defensive coding: null checks, edge cases, thorough error handling
- All changes require tests proving correctness, not just coverage
