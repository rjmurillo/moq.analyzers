# Claude Code Instructions

This file is loaded automatically by Claude Code at the start of every session.

## Primary Instructions

All AI agent instructions are in [.github/copilot-instructions.md](.github/copilot-instructions.md). Read and follow those instructions.

## Critical Serena Memories (LOAD BEFORE IMPLEMENTING)

These memories contain verified data that prevents known mistakes. Load them via `mcp__serena__read_memory` before writing analyzer code:

| Memory | Why Load It |
|--------|-------------|
| `moq-api-surface-reference` | Complete Moq 4.18.4 API verified via dotnet-inspect. 73 types, 20 Returns overloads, fluent hierarchy, version diff (4.8.2 vs 4.18.4). Prevents phantom symbol bugs. |
| `critical-mistakes-prevention` | Hard-won lessons: phantom symbols, Returns<T> trap, Roslyn target-type inference, testing requirements. Prevents repeating v0.4.1/v0.4.2 mistakes. |
| `roslyn-analyzer-best-practices` | Verified Moq type catalog, IOperation guidance, positive/negative test requirements. |
| `architectural-patterns` | WellKnown types pattern, early exit, MoqKnownSymbols lifetime, registration boilerplate. |

## Quick Reference

- **SDK**: .NET 10 (10.0.201) at `~/.dotnet`. Run `source ~/.zshrc` before `dotnet` commands.
- **Build**: `dotnet build -c Release /p:PedanticMode=true` (zero warnings required)
- **Test**: `dotnet test --settings ./build/targets/tests/test.runsettings`
- **Inspect Moq API**: `dotnet-inspect type --package Moq --all` or `dotnet-inspect member "TypeName" --package Moq --all`
- **Conventional Commits**: `feat:`, `fix:`, `docs:`, `test:`, `refactor:`, `perf:`, `chore:`
