---
applyTo: '**/*.{yml,yaml}'
---

# YAML File Instructions

> **MANDATORY:** You MUST follow these instructions when editing any YAML file in this repository.

## CI/CD Workflow Requirements

### Workflow Configuration Standards

- Use consistent YAML formatting and indentation
- Follow GitHub Actions best practices
- Ensure proper workflow syntax
- Use descriptive job and step names
- Maintain consistent structure across workflows

### CI Workflow Requirements

When making CI/CD changes:

1. **Test locally first**: Ensure workflows run successfully locally
2. **Include validation evidence**: Provide logs or screenshots showing successful execution
3. **Update documentation**: Document new CI features or changes
4. **Consider performance impact**: Ensure changes don't significantly impact CI duration

### Workflow Validation

Before submitting CI/CD changes:

1. **Syntax Validation**: Ensure YAML syntax is correct
2. **Local Testing**: Test workflows locally when possible
3. **Performance Impact**: Assess impact on CI duration
4. **Security Review**: Ensure no security vulnerabilities are introduced
5. **Documentation Update**: Update relevant documentation

## Performance Testing Guidelines

### When Performance Testing is Required

- New analyzers or fixers
- Changes to existing analyzer logic
- Dependency updates that might affect performance
- CI/CD changes that impact build times

### Performance Testing Process

1. Run benchmarks locally using `dotnet run --project tests/Moq.Analyzers.Benchmarks/`
2. Compare results against baseline
3. Document any performance regressions or improvements
4. Include benchmark results in PR description

### Performance Validation Evidence

- Benchmark output showing no significant regressions
- Comparison with previous baseline results
- Explanation of any performance changes

## Security Considerations

### Security Scanning Requirements

- **All dependency updates require security scanning**
- Run Trivy scan after dependency changes
- Address any security vulnerabilities before merging
- Document security implications in PR description

### Security Workflow Configuration

- Configure security scanning in CI/CD workflows
- Set up automated vulnerability detection
- Ensure proper security reporting
- Configure security alerts and notifications

## Code Quality Standards

### Required Checks

Before submitting a PR, ensure your changes pass all quality checks:

1. **YAML Validation**: Ensure valid YAML syntax
2. **Workflow Testing**: Test workflows locally when possible
3. **Performance Impact**: Assess impact on CI duration
4. **Security Review**: Ensure no security vulnerabilities
5. **Documentation**: Update relevant documentation

### YAML-Specific Requirements

- Use consistent indentation (2 spaces recommended)
- Follow YAML best practices
- Ensure proper syntax and structure
- Use descriptive names for jobs, steps, and variables
- Maintain consistent formatting across workflows

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

- [ ] YAML syntax is valid
- [ ] Workflows work as expected
- [ ] Documentation is updated
- [ ] Performance impact is assessed
- [ ] Security implications are considered
- [ ] CI checks pass
- [ ] PR description includes validation evidence

### Validation Evidence Requirements

**What Constitutes Validation Evidence:**
- YAML validation output
- Workflow execution logs
- Performance benchmark results
- Screenshots of successful CI runs
- Manual testing results for workflow changes

**Evidence Format:**
- Include logs, screenshots, or links to CI runs
- Provide clear, readable evidence
- Ensure evidence is recent and relevant
- Link to specific test results or validation output

## Review Process

### What Maintainers Look For

Maintainers will review PRs for:

1. **Code Quality**: Adherence to project standards
2. **Workflow Accuracy**: Proper workflow configuration
3. **Documentation**: Proper documentation updates
4. **Performance**: No significant performance regressions
5. **Security**: No security vulnerabilities introduced
6. **Validation**: Proper evidence of testing and validation

### Review Timeline

- **Initial review**: Within 2-3 business days
- **Follow-up reviews**: Within 1-2 business days after changes
- **Final approval**: After all concerns are addressed

### Common Review Feedback

**Frequently Requested Changes:**
- Add missing workflow validation
- Update documentation for new workflows
- Improve error handling and logging
- Add performance benchmarks for new workflows
- Clarify PR description or validation evidence

**PRs That May Be Rejected:**
- Missing validation evidence
- Incomplete workflow testing
- Performance regressions without justification
- Security vulnerabilities
- Insufficient documentation updates

## Formatting and Linting

- **You must address all feedback from automated bots** (e.g., YAML linters, formatting bots) as you would human reviewers.
- All formatting and linting issues flagged by bots or CI must be resolved before requesting review or merging.
- If you disagree with a bot's suggestion, explain why in the PR description.
- If a bot's feedback is not addressed and a human reviewer must repeat the request, the PR will be closed until all automated feedback is resolved.

## Workflow Best Practices

### GitHub Actions Guidelines

- Use reusable workflows where appropriate
- Implement proper error handling and retry logic
- Use appropriate triggers for workflows
- Configure proper permissions for jobs
- Use caching to improve performance

### Workflow Security

- Use minimal required permissions
- Avoid hardcoding secrets in workflows
- Use GitHub secrets for sensitive information
- Implement proper access controls
- Regular security reviews of workflows

## Code of Conduct

This project adheres to the [Contributor Covenant Code of Conduct](CODE-OF-CONDUCT.md). By participating, you are expected to uphold this code. 