# Contributing to Moq.Analyzers

Thank you for your interest in contributing to Moq.Analyzers! This document provides guidelines and requirements for contributing to this project. If you want to contribute to existing issues, check the
[help wanted](https://github.com/rjmurillo/moq.analyzers/labels/help%20wanted) or
[good first issue](https://github.com/rjmurillo/moq.analyzers/labels/good%20first%20issue) items in the backlog.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Workflow](#development-workflow)
- [Code Quality Standards](#code-quality-standards)
- [Testing Requirements](#testing-requirements)
- [Documentation Standards](#documentation-standards)
- [CI/CD and Performance Testing](#cicd-and-performance-testing)
- [Dependency Management](#dependency-management)
- [Pull Request Guidelines](#pull-request-guidelines)
- [Review Process](#review-process)
- [Release Process](#release-process)

## Code of Conduct

This project adheres to the [Contributor Covenant Code of Conduct](CODE-OF-CONDUCT.md). By participating, you are expected to uphold this code.

## Getting Started

1. **Fork the repository** and clone your fork locally
2. **Install dependencies**:
   ```bash
   dotnet restore
   ```
3. **Build the project**:
   ```bash
   dotnet build
   ```
4. **Run tests** to ensure everything works:
   ```bash
   dotnet test
   ```

## Development Workflow

### Branch Naming Convention

Use descriptive branch names following this pattern:
- `feature/issue-{number}` for new features
- `fix/issue-{number}` for bug fixes
- `docs/issue-{number}` for documentation changes
- `ci/issue-{number}` for CI/CD improvements
- `chore/issue-{number}` for maintenance tasks

### Commit Message Format

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

**Examples:**
```
feat(analyzer): add new Moq1001 analyzer for callback validation
fix(test): resolve flaky test in Moq1200AnalyzerTests
docs(readme): update installation instructions
ci(workflow): add performance testing to nightly builds
```

## Code Quality Standards

### Required Checks

Before submitting a PR, ensure your code passes all quality checks:

1. **Formatting**: Run `dotnet format` to ensure consistent code formatting
2. **Build**: Ensure `dotnet build` succeeds without warnings
3. **Tests**: All tests must pass (`dotnet test`)
4. **Static Analysis**: Run Codacy analysis locally or ensure CI passes
5. **Documentation**: Update relevant documentation files

### Code Style Guidelines

- Follow existing naming conventions and patterns
- Use descriptive variable and method names
- Keep methods focused and single-purpose
- Add XML documentation for public APIs
- Follow SOLID principles and DRY methodology
- Ensure all code paths are testable

## Testing Requirements

### Unit Tests

- **Every analyzer and fixer must have comprehensive unit tests**
- Use the project's data-driven test pattern for code fix tests
- Use `AllAnalyzersVerifier` for non-diagnostic tests
- Test all branches, edge cases, and failure paths
- Ensure tests are deterministic and focused

### Test Structure

```csharp
[TestClass]
public class MyAnalyzerTests
{
    [TestMethod]
    public async Task MyAnalyzer_Scenario_ShouldReportDiagnostic()
    {
        // Test implementation
    }
}
```

### Performance Testing

- **Performance-sensitive changes require benchmark validation**
- Run performance tests locally before submitting PRs
- Include performance regression analysis in PR description
- Follow the project's benchmarking patterns in `tests/Moq.Analyzers.Benchmarks/`

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

## CI/CD and Performance Testing

### CI Workflow Requirements

When making CI/CD changes:

1. **Test locally first**: Ensure workflows run successfully locally
2. **Include validation evidence**: Provide logs or screenshots showing successful execution
3. **Update documentation**: Document new CI features or changes
4. **Consider performance impact**: Ensure changes don't significantly impact CI duration

### Performance Testing Guidelines

**When Performance Testing is Required:**
- New analyzers or fixers
- Changes to existing analyzer logic
- Dependency updates that might affect performance
- CI/CD changes that impact build times

**Performance Testing Process:**
1. Run benchmarks locally using `dotnet run --project tests/Moq.Analyzers.Benchmarks/`
2. Compare results against baseline
3. Document any performance regressions or improvements
4. Include benchmark results in PR description

**Performance Validation Evidence:**
- Benchmark output showing no significant regressions
- Comparison with previous baseline results
- Explanation of any performance changes

## Dependency Management

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

### Validation Evidence Requirements

**What Constitutes Validation Evidence:**
- Test execution logs showing all tests pass
- Performance benchmark results (if applicable)
- Screenshots of successful CI runs
- Manual testing results for UI changes
- Code coverage reports for new features

**Evidence Format:**
- Include logs, screenshots, or links to CI runs
- Provide clear, readable evidence
- Ensure evidence is recent and relevant
- Link to specific test results or benchmarks

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

### Common Review Feedback

**Frequently Requested Changes:**
- Add missing tests for edge cases
- Update documentation for new features
- Improve error handling and logging
- Add performance benchmarks for new analyzers
- Clarify PR description or validation evidence

**PRs That May Be Rejected:**
- Missing validation evidence
- Incomplete test coverage
- Performance regressions without justification
- Security vulnerabilities
- Insufficient documentation updates

## Release Process

### Release Preparation

Before a release:

1. **Update release notes**: Move items from Unshipped.md to Shipped.md
2. **Update version**: Ensure version.json is current
3. **Run full test suite**: Ensure all tests pass
4. **Performance validation**: Run benchmarks to ensure no regressions
5. **Documentation review**: Ensure all documentation is current

### Release Checklist

- [ ] All tests pass
- [ ] Performance benchmarks show no regressions
- [ ] Documentation is current
- [ ] Release notes are complete
- [ ] Version is updated
- [ ] CI/CD is green

## Getting Help

If you need help with any aspect of contributing:

1. **Check existing documentation** in this file and the project README
2. **Search existing issues** for similar questions
3. **Create a new issue** for bugs or feature requests
4. **Join discussions** in existing PRs for questions

## Recognition

Contributors will be recognized in:
- Release notes for significant contributions
- Project README for major contributors
- GitHub contributors list

Thank you for contributing to Moq.Analyzers!

