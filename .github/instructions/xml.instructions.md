---
applyTo: '**/*.xml'
---

# XML File Instructions (Quick Reference)

- **Read this file before editing any .xml file**
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

- [project.instructions.md](project.instructions.md) - For project and build configuration
- [csharp.instructions.md](csharp.instructions.md) - For C# code changes
- [text.instructions.md](text.instructions.md) - For plain text documentation

## Validation Checklist

Before submitting any changes, verify:

**XML Quality & Documentation:**

- [ ] XML syntax is valid
- [ ] Schema compliance is maintained
- [ ] Documentation is updated
- [ ] Changes work as expected

**Process:**

- [ ] Conventional commit format used
- [ ] PR description includes validation evidence
- [ ] All checklist items completed
- [ ] No breaking changes introduced (or documented if necessary)

## Decision Trees

### When to Request Human Review

- Is this a new XML schema or documentation standard? → Yes → Request expert guidance
- Is this a breaking change to XML structure or documentation? → Yes → Document thoroughly and request review
- Are you uncertain about XML schema or documentation? → Yes → Stop and request guidance

### When to Stop and Ask for Help

- Uncertain about XML requirements
- Major changes to project configuration
- Security or legal implications

## Common Mistakes to Avoid

**DO NOT:**

- Skip validation steps
- Submit changes without validation evidence

**ALWAYS:**

- Read the entire instruction file first
- Validate all XML and documentation changes
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
- You're making "educated guesses" about XML or documentation
- You're uncertain about project configuration
- You cannot trace the logic in XML without narration

**Escalation Process:**

1. Stop all work immediately
2. Document what you were trying to accomplish
3. Explain what specific aspect is unclear
4. Request expert guidance with specific questions
5. Do not proceed until you have clear, confident understanding

## Success Criteria

Your changes are successful when:

- All XML and documentation validation checks pass
- PR description is complete and accurate
- All checklist items completed
- No automated bot feedback ignored

## XML Standards

### XML Formatting Requirements

- Use consistent indentation (2 spaces recommended)
- Follow XML schema standards where applicable
- Ensure valid XML syntax
- Use descriptive element and attribute names
- Maintain consistent structure across similar files

### XML File Types in Repository

This repository contains several types of XML files:

1. **Project files** - MSBuild project configurations
2. **Documentation files** - XML documentation for APIs
3. **Configuration files** - Various configuration settings
4. **Resource files** - Localization and resource management

## Documentation Standards

### XML Documentation Requirements

**Required for all public APIs:**

- **Use `<see cref=".." />` tags for all type references** instead of plain text
  - Good: `<see cref="Task{T}"/>` or `<see cref="MoqKnownSymbols"/>`
  - Bad: `Task<T>` or `MoqKnownSymbols`
- **Use `<see langword=".." />` for C# keywords**
  - Good: `<see langword="true"/>` or `<see langword="null"/>`
  - Bad: `true` or `null`
- **Use `<paramref name=".." />` for parameter references**
  - Good: `<paramref name="mockedMemberSymbol"/>`
  - Bad: `mockedMemberSymbol`
- **Use `<c>..</c>` for inline code snippets**
  - Good: `<c>x => x.Method()</c>`
  - Bad: `x => x.Method()`

**Examples:**

```xml
/// <summary>
/// Determines whether a member symbol is either overridable or represents a
/// <see cref="Task{T}"/>/<see cref="ValueTask{T}"/> Result property
/// that Moq allows to be setup even if the underlying <see cref="Task{T}"/>
/// property is not overridable.
/// </summary>
/// <param name="mockedMemberSymbol">The mocked member symbol.</param>
/// <param name="knownSymbols">A <see cref="MoqKnownSymbols"/> instance for resolving well-known types.</param>
/// <returns>
/// Returns <see langword="true"/> when the member is overridable or is a
/// <see cref="Task{T}"/>/<see cref="ValueTask{T}"/> Result property;
/// otherwise <see langword="false" />.
/// </returns>
```

**Validation:**

- All public APIs must have complete XML documentation
- All type references must use `<see cref=".." />` tags
- All C# keywords must use `<see langword=".." />` tags
- Documentation must be accurate and up-to-date

## Project Configuration

### MSBuild Project Files

- Follow MSBuild best practices
- Use consistent property naming
- Maintain proper dependency references
- Ensure build targets are correctly configured
- Use appropriate target framework specifications

### Configuration Standards

- Use consistent XML structure and formatting
- Follow established naming conventions
- Ensure proper validation and error handling
- Maintain backward compatibility where possible
- Document configuration changes appropriately

## Code Quality Standards

### Required Checks

Before submitting a PR, ensure your changes pass all quality checks:

1. **XML Validation**: Ensure valid XML syntax
2. **Schema Compliance**: Follow XML schema where applicable
3. **Formatting**: Use consistent formatting
4. **Documentation**: Update relevant documentation
5. **Testing**: Verify XML changes work as expected

### XML-Specific Requirements

- Use consistent indentation and formatting
- Follow XML best practices and standards
- Ensure proper validation and error handling
- Use descriptive element and attribute names
- Maintain consistent structure across files

## Git Commit Messages

### Guidelines

1. **Capitalization and Punctuation**: Capitalize the first word and do not end in punctuation. If using Conventional Commits, remember to use all lowercase.
2. **Mood**: Use imperative mood in the subject line.
3. **Type of Commit**: Specify the type of commit using conventional commit types.
4. **Length**: The first line should ideally be no longer than 50 characters, and the body should be restricted to 72 characters.
5. **Content**: Be direct, try to eliminate filler words and phrases.

### Conventional Commits

Follow the [Conventional Commits](https://www.conventionalcommits.org/) specification:

```text
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

**Types:**

- `feat`: New features
- `fix`: Bug fixes
- `docs`: Documentation changes
- `style`: Code style changes (formatting, etc.)
- `refactor`: Code refactoring
- `test`: Adding or updating tests
- `chore`: Maintenance tasks
- `ci`: CI/CD changes
- `perf`: Performance improvements
- `build`: Build system changes

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

- [ ] XML syntax is valid
- [ ] Schema compliance is maintained
- [ ] Documentation is updated
- [ ] Changes work as expected
- [ ] CI checks pass
- [ ] PR description includes validation evidence

### Validation Evidence Requirements

**What Constitutes Validation Evidence:**

- XML validation output
- Schema compliance verification
- Documentation accuracy checks
- Screenshots of successful CI runs
- Manual testing results for XML changes

**Evidence Format:**

- Include validation logs, screenshots, or links to CI runs
- Provide clear, readable evidence
- Ensure evidence is recent and relevant
- Link to specific test results or validation output

## Review Process

### What Maintainers Look For

Maintainers will review PRs for:

1. **Code Quality**: Adherence to project standards
2. **XML Accuracy**: Proper XML structure and syntax
3. **Documentation**: Proper documentation updates
4. **Schema Compliance**: Adherence to XML schemas
5. **Validation**: Proper evidence of testing and validation

### Review Timeline

- **Initial review**: Within 2-3 business days
- **Follow-up reviews**: Within 1-2 business days after changes
- **Final approval**: After all concerns are addressed

### Common Review Feedback

**Frequently Requested Changes:**

- Fix XML syntax errors
- Improve documentation quality
- Update schema compliance
- Add missing validation
- Clarify PR description or validation evidence

**PRs That May Be Rejected:**

- Invalid XML syntax
- Missing validation evidence
- Schema compliance issues
- Insufficient documentation updates
- Poor formatting or structure

## Formatting and Linting

- **You must address all feedback from automated bots** (e.g., XML linters, formatting bots) as you would human reviewers.
- All formatting and linting issues flagged by bots or CI must be resolved before requesting review or merging.
- If you disagree with a bot's suggestion, explain why in the PR description.
- If a bot's feedback is not addressed and a human reviewer must repeat the request, the PR will be closed until all automated feedback is resolved.

## XML Best Practices

### Documentation Guidelines

- Use clear, descriptive text for all documentation
- Include examples where appropriate
- Follow established documentation patterns
- Ensure accuracy and completeness
- Update documentation when APIs change

### Configuration Guidelines

- Use consistent naming conventions
- Implement proper validation
- Provide clear error messages
- Maintain backward compatibility
- Document configuration options

## Code of Conduct

This project adheres to the [Contributor Covenant Code of Conduct](CODE-OF-CONDUCT.md). By participating, you are expected to uphold this code.
