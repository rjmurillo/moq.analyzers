---
applyTo: '**/*.{props,targets}'
---

**Critical Rules:** Always read this file before editing. Never skip validation steps. All changes must be fully validated, documented, and reviewed. If you are uncertain or blocked, escalate immediately by tagging @repo-maintainers. Failure to follow these rules may result in PR rejection.

# MSBuild Property/Target File Instructions (Quick Reference)

- **Read this file before editing .props or .targets files**
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
- [ ] All property/target changes are compatible with project build requirements
- [ ] No breaking changes to build or packaging
- [ ] All affected projects build and test successfully
- [ ] Machine-readable evidence is attached (see below)
- [ ] PR description includes validation evidence and checklist

## Validation Evidence Requirements
- Attach a log section titled `## MSBuild Validation Log` showing the output of `dotnet build` and `dotnet test` for all affected projects
- Include a checklist like:
  - [x] All projects build
  - [x] All tests pass
- Paste CI run link under `## CI Evidence`

## MSBuild Guidance
- .props and .targets files control build, packaging, and deployment
- Only update when necessary and after confirming with maintainers
- After changes, run `dotnet build` and `dotnet test` for all affected projects
- Document any changes in the PR description

## Related Instruction Files
- [project.instructions.md](project.instructions.md) - For project/solution files
- [shell.instructions.md](shell.instructions.md) - For build scripts 