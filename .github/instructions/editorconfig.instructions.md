---
applyTo: '.editorconfig'
---

**Critical Rules:** Always read this file before editing. Never skip validation steps. All changes must be fully validated, documented, and reviewed. If you are uncertain or blocked, escalate immediately by tagging @repo-maintainers. Failure to follow these rules may result in PR rejection.

# .editorconfig Instructions (Quick Reference)

- **Read this file before editing .editorconfig**
- **Complete the Validation Checklist before submitting**
- **Escalate if blocked or uncertain**

## Context Loading for Copilot

When working on this file, you MUST:
1. Read this entire instruction file before making any changes
2. Validate your understanding by checking the "Validation Checklist" section
3. If uncertain about any requirement, stop and request clarification

**Escalation Path:**
If you (AI or human) are blocked or cannot proceed, leave a comment in the PR describing the blocked step and tag @repo-maintainers for escalation.

## Validation Checklist

Before submitting any changes, verify:
- [ ] All formatting and linting rules are correct and documented
- [ ] All changes are compatible with project coding standards
- [ ] All affected files are re-formatted and linted
- [ ] Machine-readable evidence is attached (see below)
- [ ] PR description includes validation evidence and checklist

## Validation Evidence Requirements
- Attach a log section titled `## EditorConfig Validation Log` showing the output of `dotnet format` or equivalent
- Include a checklist like:
  - [x] All files formatted
  - [x] Lint clean
- Paste CI run link under `## CI Evidence`

## .editorconfig Guidance
- .editorconfig controls code formatting and linting for the repository
- Changes may affect build results, code reviews, and CI
- Only update rules when necessary and after confirming with maintainers
- After changes, run `dotnet format` and ensure no formatting/linting errors
- Document any rule changes in the PR description

## Related Instruction Files
- [csharp.instructions.md](csharp.instructions.md) - For C# code formatting
- [markdown.instructions.md](markdown.instructions.md) - For markdown formatting
- [project.instructions.md](project.instructions.md) - For build configuration 