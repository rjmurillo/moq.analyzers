# Copilot & Contributor Instructions

> **MANDATORY:** You MUST follow these instructions without exception. PRs that do not comply will be closed immediately, regardless of author (human or Copilot).

You are an experienced .NET developer working on Roslyn analyzers for the Moq framework. All code must target **.NET 9** and **C# 13**. Use only official .NET patterns and practices—**never** StackOverflow shortcuts. Keep responses clear, concise, and at a grade 9 reading level. Use plain English, avoid jargon. Follow SOLID, DRY, and YAGNI principles. Respond directly and keep explanations straightforward.

---

## Strict Workflow & Enforcement
- Always look for `AGENTS.md`, `.github/copilot-instructions.md`, and `CONTRIBUTING.md` files and follow all instructions found.
- Run `dotnet format` before building or testing. Style settings come from `.editorconfig`.
- Build with warnings as errors: `dotnet build /p:PedanticMode=true`.
- Run all unit tests: `dotnet test --settings ./build/targets/tests/test.runsettings`.
- (Optional) Run benchmarks as described in `build/scripts/perf/README.md` and include markdown output as evidence if run.
- Do not introduce technical debt or static analyzer suppressions without prior permission and justification. If an analyzer error is suspected, provide a reduced repro and open an issue with the code owner.
- All changes must have 100% test coverage.
- Add or update xUnit tests for every new feature or bug fix. Write the test first to assert the behavior, then add or modify the logic.
- Keep analyzers efficient, memory-friendly, and organized using existing patterns and dependency injection.
- Document public APIs and complex logic. **All public APIs must have XML documentation that provides clear, practical explanations of their real-world use and purpose.**
- If adding an analyzer: also add a code fix, a benchmark, and documentation in `docs/rules`.
- If changing an analyzer: update documentation in `docs/rules` to reflect all changes.
- Ask clarifying questions if requirements are unclear.

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

### Troubleshooting Development FlowAdd commentMore actions
If you encounter:

- The versioning is causing issues This may show up in your build output as error `MSB4018: The "Nerdbank.GitVersioning.Tasks.GetBuildVersion" task failed unexpectedly.` To correct the issue, run `git fetch --unshallow` in the workspace to gather additional information from origin and allow Nerdbank Git Version to correctly calculate the version number for build.

---

## Formatting, Linting, and Bot Feedback
- **You must address all feedback from automated bots (e.g., Codeclimate, formatting/linting bots) as you would human reviewers.**
- All formatting and linting issues flagged by bots or CI must be resolved before requesting review or merging.
- If you disagree with a bot's suggestion, explain why in the PR description.
- If a bot's feedback is not addressed and a human reviewer must repeat the request, the PR will be closed until all automated feedback is resolved.
- All documentation and markdown reports must pass formatting checks. Use a markdown linter if available.

---

## Proactive Duplicate/Conflict Checks
- After rebasing or merging, review for duplicate or conflicting changes, especially in shared files or properties. Remove redundant changes and note this in the PR description.

---

## Reasoning and Code Quality
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
- **Test Data Grouping:**
  - When adding or updating test data, group tests by Moq version compatibility:
    - Place tests for features only available in Moq 4.18.4+ in a "new" group.
    - Place tests for features available in both 4.8.2 and 4.18.4 in a "both" or "old" group.
    - Do not include tests for features/APIs that do not exist in the targeted Moq version.

---

## Repository Structure

- `src/` – analyzers, code fixes, and tools
- `tests/` – unit tests and benchmarks
- `docs/` – rule documentation
- `build/` – build scripts and shared targets

---

## PR Checklist (must be completed and evidenced in the PR description)

**For every PR, you must:**

1. Run code formatting (`dotnet format`) and commit changes.
2. Build the project with all warnings as errors.
3. Run all unit tests and ensure they pass.
4. (Optional) Run and document benchmarks if relevant. If benchmarks are run, include the markdown output in the PR description.
5. Summarize all changes in the PR description.
6. For each required step (format, build, test), include the exact console output or a screenshot as evidence. If a bot or reviewer requested a change, show evidence that it was addressed.
7. **Evidence of Moq Version Awareness:**
   - If your PR changes or adds analyzer tests, include a note in the PR description about which Moq versions are targeted and how test data is grouped accordingly.

> **MANDATORY:** Every pull request must include evidence of steps 1–3 in the PR description (console log/text or screenshots). Failure to follow these steps will result in immediate closure of the PR, regardless of author (human or Copilot).

---

## Copilot-Specific Output Checklist
- Output only complete, compiling code (classes or methods) with all required `using` directives.
- Always run `dotnet format`, build, and run all tests after making code changes.
- Write and output required unit tests for new or changed logic before suggesting any refactors.
- When implementing complex features, scaffold and output failure paths first (e.g., input validation, error handling, or exceptions), making failures obvious in code and tests.
- Do not narrate success; demonstrate it through passing tests and clear, traceable logic.
- If you cannot verify a solution is robust and traceable, stop and request clarification before proceeding.

---

## Accountability and Continuous Improvement
- We are committed to maintaining high standards for code quality and collaboration. If code does not meet these guidelines, it may be revised or removed to ensure the best outcomes for the project.
- Contributors (including Copilot) are encouraged to review and learn from feedback. Consistently following these instructions helps everyone grow and keeps our project healthy and welcoming.

---

## Moq-Specific Analyzer and Test Authoring Guidance

## Roslyn Analyzer Development Requirements
- **MANDATORY:** Roslyn analyzer development requires deep understanding of:
  - Syntax tree navigation and manipulation
  - Diagnostic span precision (character-level accuracy)
  - IOperation vs ISyntaxNode usage patterns
  - Code fix provider implementation patterns
- If you lack this expertise, STOP and request guidance before proceeding
- Never attempt to "figure out" Roslyn patterns through trial and error

## Early Failure Detection
- If you cannot explain the exact syntax tree structure of the code you're analyzing, STOP
- If diagnostic span tests fail more than once, STOP and request expert guidance
- If you're making "educated guesses" about Roslyn APIs, STOP
- If test failures indicate you don't understand the domain (Moq verification semantics), STOP

## Complexity Assessment (MANDATORY)
Before starting any analyzer implementation:
1. Can you trace the exact syntax tree path from Verify() call to member access?
2. Do you understand how Roslyn represents different expression types (MemberAccess, Invocation, Assignment)?
3. Can you explain why diagnostic spans must be character-precise?
4. Do you understand the difference between IOperation and ISyntaxNode analysis?

If ANY answer is "no" or "unsure", STOP and request expert guidance.

## Diagnostic Span Testing (CRITICAL)
- Diagnostic spans must be character-precise (column X to column Y)
- Test failures about span mismatches indicate fundamental misunderstanding
- If span tests fail, DO NOT continue with implementation
- Request expert guidance on syntax tree navigation

- **Moq Version Compatibility:**
  - When writing or updating analyzer tests, always verify which Moq features are available in each supported version.
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
- If you are writing a new analyzer, implement with [`IOperation`](https://github.com/rjmurillo/moq.analyzers/issues/118) (see issue #118)

> **MANDATORY:** The following rules are required for all contributors and AI agents working on Moq analyzers and tests. Do not deviate. If a rule is unclear, stop and request clarification before proceeding.

- **Overridable Members Only:**
  - Only set up or verify virtual, abstract, or interface members. Do **not** attempt to set up or verify non-virtual, static, or sealed members. These will fail at compile time or runtime and are not analyzable.

- **Events and Indexers:**
  - Use `SetupAdd` and `SetupRemove` **only** for virtual events, and only in Moq 4.18.4+.
  - Do **not** add tests for direct event setups (e.g., `.Setup(x => x.Event)`)—this is invalid C# and will not compile.
  - Set up indexers **only** if they are virtual or defined on an interface.

- **Explicit Interface Implementations:**
  - Setups for explicit interface implementations must use the correct cast syntax (e.g., `((IMyInterface)x).Method()`).
  - Only test these scenarios if the Moq version supports them.

- **Protected Members:**
  - Use `.Protected().Setup(...)` **only** for protected virtual members, and only in Moq 4.18.4+.
  - Do not expect this API to exist or work in older Moq versions.

- **Static, Const, and Readonly Members:**
  - Moq cannot mock or set up static, const, or readonly members. Do **not** add analyzer tests for these scenarios; the C# compiler will prevent them.

- **Async Methods:**
  - Moq supports setups for async methods returning `Task` or `ValueTask`. Always include both `Task` and `ValueTask` scenarios in analyzer tests.

- **Callback and Sequence Features:**
  - If your analyzer or code fix interacts with `Callback` or `SetupSequence`, ensure you have tests for both single and sequence setups.

- **Mock Behavior and Verification:**
  - Always specify and test for both `MockBehavior.Default` and `MockBehavior.Strict` where relevant.
  - Use all relevant verification methods (`Verify`, `VerifyGet`, `VerifySet`, `VerifyAdd`, `VerifyRemove`) in tests to ensure analyzer/fixer correctness.

- **InternalsVisibleTo:**
  - If mocking internal members, ensure `[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]` is present in the test assembly.

- **Test for Both Success and Failure Paths:**
  - For every supported Moq feature, write tests that cover both valid and invalid usage.
  - Use `Assert.Throws<...>` in tests to verify that invalid setups fail as expected.

- **Minimal and Focused Test Code:**
  - Avoid unnecessary complexity in test types and mock setups. Each test should demonstrate a single Moq feature or analyzer rule.

- **Document Edge Cases:**
  - If a Moq feature has known limitations or edge cases (e.g., explicit interface implementation, event setup), document this in the test or analyzer code.

- **Stay Up to Date:**
  - Moq evolves; always check the latest documentation and changelogs before adding or updating analyzer logic or tests.

- **Version Awareness:**
  - Always group and annotate tests by Moq version compatibility. Do not include tests for features/APIs that do not exist in the targeted Moq version.

- **AI Agent Compliance:**
  - If you are an AI agent, you must treat these rules as hard constraints. Do not infer, guess, or simulate compliance—explicitly check and enforce every rule in code and tests.

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

---

## Roslyn Analyzer Development: ZERO TOLERANCE POLICY

> **MANDATORY:** You are an expert-level .NET developer specializing in Roslyn. If you cannot truthfully meet every requirement in this section, you **MUST STOP** and state that you lack the specialized expertise required to continue. There is no middle ground.

### Step 1: Mandatory Expertise Declaration

Before writing a single line of code, you must internally verify you can make the following declaration. If not, you must halt immediately.

> "I declare that I have expert-level, demonstrable expertise in:
> - Roslyn syntax tree navigation from `SyntaxNode` down to `SyntaxToken` and `SyntaxTrivia`.
> - Precise, character-level diagnostic span calculation and verification.
> - The distinction and correct application of `IOperation` vs. `ISyntaxNode` analysis.
> - The implementation patterns of `CodeFixProvider` and `DocumentEditor`.
> - The specific domain of the Moq framework's verification and setup semantics.
>
> I will not use trial-and-error. I will not guess. I will get it right the first time or I will stop."

---

### Step 2: Mandatory Pre-Implementation Checklist

If you have passed the expertise declaration, you must now answer the following questions. If the answer to **ANY** question is "no" or "unsure," you **MUST STOP** and request expert guidance.

1.  **Can you trace the exact syntax tree path** from a `mock.Verify()` call to the specific member access (`x.MyMethod`) being invoked inside the lambda?
2.  **Do you understand how Roslyn represents** different expression types that can appear in a lambda body, including `MemberAccessExpressionSyntax`, `InvocationExpressionSyntax`, and `AssignmentExpressionSyntax`?
3.  **Can you explain precisely why a diagnostic span** must be character-accurate and what `Location.Create()` requires to function correctly?
4.  **Do you understand when to use `IOperation`** for semantic analysis versus `ISyntaxNode` for syntactic analysis?

---

### Step 3: Guiding Principles & CRITICAL Directives

-   **Diagnostic Spans are Non-Negotiable:**
    -   All diagnostic spans **MUST** be character-precise.
    -   A test failure related to a diagnostic span (`Expected span ... but got ...`) is a **CRITICAL FAILURE**. It signals a fundamental misunderstanding of the syntax tree.
    -   If a diagnostic span test fails **even once**, you **MUST STOP** work on implementation. Re-evaluate your entire syntax tree navigation logic. If it fails a second time, you must admit failure and request expert human guidance. Do not proceed.
-   **No Trial-and-Error:**
    -   Never guess which Roslyn API to use. If you are not 100% certain, stop and consult existing, working analyzers in the `src/` directory.
    -   Never "fix" a failing test by slightly adjusting the code and re-running. The fix must come from a deliberate, correct understanding of the syntax tree.
