---
applyTo: '**/*.cs'
---

# C# File Instructions

> **MANDATORY:** You MUST follow these instructions when editing any C# file in this repository.

- **For complex changes, see the Decision Trees section below**

## MANDATORY: Only Valid C# Code in All Tests

> **You MUST NEVER write or include code in analyzer or code fix tests that produces C# compiler errors.**
>
> - All test code must be valid, compilable C#.
> - Do not write tests for static, const, readonly, or event members if the code would not compile.
> - Do not include code that triggers CSxxxx errors (e.g., invalid member access, missing members, or illegal syntax).
> - If a scenario cannot be expressed as valid C#, it is not a valid test case for analyzers or code fixes.
> - Any test that fails to compile is an immediate failure and must be removed or rewritten.

**Rationale:**

- Roslyn analyzers and code fixes only operate on valid, successfully parsed C# code. Compiler errors prevent analyzers from running and invalidate the test scenario.
- Writing invalid code in tests causes build failures, test failures, and wastes review/CI resources.
- This is a non-negotiable rule. If you are unsure whether a test is valid C#, STOP and request expert guidance.

## Primary Instructions

Always read and apply the instructions in [.github/copilot-instructions.md](../copilot-instructions.md) when working on C# source or project files.

## Additional Context

This instruction file works in conjunction with the comprehensive C# development guidelines in the main copilot-instructions.md file. The main file contains detailed requirements for:

- Roslyn analyzer development
- Code quality standards
- Testing requirements
- Performance considerations
- XML documentation standards
- Workflow requirements

## File-Specific Requirements

When working with C# files, ensure you also review:

- **Project files**: See [project.instructions.md](project.instructions.md) for .csproj and .sln files
- **XML files**: See [xml.instructions.md](xml.instructions.md) for XML documentation and configuration
- **Documentation**: See [markdown.instructions.md](markdown.instructions.md) for documentation updates

## Maintenance Note

If you update guidance in copilot-instructions.md that affects C# development, ensure consistency across all related instruction files.

## Decision Trees for Complex Scenarios

### Multi-File/Feature Change Flowchart

1. **Identify all affected file types**
2. **For each file type:**
   - Locate and read the corresponding instruction file
   - Note any validation, documentation, or escalation requirements
3. **Plan the change:**
   - List all files to be edited
   - Determine order of operations (e.g., code first, then docs, then config)
   - Identify dependencies between files
4. **Edit files in logical order**
5. **After each file edit:**
   - Run required validation (build, test, lint, etc.)
   - Document evidence as required
6. **After all edits:**
   - Re-run all tests and validations
   - Update documentation and release notes as needed
   - Prepare a comprehensive PR description with evidence for each file type
7. **If blocked or uncertain at any step:**
   - Escalate by tagging @repo-maintainers

### Introducing a New Analyzer or Code Fix Flowchart

1. **Read csharp.instructions.md and project.instructions.md**
2. **Scaffold the new analyzer/fixer in the correct directory**
3. **Add or update unit tests (see test instructions)**
4. **Update documentation:**
   - Add/modify rule docs in docs/rules/
   - Update AnalyzerReleases.Unshipped.md
5. **Update project files if needed**
6. **Run all validations (build, test, lint, Codacy, etc.)**
7. **Validate code coverage of your changed code**
8. **Prepare PR with validation evidence for each file type**
9. **If any diagnostic span or test fails more than once, STOP and escalate**
10. **If uncertain about Roslyn APIs, Moq semantics, or workflow, escalate**

## Diagnostic Investigation for Analyzer Failures

### When Symbol-Based Detection Tests Fail

If tests fail after adding/modifying symbol-based detection:

1. **Create Temporary Diagnostic Test:**

   ```csharp
   [TestMethod]
   public async Task DiagnosticSymbolTest()
   {
       string code = """
           var mock = new Mock<ITestInterface>();
           mock.Setup(x => x.Method()).Raises(x => x.Event += null, EventArgs.Empty);
           """;

       var (compilation, semanticModel, invocation) = await GetSemanticInfo(code);
       var symbolInfo = semanticModel.GetSymbolInfo(invocation);

       // Output actual symbol type to understand what's missing
       Console.WriteLine($"Symbol: {symbolInfo.Symbol?.ContainingType}");
   }
   ```

2. **Compare Against Registry:** Check if symbol type exists in `MoqKnownSymbols`
3. **Add Missing Symbol:** Register in `MoqKnownSymbols` with proper generic arity
4. **Delete Diagnostic Test:** Always clean up temporary investigation code

### Common Symbol Detection Issues

- **Generic vs Non-Generic**: `IRaise<T>` (generic) ≠ `IRaiseable` (non-generic)
- **Method Chain Returns**: Different interfaces at each chain position
- **Missing Symbol Registration**: String fallback masks missing symbols

## Crash Safety: Symbol Casts and Forward Language Versions (MANDATORY)

An analyzer that throws is worse than a missing diagnostic: Roslyn reports `AD0001` and **disables the analyzer for the rest of the session**. New C# language features are the most common trigger, because the test harness compiles at the language version its pinned reference-assembly group allows, so future constructs execute against code no test ever exercised.

### Never cast a symbol type without a pattern guard

Casts from `ITypeSymbol`/`ISymbol` to a derived interface (`IArrayTypeSymbol`, `INamedTypeSymbol`, `IPointerTypeSymbol`, ...) **MUST** use a pattern test with a graceful early-return, never a direct `(T)` cast or `as` + unchecked dereference.

- ❌ `var element = ((IArrayTypeSymbol)paramsParameter.Type).ElementType;`
- ✅ `if (paramsParameter.Type is not IArrayTypeSymbol arrayType) { return; }` then use `arrayType.ElementType`.

**Concrete failure this rule prevents (#1241):** `ConstructorArgumentsShouldMatchAnalyzer` cast `(IArrayTypeSymbol)paramsParameter.Type`. On a host compiling **C# 13 params collections** (`params List<int>`, `params ReadOnlySpan<int>`), `Type` is an `INamedTypeSymbol`, the cast throws `InvalidCastException`, and the analyzer dies with `AD0001`. `IsParams` does **not** imply array. The same shape recurs for any assumption that a symbol subtype is fixed.

Program defensively: when a code path assumes an invariant about a symbol's shape, guard it (pattern test + early-return, or `Debug.Assert` for truly-impossible states).

### Ship a regression test at the language version that triggers the crash

Any crash-path fix tied to a specific language feature **MUST** include a regression test compiled at the C# version that reproduces it. If the default harness pins an older language version (e.g. the `net8.0` group pins C# 12, so `params List<int>` fails `CS0225`), use the isolated newest-language test project (`Moq.Analyzers.CSharp13.Test`, Roslyn 4.13 — kept separate so shipping analyzers keep their netstandard2.0 + Roslyn 4.8 API ceiling). A fix without a test at the triggering language version does not close the defect.

For the broader structural guardrails (a first-class "latest language version" test leg, an automated unguarded-cast guard, a scheduled newest-SDK CI probe), see issue [#1321](https://github.com/rjmurillo/moq.analyzers/issues/1321).

## Test Data & Sample Inputs/Outputs

### What Constitutes Good Test Data?

- Cover all code paths: positive, negative, and edge cases
- Include both minimal and complex/realistic examples
- Test for invalid inputs, exceptions, and boundary conditions
- Include performance-sensitive scenarios if relevant
- For analyzers/code fixes: test all diagnostic locations, fix applications, and no-fix scenarios

### Coverage Strategy

- For every new analyzer/code fix, include:
  - At least one positive, one negative, and one edge case test
  - Data-driven tests for all fixable patterns
  - Performance test if analyzer is non-trivial
- Document test data rationale in comments or PR description
- Code coverage is automatically generated when `dotnet test --settings ./build/targets/tests/test.runsettings` is run and placed in `./artifacts/TestResults/coverage/Cobertura.xml`
