---
applyTo: '**/*'
---

**Critical Rules:** Always read this file before editing. Never skip validation steps. All changes must be fully validated, documented, and reviewed. If you are uncertain or blocked, escalate immediately by tagging @repo-maintainers. Failure to follow these rules may result in PR rejection.

# Generic File Instructions (Quick Reference)

- **Read this file before editing unknown or rarely used file types**
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
- [ ] All changes are compatible with project requirements
- [ ] No breaking changes to build, test, or deployment
- [ ] Machine-readable evidence is attached (see below)
- [ ] PR description includes validation evidence and checklist

## Validation Evidence Requirements
- Attach a log section titled `## Validation Log` showing the output of any relevant validation commands (build, test, lint, etc.)
- Include a checklist like:
  - [x] No errors or warnings
- Paste CI run link under `## CI Evidence`

## Guidance
- If you are unsure how to edit this file type, consult maintainers before proceeding
- Document any changes in the PR description

## Related Instruction Files
- [project.instructions.md](project.instructions.md) - For project/solution files
- [csharp.instructions.md](csharp.instructions.md) - For C# files
- [markdown.instructions.md](markdown.instructions.md) - For documentation
- [shell.instructions.md](shell.instructions.md) - For scripts 