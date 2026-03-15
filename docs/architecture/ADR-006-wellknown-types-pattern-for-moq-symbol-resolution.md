---
title: "ADR-006: WellKnown Types Pattern for Moq Symbol Resolution"
status: "Accepted"
date: "2026-03-15"
authors: "moq.analyzers maintainers"
tags: ["architecture", "decision", "roslyn", "symbols", "performance"]
supersedes: ""
superseded_by: ""
---

## Status

Accepted

## Context

23 analyzers in this project need references to Moq framework types: `Mock<T>`, `MockRepository`, `Times`, `It`, `ISetup`, and others. Each analyzer must resolve these types before it can perform semantic checks.

Roslyn's `Compilation.GetTypeByMetadataName()` performs a symbol lookup across all referenced assemblies. Calling it once per operation (per invocation analyzed, per file, per keystroke in an IDE) multiplies into thousands of redundant lookups during a typical build or IDE session.

The `WellKnownTypes` pattern is established in the Roslyn codebase itself: resolve all needed symbols once at compilation start, store them in a data object, and pass that object through analysis callbacks.

## Decision

A dedicated `MoqKnownSymbols` class in `src/Common/WellKnown/` resolves all Moq `INamedTypeSymbol` references once per `CompilationStartAnalysisContext`. Analyzers receive the resolved instance as a parameter. No analyzer calls `GetTypeByMetadataName()` directly.

## Consequences

### Positive

- **POS-001**: Symbol resolution cost is paid once per compilation, not once per operation. This directly reduces IDE keystroke latency.
- **POS-002**: All Moq type references are defined in one class. Adding a new type reference is a single-location change.
- **POS-003**: If Moq is not referenced, all properties on `MoqKnownSymbols` return null. Analyzers check for null and exit early, avoiding any downstream null reference exceptions.
- **POS-004**: Consistent interface across all 23 analyzers. No analyzer has its own ad hoc symbol resolution.

### Negative

- **NEG-001**: `MoqKnownSymbols` must be created at the `CompilationStartAnalysisContext` callback, not at the operation callback. Incorrect instantiation site negates the caching benefit.
- **NEG-002**: Adding new Moq type references requires modifying `MoqKnownSymbols`, which is a shared class. Concurrent feature branches touching this class risk merge conflicts.
- **NEG-003**: The class grows as Moq adds new API surface. Maintenance is required to keep it current with supported Moq versions.

## Alternatives Considered

### Inline GetTypeByMetadataName Calls

- **ALT-001**: **Description**: Each analyzer calls `compilation.GetTypeByMetadataName()` inside its operation or syntax callbacks.
- **ALT-002**: **Rejection Reason**: Duplicates resolution logic across 23 analyzers. Performs redundant lookups on every analyzed operation. No single place to audit which Moq types are in use.

### Static Compilation Cache

- **ALT-003**: **Description**: Cache resolved symbols in a static `ConcurrentDictionary<Compilation, MoqKnownSymbols>`.
- **ALT-004**: **Rejection Reason**: Roslyn runs analyzers concurrently across multiple threads and compilations. A static cache requires careful lock management. The `CompilationStartAnalysisContext` lifetime model already provides a per-compilation scope that is managed by Roslyn; using it is safer and idiomatic.

## Implementation Notes

- **IMP-001**: `MoqKnownSymbols.Create(compilation)` is called once inside `context.RegisterCompilationStartAction(compilationContext => { var symbols = MoqKnownSymbols.Create(compilationContext.Compilation); ... })`.
- **IMP-002**: Analyzers receive `MoqKnownSymbols` via closure capture or method parameter. They do not create new instances.
- **IMP-003**: Each property on `MoqKnownSymbols` that returns null when Moq is not referenced is documented with an XML comment explaining the null condition.

## References

- **REF-001**: `src/Common/WellKnown/MoqKnownSymbols.cs` -- implementation
- **REF-002**: ADR-001 -- Symbol-Based Detection Over String Matching
- **REF-003**: Roslyn analyzer performance guidance: <https://github.com/dotnet/roslyn-analyzers/blob/main/docs/Analyzer%20Performance.md>
