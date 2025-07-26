---
applyTo: '**/*.txt'
---

# Text File Instructions

> **MANDATORY:** You MUST follow these instructions when editing any text file (.txt) in this repository.

## Text File Standards

### Text File Formatting Requirements

- Use consistent formatting and structure
- Follow established patterns for similar files
- Ensure proper encoding (UTF-8 recommended)
- Use descriptive file names
- Maintain consistent line endings

### Text File Types in Repository

This repository contains several types of text files:

1. **Configuration files** - Various configuration settings
2. **Documentation files** - Plain text documentation
3. **Resource files** - Localization and resource management
4. **Log files** - Build and test output logs
5. **Banned symbols files** - Code analysis configuration

## Documentation Standards

### Text Documentation Requirements

- Use clear, concise language
- Follow established documentation patterns
- Ensure accuracy and completeness
- Update documentation when related code changes
- Maintain consistent formatting across similar files

### Documentation Format

- Use consistent structure and formatting
- Include appropriate headers and sections
- Use clear, descriptive language
- Provide examples where appropriate
- Maintain proper organization and readability

## Configuration Management

### Configuration File Standards

- Use consistent formatting and structure
- Follow established naming conventions
- Ensure proper validation and error handling
- Document configuration options clearly
- Maintain backward compatibility where possible

### Banned Symbols Configuration

For files like `BannedSymbols.txt`:

- Use clear, descriptive symbol names
- Include appropriate justification for bans
- Follow established formatting patterns
- Ensure accuracy and completeness
- Update when new symbols need to be banned

## Code Quality Standards

### Required Checks

Before submitting a PR, ensure your changes pass all quality checks:

1. **Text Validation**: Ensure proper formatting and structure
2. **Content Accuracy**: Verify accuracy of information
3. **Formatting**: Use consistent formatting
4. **Documentation**: Update relevant documentation
5. **Testing**: Verify text file changes work as expected

### Text-Specific Requirements

- Use consistent formatting and structure
- Follow established patterns and conventions
- Ensure proper encoding and line endings
- Use descriptive content and organization
- Maintain readability and clarity

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

- [ ] Text formatting is consistent
- [ ] Content is accurate and complete
- [ ] Documentation is updated
- [ ] Changes work as expected
- [ ] CI checks pass
- [ ] PR description includes validation evidence

### Validation Evidence Requirements

**What Constitutes Validation Evidence:**
- Text validation output
- Content accuracy verification
- Formatting consistency checks
- Screenshots of successful CI runs
- Manual verification of text file changes

**Evidence Format:**
- Include validation logs, screenshots, or links to CI runs
- Provide clear, readable evidence
- Ensure evidence is recent and relevant
- Link to specific test results or validation output

## Review Process

### What Maintainers Look For

Maintainers will review PRs for:

1. **Content Quality**: Accuracy and completeness of information
2. **Formatting**: Consistent formatting and structure
3. **Documentation**: Proper documentation updates
4. **Accuracy**: Correctness of information
5. **Validation**: Proper evidence of testing and validation

### Review Timeline

- **Initial review**: Within 2-3 business days
- **Follow-up reviews**: Within 1-2 business days after changes
- **Final approval**: After all concerns are addressed

### Common Review Feedback

**Frequently Requested Changes:**
- Fix formatting inconsistencies
- Improve content accuracy
- Update documentation quality
- Add missing information
- Clarify PR description or validation evidence

**PRs That May Be Rejected:**
- Inconsistent formatting
- Missing validation evidence
- Inaccurate or incomplete content
- Insufficient documentation updates
- Poor organization or structure

## Formatting and Linting

- **You must address all feedback from automated bots** (e.g., text linters, formatting bots) as you would human reviewers.
- All formatting and linting issues flagged by bots or CI must be resolved before requesting review or merging.
- If you disagree with a bot's suggestion, explain why in the PR description.
- If a bot's feedback is not addressed and a human reviewer must repeat the request, the PR will be closed until all automated feedback is resolved.

## Text File Best Practices

### Content Guidelines

- Use clear, descriptive language
- Include appropriate examples where helpful
- Follow established content patterns
- Ensure accuracy and completeness
- Update content when related code changes

### Organization Guidelines

- Use consistent structure and formatting
- Implement proper organization and hierarchy
- Maintain readability and clarity
- Use appropriate headers and sections
- Follow established naming conventions

### Configuration Guidelines

- Use consistent formatting and structure
- Follow established naming conventions
- Ensure proper validation and error handling
- Document configuration options clearly
- Maintain backward compatibility where possible

## Code of Conduct

This project adheres to the [Contributor Covenant Code of Conduct](CODE-OF-CONDUCT.md). By participating, you are expected to uphold this code. 