---
applyTo: '**/*.{sh,ps1}'
---

# Shell Script Instructions

> **MANDATORY:** You MUST follow these instructions when editing any shell script (.sh, .ps1) in this repository.

## Script Standards

### Script Quality Requirements

- Use consistent formatting and indentation
- Follow shell scripting best practices
- Implement proper error handling
- Use descriptive variable and function names
- Add appropriate comments and documentation
- Ensure scripts are portable and maintainable

### Script Types in Repository

This repository contains several types of shell scripts:

1. **Build scripts** - Build automation and CI/CD
2. **Installation scripts** - Tool installation and setup
3. **Performance testing scripts** - Benchmark execution
4. **Utility scripts** - Development and maintenance tasks

## Error Handling and Security

### Error Handling Requirements

- Implement proper error checking for all critical operations
- Use appropriate exit codes for different failure scenarios
- Provide clear error messages with actionable guidance
- Implement retry logic for transient failures
- Create rollback procedures for failed operations

### Security Considerations

- **Validate all inputs** before processing
- **Use secure practices** for handling sensitive information
- **Implement proper access controls** where applicable
- **Avoid command injection vulnerabilities**
- **Use secure defaults** for all configurations

### Script Security Guidelines

- Never execute user input directly
- Use parameterized commands where possible
- Validate file paths and inputs
- Implement proper logging for security events
- Use secure random number generation when needed

## Performance and Reliability

### Performance Requirements

- Optimize scripts for execution speed where appropriate
- Implement proper resource management
- Use efficient algorithms and data structures
- Monitor and log performance metrics
- Implement caching strategies where beneficial

### Reliability Standards

- Implement comprehensive error handling
- Use defensive programming practices
- Add appropriate logging and debugging information
- Test scripts thoroughly before deployment
- Implement proper cleanup procedures

## Code Quality Standards

### Required Checks

Before submitting a PR, ensure your changes pass all quality checks:

1. **Script Validation**: Ensure scripts execute without errors
2. **Security Review**: Check for security vulnerabilities
3. **Performance Testing**: Verify performance impact
4. **Error Handling**: Test error scenarios
5. **Documentation**: Update relevant documentation

### Script-Specific Requirements

- Use consistent coding style and formatting
- Follow shell scripting best practices
- Implement proper error handling and logging
- Use descriptive variable and function names
- Add appropriate comments and documentation

## Testing Requirements

### Script Testing Guidelines

- **Test all error paths** and edge cases
- **Verify script behavior** with different inputs
- **Test performance** under various conditions
- **Validate security** of script operations
- **Test cross-platform compatibility** where applicable

### Testing Process

1. **Unit Testing**: Test individual functions and components
2. **Integration Testing**: Test script interactions with other components
3. **Performance Testing**: Measure execution time and resource usage
4. **Security Testing**: Validate security measures and input handling
5. **Regression Testing**: Ensure existing functionality is not broken

## Documentation Standards

### Script Documentation Requirements

- **Document script purpose** and functionality
- **Explain parameters** and their expected values
- **Provide usage examples** for common scenarios
- **Document error conditions** and handling
- **Include troubleshooting information**

### Documentation Format

- Use clear, concise language
- Include code examples where appropriate
- Follow existing documentation patterns
- Ensure all examples are accurate and tested
- Update documentation when scripts change

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

- [ ] Scripts execute without errors
- [ ] Error handling is comprehensive
- [ ] Security measures are implemented
- [ ] Documentation is updated
- [ ] Performance impact is assessed
- [ ] CI checks pass
- [ ] PR description includes validation evidence

### Validation Evidence Requirements

**What Constitutes Validation Evidence:**
- Script execution logs showing successful completion
- Error handling test results
- Performance benchmark results
- Security validation output
- Screenshots of successful CI runs

**Evidence Format:**
- Include logs, screenshots, or links to CI runs
- Provide clear, readable evidence
- Ensure evidence is recent and relevant
- Link to specific test results or validation output

## Review Process

### What Maintainers Look For

Maintainers will review PRs for:

1. **Code Quality**: Adherence to project standards
2. **Security**: Proper security measures implemented
3. **Error Handling**: Comprehensive error handling
4. **Documentation**: Proper documentation updates
5. **Performance**: No significant performance regressions
6. **Validation**: Proper evidence of testing and validation

### Review Timeline

- **Initial review**: Within 2-3 business days
- **Follow-up reviews**: Within 1-2 business days after changes
- **Final approval**: After all concerns are addressed

### Common Review Feedback

**Frequently Requested Changes:**
- Add missing error handling
- Improve security measures
- Update documentation for script changes
- Add performance benchmarks
- Clarify PR description or validation evidence

**PRs That May Be Rejected:**
- Missing validation evidence
- Incomplete error handling
- Security vulnerabilities
- Performance regressions without justification
- Insufficient documentation updates

## Formatting and Linting

- **You must address all feedback from automated bots** (e.g., shell linters, formatting bots) as you would human reviewers.
- All formatting and linting issues flagged by bots or CI must be resolved before requesting review or merging.
- If you disagree with a bot's suggestion, explain why in the PR description.
- If a bot's feedback is not addressed and a human reviewer must repeat the request, the PR will be closed until all automated feedback is resolved.

## Script Best Practices

### Shell Scripting Guidelines

- Use shebang lines appropriately
- Implement proper argument parsing
- Use functions for reusable code
- Implement proper logging
- Use appropriate exit codes

### PowerShell Guidelines

- Use approved PowerShell verbs
- Implement proper parameter validation
- Use appropriate error handling
- Follow PowerShell best practices
- Implement proper logging and debugging

## Code of Conduct

This project adheres to the [Contributor Covenant Code of Conduct](CODE-OF-CONDUCT.md). By participating, you are expected to uphold this code. 