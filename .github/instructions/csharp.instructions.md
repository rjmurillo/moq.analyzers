---
applyTo: '**/*.cs'
---

# C# File Instructions

> **MANDATORY:** You MUST follow these instructions when editing any C# file in this repository.

- **For complex changes, see the Decision Trees section below**

## Primary Instructions

Always read and apply the instructions in [.github/copilot-instructions.md](../copilot-instructions.md) when working on C# source or project files.

## Additional Context

This instruction file works in conjunction with the comprehensive C# development guidelines in the main copilot-instructions.md file. The main file contains detailed requirements for:

- Roslyn analyzer development
- Code quality standards
- Testing requirements
- Performance considerations
- XML documentation standards
- Workflow requirements

## File-Specific Requirements

When working with C# files, ensure you also review:

- **Project files**: See [project.instructions.md](project.instructions.md) for .csproj and .sln files
- **XML files**: See [xml.instructions.md](xml.instructions.md) for XML documentation and configuration
- **Documentation**: See [markdown.instructions.md](markdown.instructions.md) for documentation updates

## Maintenance Note

If you update guidance in copilot-instructions.md that affects C# development, ensure consistency across all related instruction files.

## Decision Trees for Complex Scenarios

### Multi-File/Feature Change Flowchart

1. **Identify all affected file types**
2. **For each file type:**
   - Locate and read the corresponding instruction file
   - Note any validation, documentation, or escalation requirements
3. **Plan the change:**
   - List all files to be edited
   - Determine order of operations (e.g., code first, then docs, then config)
   - Identify dependencies between files
4. **Edit files in logical order**
5. **After each file edit:**
   - Run required validation (build, test, lint, etc.)
   - Document evidence as required
6. **After all edits:**
   - Re-run all tests and validations
   - Update documentation and release notes as needed
   - Prepare a comprehensive PR description with evidence for each file type
7. **If blocked or uncertain at any step:**
   - Escalate by tagging @repo-maintainers

### Introducing a New Analyzer or Code Fix Flowchart

1. **Read csharp.instructions.md and project.instructions.md**
2. **Scaffold the new analyzer/fixer in the correct directory**
3. **Add or update unit tests (see test instructions)**
4. **Update documentation:**
   - Add/modify rule docs in docs/rules/
   - Update AnalyzerReleases.Unshipped.md
5. **Update project files if needed**
6. **Run all validations (build, test, lint, Codacy, etc.)**
7. **Prepare PR with validation evidence for each file type**
8. **If any diagnostic span or test fails more than once, STOP and escalate**
9. **If uncertain about Roslyn APIs, Moq semantics, or workflow, escalate**
