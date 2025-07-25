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
- [Roslyn Analyzer Development](#roslyn-analyzer-development)
- [Git Commit Messages](#git-commit-messages)

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

## Universal Agent Success Principles for Project Maintainers

> **IMPORTANT:** These guidelines help project maintainers create environments where AI agents can be more successful, regardless of the specific agent platform or tools being used.

### Creating Agent-Friendly Project Environments

#### 1. Clear Expertise Validation Requirements

**Document specific expertise requirements:**
- **Define domain-specific knowledge** that agents must demonstrate before contributing
- **Create validation checklists** with specific technical questions
- **Establish clear criteria** for when agents should request expert guidance
- **Provide escalation paths** for complex or unclear situations

**Implementation:**
- Create comprehensive documentation of domain concepts
- Develop specific technical questions for expertise validation
- Establish clear guidelines for when to seek human expert guidance
- Document common failure patterns and their solutions

#### 2. Mandatory Workflow Documentation

**Create clear, enforceable workflows:**
- **Document all mandatory steps** in the development process
- **Provide validation checkpoints** that agents can verify
- **Create clear success criteria** for each workflow step
- **Establish rollback procedures** for failed workflows

**Implementation:**
- Document step-by-step workflows with clear success criteria
- Create automated validation scripts where possible
- Provide clear error messages and recovery procedures
- Establish checkpoints that agents can validate independently

#### 3. Configuration and Documentation Standards

**Make project context easily discoverable:**
- **Centralize configuration** in well-documented files
- **Create comprehensive documentation** of project structure and conventions
- **Establish clear naming conventions** and architectural patterns
- **Document decision-making processes** and design rationales

**Implementation:**
- Use consistent configuration file formats and locations
- Create comprehensive README files with clear project overview
- Document architectural decisions and their rationales
- Establish clear coding standards and conventions

#### 4. Validation and Testing Infrastructure

**Create robust validation systems:**
- **Automate testing and validation** where possible
- **Provide clear feedback** on validation failures
- **Create comprehensive test suites** that cover all critical paths
- **Establish performance benchmarks** for performance-sensitive code

**Implementation:**
- Set up automated CI/CD pipelines with comprehensive testing
- Create clear test documentation and examples
- Establish performance testing frameworks
- Provide clear error messages and debugging information

#### 5. Error Handling and Recovery

**Design systems for graceful failure handling:**
- **Create clear error messages** that guide agents toward solutions
- **Establish retry mechanisms** for transient failures
- **Provide rollback procedures** for failed changes
- **Document common failure patterns** and their solutions

**Implementation:**
- Use descriptive error messages with actionable guidance
- Implement retry logic for network and transient failures
- Create rollback procedures for database and configuration changes
- Document troubleshooting guides for common issues

#### 6. State Management and Context Preservation

**Design systems that preserve context:**
- **Use persistent storage** for important state information
- **Create clear state transition documentation**
- **Implement context recovery mechanisms**
- **Document state dependencies** and relationships

**Implementation:**
- Use databases or persistent storage for important state
- Document state transitions and their triggers
- Implement automatic context recovery after interruptions
- Create clear documentation of state dependencies

#### 7. Tool Integration and Documentation

**Provide comprehensive tool documentation:**
- **Document all available tools** and their capabilities
- **Create clear usage examples** for each tool
- **Establish tool integration patterns**
- **Provide troubleshooting guides** for tool failures

**Implementation:**
- Create comprehensive tool documentation with examples
- Establish clear patterns for tool integration
- Provide troubleshooting guides for common tool issues
- Create tool validation scripts where possible

#### 8. Expert Guidance Protocols

**Establish clear escalation procedures:**
- **Define when agents should seek expert guidance**
- **Create clear escalation paths** with contact information
- **Establish response time expectations**
- **Document expert availability** and areas of expertise

**Implementation:**
- Create clear criteria for when to escalate to human experts
- Establish contact procedures and response time expectations
- Document areas of expertise and availability
- Create escalation templates and procedures

### Best Practices for Agent Integration

#### Documentation Standards

- **Use clear, unambiguous language** in all documentation
- **Provide concrete examples** for all concepts and procedures
- **Create step-by-step guides** for complex processes
- **Include troubleshooting sections** in all documentation

#### Configuration Management

- **Use consistent configuration formats** across the project
- **Provide configuration validation** tools and scripts
- **Document configuration dependencies** and relationships
- **Create configuration templates** for common scenarios

#### Testing and Validation

- **Automate testing** wherever possible
- **Provide clear test documentation** and examples
- **Create comprehensive test coverage** for all critical paths
- **Establish performance benchmarks** and monitoring

#### Error Handling

- **Use descriptive error messages** with actionable guidance
- **Implement graceful degradation** for non-critical failures
- **Provide rollback procedures** for all changes
- **Create troubleshooting guides** for common issues

### Monitoring and Improvement

#### Success Metrics

- **Track agent success rates** and failure patterns
- **Monitor workflow completion rates** and bottlenecks
- **Collect feedback** on documentation and process clarity
- **Measure time to resolution** for common issues

#### Continuous Improvement

- **Regularly review and update** documentation and processes
- **Collect and analyze** agent feedback and failure patterns
- **Implement improvements** based on success metrics
- **Update expertise requirements** as the project evolves

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

### Strict Workflow Requirements

**Every pull request must pass these checks before review:**

- **Formatting:**  
  Run `dotnet format` and commit all changes. PRs with formatting issues will be rejected.
- **Build:**  
  Build with `dotnet build /p:PedanticMode=true`. All warnings must be treated as errors. PRs that do not build cleanly will be closed.
- **Tests:**  
  Run all unit tests:  
  `dotnet test --settings ./build/targets/tests/test.runsettings`  
  All tests must pass. PRs with failing tests will be closed.
- **Codacy Analysis:**  
  Run Codacy CLI analysis on all changed files. Fix all reported issues before submitting the PR.
- **Evidence Required:**  
  PR description must include console output or screenshots for:
  - `dotnet format`
  - `dotnet build`
  - `dotnet test`
  - Codacy analysis (if issues were found and fixed)
- **No Received Files:**  
  Remove any `*.received.*` files before committing.

**CI Pipeline:**  
- All PRs are validated by GitHub Actions.  
- PRs that fail CI (format, build, test, or Codacy) will be closed without review.

**Summary:**  
If your PR does not pass all checks locally and in CI, it will not be reviewed. Always verify and document your results before submitting.

### Proactive Duplicate/Conflict Checks

- After rebasing or merging, review for duplicate or conflicting changes, especially in shared files or properties. Remove redundant changes and note this in the PR description.

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

### Reasoning and Code Quality Principles

- **Never use chain-of-thought narration as a crutch.** All logic must be explicit, traceable, and enforced by code and tests. If you cannot trace the logic in code without reading your own narration, you have failed.
- Never simulate steps—show each one, no skipping. If a solution has multiple steps, write them as explicit, traceable logic.
- If a step can fail, code the failure path first and make it obvious. **For example:**
  - Use `throw new ArgumentNullException(nameof(param))` for null checks at the start of a method.
  - Use `Debug.Assert(condition)` or `Contract.Requires(condition)` to enforce invariants.
  - In tests, use `Assert.Throws<ExceptionType>(() => ...)` to verify error handling.
- Do not trust yourself to be right at the end. **Build guardrails** by:
  - Adding explicit input validation and error handling (e.g., guard clauses, early returns on invalid state).
  - Using static analyzers (e.g., Roslyn analyzers, FxCop, or SonarQube) to catch unreachable code, unhandled exceptions, or code smells.
  - Writing unit tests for every branch, including edge and fail paths.
- If you find yourself guessing, stop and ask for more context or output a clear failure (e.g., `throw new NotImplementedException("Unclear requirement: ...")`).
- Never copy-and-tweak analyzer patterns. Re-read the requirements before you start.

### Formatting, Linting, and Bot Feedback

- **You must address all feedback from automated bots (e.g., Codeclimate, formatting/linting bots) as you would human reviewers.**
- All formatting and linting issues flagged by bots or CI must be resolved before requesting review or merging.
- If you disagree with a bot's suggestion, explain why in the PR description.
- If a bot's feedback is not addressed and a human reviewer must repeat the request, the PR will be closed until all automated feedback is resolved.
- All documentation and markdown reports must pass formatting checks. Use a markdown linter if available.

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

### Data-Driven Testing for Code Fixes (MANDATORY PATTERN)

When testing code fixes that modify a class member (method, property, etc.), you **MUST** use the following data-driven pattern. This separates code snippets from boilerplate and enables combinatorial testing.

**1. Create a Data Source Method (`IEnumerable<object[]>`)**

Define a `public static IEnumerable<object[]>` method to provide test cases.

-   **Signature:** `public static IEnumerable<object[]> YourDataSourceMethod()`
-   **Content:** Return a `new object[][] { ... }`. Each inner `object[]` must contain two strings:
    1.  The original code snippet that triggers the analyzer (`brokenCode`).
    2.  The target code snippet after the code fix is applied (`fixedCode`).
-   **Helpers:** You **MUST** chain `.WithNamespaces().WithMoqReferenceAssemblyGroups()` to the collection to automatically generate test variations.

**Example:**
```csharp
public static IEnumerable<object[]> MakesNonVirtualMethodVirtualData()
{
    return new object[][]
    {
        [
            """public int MyMethod() => 0;""", // brokenCode
            """public virtual int MyMethod() => 0;""", // fixedCode
        ],
    }.WithNamespaces().WithMoqReferenceAssemblyGroups();
}
```

**2. Create the `[Theory]` Test Method**

Create an `async Task` method decorated with `[Theory]` and `[MemberData]`.

-   **Signature:** The signature **MUST** match the data source output: `async Task YourTestMethod(string referenceAssemblyGroup, string @namespace, string brokenCode, string fixedCode)`

**Example:**
```csharp
[Theory]
[MemberData(nameof(MakesNonVirtualMethodVirtualData))]
public async Task MakesNonVirtualMethodVirtual(string referenceAssemblyGroup, string @namespace, string brokenCode, string fixedCode)
{
    // ... test logic
}
```

**3. Define the Code Template (`Template` function)**

Inside the test method, define a `static` local function named `Template` that builds the full source code using a raw string literal.

-   **Placeholders:** The template **MUST** use `{{ns}}` for the namespace and `{{code}}` for the code snippet.
-   **Context:** The template **MUST** include all necessary `using` statements and class structures to create a valid, compilable test case. Note that `tests\Moq.Analyzers.Test\Helpers\Test.cs` inserts global usings common for tests.
-   **Diagnostic Marker:** The code that triggers the analyzer **MUST** be wrapped with `{|DIAGNOSTIC_ID: ... |}` (e.g., `{|Moq1210:...|}`). This is non-negotiable for the test verifier to work.

**Example:**
```csharp
static string Template(string ns, string code) =>
$$"""
{{ns}}

public class MyClass
{
    {{code}} // This is where brokenCode/fixedCode will be injected
}

public class MyTest
{
    public void Test()
    {
        var mock = new Mock<MyClass>();
        {|Moq1210:mock.Verify(x => x.MyMethod())|};
    }
}
""";
```

**4. Verify the Code Fix**

Use the `Template` function to generate the "before" and "after" source files and pass them to `Verify.VerifyCodeFixAsync`.

**Example:**
```csharp
string originalSource = Template(@namespace, brokenCode);
string fixedSource = Template(@namespace, fixedCode);

await Verify.VerifyCodeFixAsync(originalSource, fixedSource, referenceAssemblyGroup);
```

### AllAnalyzersVerifier for Comprehensive Testing

When writing tests that verify code patterns don't trigger unwanted diagnostics from **any** Moq analyzer, use the `AllAnalyzersVerifier` helper class:

```csharp
await AllAnalyzersVerifier.VerifyAllAnalyzersAsync(sourceCode, referenceAssemblyGroup);
```

**Key Benefits:**
- **Automatic Discovery**: Uses reflection to find all `DiagnosticAnalyzer` types in the `Moq.Analyzers` namespace
- **No Manual Maintenance**: New analyzers are automatically included without code changes
- **Comprehensive Coverage**: Tests against ALL analyzers simultaneously to ensure no false positives

**Important**: When you add a new analyzer, the `AllAnalyzersVerifier` automatically discovers and includes it. No manual updates to test infrastructure are required.

### Moq-Specific Testing Guidelines

**Test Data Grouping:**
- When adding or updating test data, group tests by Moq version compatibility:
  - Place tests for features only available in Moq 4.18.4+ in a "new" group.
  - Place tests for features available in both 4.8.2 and 4.18.4 in a "both" or "old" group.
  - Do not include tests for features/APIs that do not exist in the targeted Moq version.

**Moq Version Compatibility:**
- **Moq 4.8.2:**
  - Does _not_ support `SetupAdd`, `SetupRemove`, or `.Protected().Setup`.
  - Indexer setups are supported only for virtual or interface indexers.
  - Do _not_ add tests for APIs or patterns that do not exist in this version; such tests will fail at compile time, not at analyzer time.
- **Moq 4.18.4+:**
  - Supports `SetupAdd`, `SetupRemove`, and `.Protected().Setup`.
  - Allows setups for virtual events and indexers, and explicit interface implementations.
  - Tests for these features should be placed in the "new" test group and must not be expected to pass in "old" Moq test runs.
- **General:**
  - Never expect analyzer diagnostics for code that cannot compile due to missing APIs or language restrictions.
  - When in doubt, consult the official Moq documentation and changelogs for feature support.

**Required Moq Testing Patterns:**
- **Overridable Members Only:** Only set up or verify virtual, abstract, or interface members. Do **not** attempt to set up or verify non-virtual, static, or sealed members.
- **Events and Indexers:** Use `SetupAdd` and `SetupRemove` **only** for virtual events, and only in Moq 4.18.4+.
- **Explicit Interface Implementations:** Setups for explicit interface implementations must use the correct cast syntax (e.g., `((IMyInterface)x).Method()`).
- **Protected Members:** Use `.Protected().Setup(...)` **only** for protected virtual members, and only in Moq 4.18.4+.
- **Static, Const, and Readonly Members:** Moq cannot mock or set up static, const, or readonly members. Do **not** add analyzer tests for these scenarios.
- **Async Methods:** Moq supports setups for async methods returning `Task` or `ValueTask`. Always include both `Task` and `ValueTask` scenarios in analyzer tests.
- **Callback and Sequence Features:** If your analyzer or code fix interacts with `Callback` or `SetupSequence`, ensure you have tests for both single and sequence setups.
- **Mock Behavior and Verification:** Always specify and test for both `MockBehavior.Default` and `MockBehavior.Strict` where relevant.
- **InternalsVisibleTo:** If mocking internal members, ensure `[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]` is present in the test assembly.

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

### Evidence of Moq Version Awareness

- If your PR changes or adds analyzer tests, include a note in the PR description about which Moq versions are targeted and how test data is grouped accordingly.

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

## Roslyn Analyzer Development

### Mandatory Expertise Requirements

Before working on Roslyn analyzers, you must have expert-level understanding of:

- Roslyn syntax tree navigation from `SyntaxNode` down to `SyntaxToken` and `SyntaxTrivia`
- Precise, character-level diagnostic span calculation and verification
- The distinction and correct application of `IOperation` vs. `ISyntaxNode` analysis
- The implementation patterns of `CodeFixProvider` and `DocumentEditor`
- The specific domain of the Moq framework's verification and setup semantics

### Pre-Implementation Checklist

Before starting any analyzer implementation, verify you can answer:

1. **Can you trace the exact syntax tree path** from a `mock.Verify()` call to the specific member access (`x.MyMethod`) being invoked inside the lambda?
2. **Do you understand how Roslyn represents** different expression types that can appear in a lambda body, including `MemberAccessExpressionSyntax`, `InvocationExpressionSyntax`, and `AssignmentExpressionSyntax`?
3. **Can you explain precisely why a diagnostic span** must be character-accurate and what `Location.Create()` requires to function correctly?
4. **Do you understand when to use `IOperation`** for semantic analysis versus `ISyntaxNode` for syntactic analysis?

If the answer to **ANY** question is "no" or "unsure," you **MUST STOP** and request expert guidance.

### Critical Directives

- **Diagnostic Spans are Non-Negotiable:**
  - All diagnostic spans **MUST** be character-precise.
  - A test failure related to a diagnostic span (`Expected span ... but got ...`) is a **CRITICAL FAILURE**. It signals a fundamental misunderstanding of the syntax tree.
  - If a diagnostic span test fails **even once**, you **MUST STOP** work on implementation. Re-evaluate your entire syntax tree navigation logic. If it fails a second time, you must admit failure and request expert human guidance. Do not proceed.
- **No Trial-and-Error:**
  - Never guess which Roslyn API to use. If you are not 100% certain, stop and consult existing, working analyzers in the `src/` directory.
  - Never "fix" a failing test by slightly adjusting the code and re-running. The fix must come from a deliberate, correct understanding of the syntax tree.

### Implementation Requirements

- **MANDATORY:** Roslyn analyzer development requires deep understanding of:
  - Syntax tree navigation and manipulation
  - Diagnostic span precision (character-level accuracy)
  - IOperation vs ISyntaxNode usage patterns
  - Code fix provider implementation patterns
- If you lack this expertise, STOP and request guidance before proceeding
- Never attempt to "figure out" Roslyn patterns through trial and error

### Early Failure Detection

- If you cannot explain the exact syntax tree structure of the code you're analyzing, STOP
- If diagnostic span tests fail more than once, STOP and request expert guidance
- If you're making "educated guesses" about Roslyn APIs, STOP
- If test failures indicate you don't understand the domain (Moq verification semantics), STOP

### Repository Structure

- `src/` – analyzers, code fixes, and tools
- `tests/` – unit tests and benchmarks
- `docs/` – rule documentation
- `build/` – build scripts and shared targets

## Git Commit Messages

### Guidelines

1. **Capitalization and Punctuation**: Capitalize the first word and do not end in punctuation. If using Conventional Commits, remember to use all lowercase.
2. **Mood**: Use imperative mood in the subject line. Example – Add fix for dark mode toggle state. Imperative mood gives the tone you are giving an order or request.
3. **Type of Commit**: Specify the type of commit. It is recommended and can be even more beneficial to have a consistent set of words to describe your changes. Example: Bugfix, Update, Refactor, Bump, and so on. See the section on Conventional Commits below for additional information.
4. **Length**: The first line should ideally be no longer than 50 characters, and the body should be restricted to 72 characters.
5. **Content**: Be direct, try to eliminate filler words and phrases in these sentences (examples: though, maybe, I think, kind of). Think like a journalist.

### How to Find Your Inner Journalist

When writing an article they look to answer who, what, where, when, why and how. For committing purposes, it is most important to answer the what and why for our commit messages.

To come up with thoughtful commits, consider the following:

- Why have I made these changes?
- What effect have my changes made?
- Why was the change needed?
- What are the changes in reference to?
- Assume the reader does not understand what the commit is addressing. They may not have access to the story addressing the detailed background of the change.

Don't expect the code to be self-explanatory. This is similar to the point above.

It might seem obvious to you, the programmer, if you're updating something like CSS styles since it is visual. You may have intimate knowledge on why these changes were needed at the time, but it's unlikely you will recall why you did that hundreds of pull requests later.

Make it clear why that change was made, and note if it may be crucial for the functionality or not.

See the differences below:

Bad: `git commit -m 'Add margin'`
Good: `git commit -m 'Add margin to nav items to prevent them from overlapping the logo'`

It is clear which of these would be more useful to future readers.

Pretend you're writing an important newsworthy article. Give the headline that will sum up what happened and what is important. Then, provide further details in the body in an organized fashion.

In filmmaking, it is often quoted "show, don't tell" using visuals as the communication medium compared to a verbal explanation of what is happening.

In our case, "tell, don't [just] show" – though we have some visuals at our disposal such as the browser, most of the specifics come from reading the physical code.

When writing the commit, imagine how useful this could be in troubleshooting a bug or back-tracing changes made. 

Where possible, use **Conventional Commits**

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

The commit type subject line should be all lowercase with a character limit to encourage succinct descriptions.

The optional commit body should be used to provide further detail that cannot fit within the character limitations of the subject line description.

It is also a good location to utilize `BREAKING CHANGE: <description>` to note the reason for a breaking change within the commit.

The footer is also optional. We use the footer to link the GitHub issue that would be closed with these changes for example: `Closes #42`.

Example:

```
fix: fix foo to enable bar

This fixes the broken behavior of the component by doing xyz. 

BREAKING CHANGE
Before this fix foo wasn't enabled at all, behavior changes from <old> to <new>

Closes #12345
```

#### Commit Message Examples

**Good**

- `feat: improve performance with lazy load implementation for images`
- `chore: update npm dependency to latest version`
- `Fix bug preventing users from submitting the subscribe form`
- `Update incorrect client phone number within footer body per client request`

**Bad**

- `fixed bug on landing page`
- `Changed style`
- `oops`
- `I think I fixed it this time?`
- *empty commit messages*

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

