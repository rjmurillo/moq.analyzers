---
applyTo: '.gitignore'
---

**Critical Rules:** Always read this file before editing. Never skip validation steps. All changes must be fully validated, documented, and reviewed. If you are uncertain or blocked, escalate immediately by tagging @repo-maintainers. Failure to follow these rules may result in PR rejection.

# .gitignore Instructions (Quick Reference)

- **Read this file before editing .gitignore**
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
- [ ] Only files that should not be tracked are ignored
- [ ] No source, config, or required files are ignored
- [ ] All new build artifacts, logs, and secrets are ignored
- [ ] Machine-readable evidence is attached (see below)
- [ ] PR description includes validation evidence and checklist

## Validation Evidence Requirements
- Attach a log section titled `## Gitignore Validation Log` showing the output of `git status` before and after changes
- Include a checklist like:
  - [x] No required files ignored
  - [x] All build artifacts ignored
- Paste CI run link under `## CI Evidence`

## .gitignore Guidance
- Only ignore files that are not needed in the repository (build output, logs, secrets, etc.)
- Never ignore source code, configuration, or documentation files
- Review changes with maintainers if unsure
- After changes, run `git status` to confirm only intended files are ignored
- Document any changes in the PR description

## Related Instruction Files
- [project.instructions.md](project.instructions.md) - For build artifacts
- [text.instructions.md](text.instructions.md) - For text file patterns 