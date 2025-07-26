---
applyTo: '**/*.{csproj,sln}'
---

# Project File Instructions

> **MANDATORY:** You MUST follow these instructions when editing any project file (.csproj, .sln) in this repository.

## Build Configuration Standards

### Target Framework Requirements

- **Target .NET 9, C# 13**
- Use only official .NET patterns and practices
- Ignore StackOverflow shortcuts
- Ensure compatibility with the specified framework version

### Project Structure

- Follow existing project organization patterns
- Maintain consistent naming conventions
- Ensure proper project references and dependencies
- Follow the established directory structure

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

### Dependency Validation

Before updating any dependencies:

1. **Check compatibility** with target framework (.NET 9)
2. **Verify breaking changes** in release notes
3. **Test locally** with the new dependency version
4. **Run security scans** using Trivy
5. **Update documentation** if necessary

## Build System Requirements

### Build Configuration

- Ensure `dotnet build` succeeds without warnings
- Use `/p:PedanticMode=true` for strict builds
- Treat all warnings as errors
- Maintain consistent build configurations across projects

### Project File Standards

- Use consistent property naming
- Follow established patterns for project references
- Maintain proper dependency versions
- Ensure build targets are correctly configured

## Code Quality Standards

### Required Checks

Before submitting a PR, ensure your changes pass all quality checks:

1. **Formatting**: Run `dotnet format` to ensure consistent code formatting
2. **Build**: Ensure `dotnet build` succeeds without warnings
3. **Tests**: All tests must pass (`dotnet test`)
4. **Static Analysis**: Run Codacy analysis locally or ensure CI passes
5. **Documentation**: Update relevant documentation files

### Build Validation

- **Formatting:** Run `dotnet format` and commit all changes
- **Build:** Build with `dotnet build /p:PedanticMode=true`
- **Tests:** Run all unit tests with `dotnet test --settings ./build/targets/tests/test.runsettings`
- **Codacy Analysis:** Run Codacy CLI analysis on all changed files

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
- Build output showing successful compilation
- Screenshots of successful CI runs
- Dependency compatibility test results

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

## Code of Conduct

This project adheres to the [Contributor Covenant Code of Conduct](CODE-OF-CONDUCT.md). By participating, you are expected to uphold this code. 