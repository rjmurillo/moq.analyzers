---
applyTo: '**/*.{sh,ps1}'
---

# Shell Script Instructions (Quick Reference)

- **Read this file before editing any .sh or .ps1 file**
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
- [yaml.instructions.md](yaml.instructions.md) - For CI/CD workflows
- [project.instructions.md](project.instructions.md) - For build configuration
- [text.instructions.md](text.instructions.md) - For plain text documentation

## Validation Checklist

Before submitting any changes, verify:

**Script Quality & Security:**
- [ ] Script executes without errors
- [ ] Error handling is comprehensive
- [ ] Security measures are implemented
- [ ] Performance impact is assessed

**Process:**
- [ ] Conventional commit format used
- [ ] PR description includes validation evidence
- [ ] All checklist items completed
- [ ] No breaking changes introduced (or documented if necessary)

## Decision Trees

### When to Request Human Review
- Is this a new script or automation? → Yes → Request expert guidance
- Is this a breaking change to build or deployment? → Yes → Document thoroughly and request review
- Are you uncertain about scripting or security? → Yes → Stop and request guidance

### When to Stop and Ask for Help
- Uncertain about scripting or security requirements
- Major changes to build or deployment process
- Security or legal implications

## Common Mistakes to Avoid

**DO NOT:**
- Skip validation steps
- Ignore security scanning after dependency changes
- Submit changes without validation evidence

**ALWAYS:**
- Read the entire instruction file first
- Validate all script and security changes
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
- You're making "educated guesses" about scripting or security
- You're uncertain about build or deployment process
- You cannot trace the logic in script without narration

**Escalation Process:**
1. Stop all work immediately
2. Document what you were trying to accomplish
3. Explain what specific aspect is unclear
4. Request expert guidance with specific questions
5. Do not proceed until you have clear, confident understanding

## Success Criteria

Your changes are successful when:
- All script and security validation checks pass
- Security scan is clean
- PR description is complete and accurate
- All checklist items completed
- No automated bot feedback ignored

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

## Test Data & Sample Inputs/Outputs

### What Constitutes Good Shell Script Test Data?
- Validate script runs without errors
- Include both working and intentionally broken examples
- Test for missing shebang, syntax errors, and invalid arguments
- Check for correct error handling and exit codes

### Example: Valid Script
```sh
#!/bin/bash
echo "Hello, world!"
```

### Example: Negative/Edge Case
```sh
echo "Hello, world!" # missing shebang
exit 1
```

### Coverage Strategy
- For every script change, test both valid and invalid scenarios
- Document test data rationale in comments or PR description 