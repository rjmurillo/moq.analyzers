---
applyTo: '**/*.json'
---

# JSON File Instructions (Quick Reference)

- **Read this file before editing any .json file**
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
- [yaml.instructions.md](yaml.instructions.md) - For CI/CD workflows
- [text.instructions.md](text.instructions.md) - For plain text configuration

## Validation Checklist

Before submitting any changes, verify:

**Configuration:**
- [ ] JSON syntax is valid
- [ ] Schema compliance is maintained
- [ ] All configuration standards followed
- [ ] Security scan completed after dependency changes

**Process:**
- [ ] Conventional commit format used
- [ ] PR description includes validation evidence
- [ ] All checklist items completed
- [ ] No breaking changes introduced (or documented if necessary)

## Decision Trees

### When to Request Human Review
- Is this a new configuration standard? → Yes → Request expert guidance
- Is this a breaking change to configuration? → Yes → Document thoroughly and request review
- Are you uncertain about JSON schema or configuration? → Yes → Stop and request guidance

### When to Stop and Ask for Help
- Uncertain about configuration requirements
- Major changes to project configuration
- Security or legal implications

## Common Mistakes to Avoid

**DO NOT:**
- Skip validation steps
- Ignore security scanning after dependency changes
- Submit changes without validation evidence

**ALWAYS:**
- Read the entire instruction file first
- Validate all configuration changes
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
- You're making "educated guesses" about configuration
- You're uncertain about project configuration
- You cannot trace the logic in configuration without narration

**Escalation Process:**
1. Stop all work immediately
2. Document what you were trying to accomplish
3. Explain what specific aspect is unclear
4. Request expert guidance with specific questions
5. Do not proceed until you have clear, confident understanding

## Success Criteria

Your changes are successful when:
- All configuration validation checks pass
- Security scan is clean
- PR description is complete and accurate
- All checklist items completed
- No automated bot feedback ignored

## Configuration Standards

### JSON Formatting

- Use consistent indentation (2 spaces recommended)
- Follow JSON schema standards where applicable
- Ensure valid JSON syntax
- Use descriptive property names
- Maintain consistent structure across similar files

### Configuration File Types

This repository contains several types of JSON configuration files:

1. **version.json** - Version management
2. **renovate.json** - Dependency management
3. **nuget.config** - NuGet package configuration
4. **global.json** - .NET SDK configuration
5. **Moq.Analyzers.lutconfig** - Project-specific configuration

## Version Management

### version.json Requirements

- Follow semantic versioning principles
- Update version numbers appropriately for changes
- Ensure version consistency across all project files
- Document version changes in release notes

### Version Update Guidelines

When updating version numbers:

1. **Check current version** in version.json
2. **Determine appropriate version increment** based on change type
3. **Update all related files** that reference version
4. **Update release notes** in AnalyzerReleases.Unshipped.md
5. **Test build process** with new version

## Dependency Management

### renovate.json Configuration

- Configure appropriate update schedules
- Set dependency update policies
- Include security scanning requirements
- Configure automated testing for updates

### Dependency Update Guidelines

**For Renovate/Dependabot PRs:**
- Review changelog and release notes
- Test locally to ensure compatibility
- Check for breaking changes
- Verify all tests pass with new dependency
- Include testing evidence in PR description

**For Manual Dependency Updates:**
- Follow the same process as automated updates
- Document the reason for the update
- Include compatibility testing results

### Security Considerations

- **All dependency updates require security scanning**
- Run Trivy scan after dependency changes
- Address any security vulnerabilities before merging
- Document security implications in PR description

## Build Configuration

### global.json Requirements

- Specify correct .NET SDK version
- Ensure compatibility with target framework (.NET 9)
- Maintain consistent SDK version across development team
- Update when new SDK versions are required

### NuGet Configuration

- Configure appropriate package sources
- Set package restore policies
- Ensure secure package sources
- Configure package signing where applicable

## Code Quality Standards

### Required Checks

Before submitting a PR, ensure your changes pass all quality checks:

1. **JSON Validation**: Ensure valid JSON syntax
2. **Schema Compliance**: Follow JSON schema where applicable
3. **Formatting**: Use consistent formatting
4. **Testing**: Verify configuration changes work as expected
5. **Documentation**: Update relevant documentation

### Configuration Validation

- **JSON Syntax:** Validate JSON syntax using appropriate tools
- **Schema Compliance:** Ensure compliance with JSON schemas
- **Functionality:** Test that configuration changes work as expected
- **Documentation:** Update documentation for configuration changes

## Git Commit Messages

### Guidelines

1. **Capitalization and Punctuation**: Capitalize the first word and do not end in punctuation. If using Conventional Commits, remember to use all lowercase.
2. **Mood**: Use imperative mood in the subject line.
3. **Type of Commit**: Specify the type of commit using conventional commit types.
4. **Length**: The first line should ideally be no longer than 50 characters, and the body should be restricted to 72 characters.
5. **Content**: Be direct, try to eliminate filler words and phrases.

### Conventional Commits

Follow the [Conventional Commits](https://www.conventionalcommits.org/) specification:

```
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

- [ ] JSON syntax is valid
- [ ] Configuration changes work as expected
- [ ] Documentation is updated
- [ ] Security implications are considered
- [ ] CI checks pass
- [ ] PR description includes validation evidence

### Validation Evidence Requirements

**What Constitutes Validation Evidence:**
- JSON validation output
- Configuration testing results
- Screenshots of successful CI runs
- Manual testing results for configuration changes

**Evidence Format:**
- Include validation logs, screenshots, or links to CI runs
- Provide clear, readable evidence
- Ensure evidence is recent and relevant
- Link to specific test results or validation output

## Review Process

### What Maintainers Look For

Maintainers will review PRs for:

1. **Code Quality**: Adherence to project standards
2. **Configuration Accuracy**: Proper configuration settings
3. **Documentation**: Proper documentation updates
4. **Security**: No security vulnerabilities introduced
5. **Validation**: Proper evidence of testing and validation

### Review Timeline

- **Initial review**: Within 2-3 business days
- **Follow-up reviews**: Within 1-2 business days after changes
- **Final approval**: After all concerns are addressed

## Formatting and Linting

- **You must address all feedback from automated bots** (e.g., JSON linters, formatting bots) as you would human reviewers.
- All formatting and linting issues flagged by bots or CI must be resolved before requesting review or merging.
- If you disagree with a bot's suggestion, explain why in the PR description.
- If a bot's feedback is not addressed and a human reviewer must repeat the request, the PR will be closed until all automated feedback is resolved.

## Test Data & Sample Inputs/Outputs

### What Constitutes Good JSON Test Data?
- Validate against schema (if available)
- Include both valid and invalid examples
- Test for missing required fields, extra fields, and type mismatches
- Check for correct handling of comments (if allowed)

### Example: Valid Config
```json
{
  "settingA": true,
  "maxItems": 10
}
```

### Example: Negative/Edge Case
```json
{
  "settingA": "yes", // invalid type
  // missing maxItems
}
```

### Coverage Strategy
- For every config change, validate with schema and test both valid and invalid cases
- Document test data rationale in comments or PR description

## Code of Conduct

This project adheres to the [Contributor Covenant Code of Conduct](CODE-OF-CONDUCT.md). By participating, you are expected to uphold this code. 