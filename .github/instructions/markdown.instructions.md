---
applyTo: '**/*.md'
---

# Markdown File Instructions (Quick Reference)

- **Read this file before editing any .md file**
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
- [project.instructions.md](project.instructions.md) - For build configuration changes
- [text.instructions.md](text.instructions.md) - For plain text documentation

## Validation Checklist

Before submitting any changes, verify:

**Documentation:**
- [ ] All documentation standards followed
- [ ] Formatting and linting requirements met
- [ ] Table of contents updated if needed
- [ ] All links are valid and working

**Process:**
- [ ] Conventional commit format used
- [ ] PR description includes validation evidence
- [ ] All checklist items completed
- [ ] No breaking changes introduced (or documented if necessary)

## Decision Trees

### When to Request Human Review
- Is this a new documentation standard? → Yes → Request expert guidance
- Is this a breaking change to documentation? → Yes → Document thoroughly and request review
- Are you uncertain about documentation structure? → Yes → Stop and request guidance

### When to Stop and Ask for Help
- Uncertain about documentation requirements
- Major changes to project documentation
- Security or legal implications

## Common Mistakes to Avoid

**DO NOT:**
- Skip validation steps
- Assume external file references will be loaded
- Ignore automated bot feedback
- Submit changes without validation evidence

**ALWAYS:**
- Read the entire instruction file first
- Validate all links and formatting
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
- You're making "educated guesses" about documentation standards
- You're uncertain about project documentation structure
- You cannot trace the logic in documentation without narration

**Escalation Process:**
1. Stop all work immediately
2. Document what you were trying to accomplish
3. Explain what specific aspect is unclear
4. Request expert guidance with specific questions
5. Do not proceed until you have clear, confident understanding

## Success Criteria

Your changes are successful when:
- All formatting and linting checks pass
- All documentation standards are met
- All links are valid
- PR description is complete and accurate
- All checklist items completed
- No automated bot feedback ignored

## Documentation Standards

### When Documentation is Required

Documentation updates are required for:
- New analyzers or fixers
- Changes to existing analyzer behavior
- API changes or additions
- Installation or usage changes
- CI/CD workflow changes

### Documentation Files to Update

1. **Rule Documentation**: Update `docs/rules/` for analyzer changes
2. **Release Notes**: Update `src/Analyzers/AnalyzerReleases.Unshipped.md`
3. **README**: Update for significant changes
4. **Contributing Guidelines**: Update for workflow changes

### Documentation Format

- Use clear, concise language
- Include code examples where appropriate
- Follow existing documentation patterns
- Ensure all links are valid
- Update table of contents if adding new sections

### Markdown-Specific Requirements

- **Use clear, unambiguous language** in all documentation
- **Provide concrete examples** for all concepts and procedures
- **Create step-by-step guides** for complex processes
- **Include troubleshooting sections** in all documentation
- **Use consistent formatting** across all documentation files
- **Validate all links** before committing
- **Update table of contents** when adding new sections

## Code of Conduct

This project adheres to the [Contributor Covenant Code of Conduct](CODE-OF-CONDUCT.md). By participating, you are expected to uphold this code.

## Git Commit Messages

### Guidelines

1. **Capitalization and Punctuation**: Capitalize the first word and do not end in punctuation. If using Conventional Commits, remember to use all lowercase.
2. **Mood**: Use imperative mood in the subject line. Example – Add fix for dark mode toggle state. Imperative mood gives the tone you are giving an order or request.
3. **Type of Commit**: Specify the type of commit. It is recommended and can be even more beneficial to have a consistent set of words to describe your changes. Example: Bugfix, Update, Refactor, Bump, and so on. See the section on Conventional Commits below for additional information.
4. **Length**: The first line should ideally be no longer than 50 characters, and the body should be restricted to 72 characters.
5. **Content**: Be direct, try to eliminate filler words and phrases in these sentences (examples: though, maybe, I think, kind of). Think like a journalist.

### Conventional Commits

Conventional Commit is a formatting convention that provides a set of rules to formulate a consistent commit message structure like so:

```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

The commit type can include the following:

- feat – a new feature is introduced with the changes
- fix – a bug fix has occurred
- chore – changes that do not relate to a fix or feature and don't modify src or test files (for example updating dependencies)
- refactor – refactored code that neither fixes a bug nor adds a feature
- docs – updates to documentation such as a the README or other markdown files
- style – changes that do not affect the meaning of the code, likely related to code formatting such as white-space, missing semi-colons, and so on.
- test – including new or correcting previous tests
- perf – performance improvements
- ci – continuous integration related
- build – changes that affect the build system or external dependencies
- revert – reverts a previous commit

## Pull Request Guidelines

### PR Title and Description

**Title Format:**
Follow conventional commit format: `type(scope): description`

**Description Requirements:**
1. **Clear summary** of changes
2. **Problem statement** (what issue does this solve?)
3. **Solution description** (how does this solve the problem?)
4. **Validation evidence** (how was this tested?)
5. **Related issues** (link to GitHub issues)
6. **Breaking changes** (if any)

### Required PR Checklist

Before submitting a PR, ensure:

- [ ] Code follows project style guidelines
- [ ] All tests pass locally
- [ ] Documentation is updated
- [ ] Performance impact is assessed (if applicable)
- [ ] Security implications are considered
- [ ] CI checks pass
- [ ] PR description includes validation evidence

## Review Process

### What Maintainers Look For

Maintainers will review PRs for:

1. **Code Quality**: Adherence to project standards
2. **Test Coverage**: Comprehensive testing of changes
3. **Documentation**: Proper documentation updates
4. **Performance**: No significant performance regressions
5. **Security**: No security vulnerabilities introduced
6. **Validation**: Proper evidence of testing and validation

### Review Timeline

- **Initial review**: Within 2-3 business days
- **Follow-up reviews**: Within 1-2 business days after changes
- **Final approval**: After all concerns are addressed

## Formatting and Linting

- **You must address all feedback from automated bots** (e.g., Codeclimate, formatting/linting bots) as you would human reviewers.
- All formatting and linting issues flagged by bots or CI must be resolved before requesting review or merging.
- If you disagree with a bot's suggestion, explain why in the PR description.
- If a bot's feedback is not addressed and a human reviewer must repeat the request, the PR will be closed until all automated feedback is resolved.
- All documentation and markdown reports must pass formatting checks. Use a markdown linter if available. 