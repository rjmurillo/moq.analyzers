# Quality and Correctness Standards (CRITICAL)

These analyzers operate on mission-critical codebases. Correctness and accuracy are the highest priority.

All code MUST adhere to the strict quality and performance standards of the .NET Base Class Library (BCL).

## Requirements

- **Zero tolerance for false positives or false negatives.** An incorrect diagnostic erodes trust and causes developers to disable the analyzer entirely.
- **Performance is a hard requirement.** Analyzers execute on every keystroke in IDEs. Allocations, LINQ overhead, and unnecessary computation directly impact developer experience.
- **Symbol-based detection only.** String matching is fragile, not refactoring-safe, and produces false matches. Use `ISymbol`, `SymbolEqualityComparer`, and `MoqKnownSymbols` for all type resolution.
- **Defensive coding.** Handle nulls, edge cases, and unexpected syntax gracefully. An analyzer crash is worse than a missed diagnostic.
- **Tests prove correctness, not just coverage.** Every test must verify a specific, meaningful behavior. Include positive cases, negative cases, and edge cases that exercise boundary conditions.
- **No speculative implementations.** Understand the problem fully before writing code. If you cannot explain why your approach is correct, stop and ask.

## Applies To

- All contributions, all agents, all sessions
- Both new analyzers and modifications to existing ones
- Test code quality matters equally (tests prove correctness)

## See Also

- `.github/copilot-instructions.md` for full AI agent instructions
- `CONTRIBUTING.md` for general contributor guidance
