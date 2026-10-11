# Copilot Instructions

The instructions for this repository live in [AGENTS.md](../AGENTS.md) at the repository root. Read and follow that file before you make any change.

This file exists for Copilot surfaces that read only `.github/copilot-instructions.md`. Keep it short. Put new rules in `AGENTS.md`.

The most critical rules from `AGENTS.md`:

- Code MUST meet .NET BCL standards for quality and performance. These analyzers run on mission-critical codebases.
- Never guess or use trial-and-error. STOP if unsure.
- Use symbol-based detection only. String matching is fragile and produces false matches.
- All changes must be covered by tests, including edge and failure paths. 100% block coverage is required for modified code.
- Run `dotnet format`, `dotnet build` (no warnings allowed), and `dotnet test` (all must pass).
- Read and comply with all `.github/instructions/*` files for the file types you edit.
