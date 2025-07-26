---
applyTo: '**/*.{csproj,sln}'
---

# Project File Instructions (Quick Reference)

- **Read this file before editing any .csproj or .sln file**
- **Cross-reference with related instruction files**
- **Complete the Validation Checklist before submitting**
- **Stop and request help if uncertain**

## Context Loading for Copilot

When working on this file type, you MUST:
1. Read this entire instruction file before making any changes
2. Cross-reference with related instruction files (listed below)
3. Validate your understanding by checking the "Validation Checklist" section
4. If uncertain about any requirement, stop and request clarification

**Related Instruction Files:**
- [csharp.instructions.md](csharp.instructions.md) - For C# code changes
- [json.instructions.md](json.instructions.md) - For configuration files
- [yaml.instructions.md](yaml.instructions.md) - For CI/CD workflows

## Validation Checklist

Before submitting any changes, verify:

**Build & Dependency Management:**
- [ ] Target .NET 9, C# 13
- [ ] All build and dependency requirements met
- [ ] No warnings or errors in build
- [ ] All tests pass
- [ ] Security scan completed after dependency changes

**Process:**
- [ ] Conventional commit format used
- [ ] PR description includes validation evidence
- [ ] All checklist items completed
- [ ] No breaking changes introduced (or documented if necessary)

## Decision Trees

### When to Request Human Review
- Is this a new project or build configuration? → Yes → Request expert guidance
- Is this a breaking change to build or dependencies? → Yes → Document thoroughly and request review
- Are you uncertain about MSBuild or dependency management? → Yes → Stop and request guidance

### When to Stop and Ask for Help
- Uncertain about build or dependency requirements
- Major changes to project structure
- Security or legal implications

## Common Mistakes to Avoid

**DO NOT:**
- Skip validation steps
- Ignore security scanning after dependency changes
- Submit changes without validation evidence

**ALWAYS:**
- Read the entire instruction file first
- Validate all build and dependency changes
- Include comprehensive documentation updates
- Document all changes thoroughly

## Context Management

**Before Starting:**
- Read the complete instruction file
- Understand the current file's purpose and structure
- Identify all related files that may need updates

**During Editing:**
- Keep track of all changes made
- Validate each change against requirements
- Maintain consistency with existing patterns

**After Completing:**
- Review all changes against the validation checklist
- Ensure all requirements are met
- Prepare comprehensive PR description with evidence

## Handling Uncertainty

**Stop and Request Help When:**
- You cannot explain your approach clearly
- You're making "educated guesses" about build or dependency management
- You're uncertain about project structure
- You cannot trace the logic in configuration without narration

**Escalation Process:**
1. Stop all work immediately
2. Document what you were trying to accomplish
3. Explain what specific aspect is unclear
4. Request expert guidance with specific questions
5. Do not proceed until you have clear, confident understanding

## Success Criteria

Your changes are successful when:
- All builds pass without warnings
- All tests pass
- No linting errors
- Security scan is clean
- PR description is complete and accurate
- All checklist items completed
- No automated bot feedback ignored

## (The rest of the file remains as previously written, including build configuration, dependency management, code quality standards, commit message guidelines, PR guidelines, review process, and formatting/linting requirements.) 