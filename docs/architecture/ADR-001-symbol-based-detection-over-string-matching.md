---
title: "ADR-001: Symbol-Based Detection Over String Matching"
status: "Accepted"
date: "2026-03-15"
authors: "moq.analyzers maintainers"
tags: ["architecture", "decision", "roslyn", "detection"]
supersedes: ""
superseded_by: ""
---

## Status

Accepted

## Context

Analyzers in this project detect Moq API usage at compile time. The most direct detection approach is to compare type or method names as strings. This approach is fragile. A user type named `Mock`, an aliased import (`using M = Moq.Mock`), or a fully-qualified name (`Moq.Mock<T>`) each defeat simple string comparisons.

Roslyn provides the `ISymbol` API and `SymbolEqualityComparer.Default`, which compare symbols by canonical identity rather than by name. Two symbols are equal if and only if they refer to the same type definition in the same compilation.

The analyzer codebase must be correct by construction. False positives erode user trust and produce noise. False negatives allow real Moq misuse to go undetected.

## Decision

All analyzers use Roslyn symbol APIs (`ISymbol`, `INamedTypeSymbol`, `SymbolEqualityComparer.Default`) for type and method identity checks. String matching on type or method names is not used for semantic decisions. The `MoqKnownSymbols` class resolves and caches Moq framework symbols per compilation and is the single source of truth for symbol references.

## Consequences

### Positive

- **POS-001**: Eliminates false positives caused by user-defined types with names that collide with Moq types.
- **POS-002**: Handles aliased imports, fully-qualified names, and `extern alias` transparently.
- **POS-003**: Refactoring-safe: renames inside Moq's assembly do not affect detection as long as metadata names are stable.
- **POS-004**: Consistent with Roslyn best practices and the broader analyzer ecosystem.

### Negative

- **NEG-001**: Requires Moq to be referenced in the compilation. If Moq is absent, `MoqKnownSymbols` returns null symbols and analyzers exit early without reporting diagnostics.
- **NEG-002**: Symbol resolution is more complex than string comparison and requires understanding Roslyn's semantic model.
- **NEG-003**: `MoqKnownSymbols` must be initialized once at the analysis entry point. Incorrect lifecycle management leads to repeated resolution overhead.

## Alternatives Considered

### String Matching on Type and Method Names

- **ALT-001**: **Description**: Compare `symbol.Name` or `symbol.ToDisplayString()` against known Moq type names.
- **ALT-002**: **Rejection Reason**: Produces false positives for user-defined types with the same name. Not resilient to aliased imports or fully-qualified references. Not refactoring-safe if Moq renames internal members.

## Implementation Notes

- **IMP-001**: `MoqKnownSymbols` is constructed via `new MoqKnownSymbols(compilation)` at the start of each `CompilationStartAnalysisContext` callback and passed to operation action callbacks via closure capture.
- **IMP-002**: Analyzers check for null symbols from `MoqKnownSymbols` before proceeding. A null symbol means Moq is not referenced; the analyzer returns without reporting.
- **IMP-003**: `SymbolEqualityComparer.Default` is used for all symbol equality checks to avoid accidental reference equality.

## References

- **REF-001**: `src/Common/WellKnown/MoqKnownSymbols.cs` -- symbol resolution and caching
- **REF-002**: Roslyn API documentation: `ISymbol`, `SymbolEqualityComparer`
- **REF-003**: ADR-006 -- WellKnown Types Pattern for Moq Symbol Resolution
