# Diff Code Reuse Review - Summary

## Analysis Date

Based on diff review of changes to moq.analyzers project.

## Key Findings

### 1. **IOperationExtensions.cs - foreach Loop Pattern (NO REUSE ISSUE)**

**Code in question:**

- `GetReferencedMemberSymbolFromLambda()` - iterates `blockOperation.Operations` with foreach
- `GetReferencedMemberSyntaxFromLambda()` - duplicates same foreach pattern

**Finding:** NO ISSUE - This is NOT duplication. Both methods use a **general helper pattern** that was deliberately designed to avoid duplication:

- The `TraverseOperation<T>()` private method (line 102) is the shared abstraction
- It uses a `selector` function parameter to extract either `SyntaxNode` or `ISymbol`
- The `GetReferencedMemberSymbolFromLambda()` calls `GetSymbolFromOperation()`
- The `GetReferencedMemberSyntaxFromLambda()` calls `GetSyntaxFromOperation()`
- Both these helper methods use `TraverseOperation<T>()` internally with different selectors

The foreach loops in both methods are unavoidable because they need to iterate through `blockOperation.Operations` to find the first non-null result. Each loop has **different extraction logic** (symbol vs syntax), making a shared implementation impractical.

This is good design: Extract what is common (traversal logic in `TraverseOperation<T>`) while keeping operation-specific code separate.

---

### 2. **MockDetectionHelpers.cs - String Check Replaced with Symbol Check (NO REUSE ISSUE)**

**Code in question:**

- Diff mentions: String-based "Of" check replaced with `targetMethod.IsInstanceOf(knownSymbols.MockOf)`

**Finding:** NO ISSUE - This is actually an **improvement**, not a reuse problem:

- The change upgrades from string matching to symbol-based detection (correct approach per project standards)
- `IsValidMockOfMethod()` already exists and encapsulates this check (line 93-101)
- The method properly uses `IsInstanceOf()` extension which is symbol-aware
- The pattern is used consistently across the codebase

---

### 3. **NoMockOfLoggerAnalyzer.cs - TryGetNullLoggerAlternative (NO REUSE ISSUE)**

**Code in question:**

- `TryGetNullLoggerAlternative()` method with `[NotNullWhen(true)]` out parameter pattern

**Finding:** NO ISSUE - This is NOT a duplicate pattern. Analysis:

- The method is **specific to logger type detection** (lines 115-139)
- It handles the specific logic: check if mocked type is ILogger or ILogger<T>, then output appropriate suggestion
- **Broader Try patterns exist in codebase:**
  - `TryGetMockedTypeFromGeneric()` in MockDetectionHelpers.cs (line 109) - generic type extraction
  - `TryGetOverloadWithParameterOfType()` in IMethodSymbolExtensions.cs (line 46) - method overload matching
  - `TryGetEventPropertyFromLambdaSelector()` in SemanticModelExtensions.cs (line 152) - event property extraction
  - `TryGetFromImmutableDictionary()` in DiagnosticEditProperties.cs (line 55) - dictionary parsing

The `TryGetNullLoggerAlternative()` method is too specialized to extract into a reusable utility. It combines symbol comparison with logger-specific output formatting. Creating a generic utility would be over-engineering (YAGNI violation).

---

### 4. **Invoke-PreCommit.ps1 - Python Command Resolution (NO REUSE ISSUE)**

**Code in question:**

```powershell
$pythonCmd = if (Get-Command "python3" -ErrorAction SilentlyContinue) { "python3" }
             elseif (Get-Command "python" -ErrorAction SilentlyContinue) { "python" }
             else { $null }
```

**Finding:** NO REUSE ISSUE - This is **not duplicated elsewhere**:

- The `Test-ToolAvailable()` function in LintHelpers.ps1 (line 6) handles **availability checking** only
- It does NOT handle fallback command resolution (python3 vs python)
- This pattern is **specific to Python** which has platform-dependent binary names
- Other tools (markdownlint-cli2, yamllint, shellcheck, actionlint) have fixed names
- Attempt to genericize this in `Test-ToolAvailable()` would weaken the abstraction (the function doesn't know about command fallbacks)

This is appropriate specialization for a language-specific need.

---

### 5. **Invoke-PrePush.ps1 - Output Capture (NO REUSE ISSUE)**

**Code in question:**

```powershell
$output = dotnet build (Join-Path $repoRoot "Moq.Analyzers.sln") /p:PedanticMode=true --verbosity quiet 2>&1
```

**Finding:** NO REUSE ISSUE:

- This pattern `$output = command 2>&1` is standard PowerShell idiom for capturing stderr+stdout
- It's not a custom utility that should be extracted
- It's used consistently throughout LintHelpers.ps1 (lines 24, 34, 47, 64, etc.)
- Creating an extraction helper would add unnecessary indirection

---

### 6. **NoMethodsInPropertySetupAnalyzer.cs - Syntax-Tree → IOperation Migration (NO REUSE ISSUE)**

**Code in question:**

- Migration from syntax-tree `FindMockedMethodInvocationFromSetupMethod` to IOperation helpers

**Finding:** NO REUSE ISSUE - This is actually **code improvement**:

- Now uses `lambdaOperation.Body.GetReferencedMemberSymbolFromLambda()` (line 93)
- Now uses `lambdaOperation.Body.GetReferencedMemberSyntaxFromLambda()` (line 100)
- Both methods already exist in IOperationExtensions.cs
- This consolidates symbol/syntax extraction logic (DRY principle)
- The old syntax-tree approach was likely duplicate logic that is now centralized in the extension methods

---

## Conclusion

**NO ISSUES FOUND.** The diff demonstrates good design discipline:

1. **Shared abstractions** are properly extracted (TraverseOperation<T>, extension methods)
2. **Specialized logic** is kept separate where generalization would violate YAGNI
3. **Symbol-based detection** replaces string matching (correctness improvement)
4. **IOperation-based approach** consolidates logic vs. old syntax-tree duplication
5. **PowerShell patterns** follow standard idioms and don't warrant extraction

The code follows SOLID and DRY principles appropriately, with good judgment about where generalization helps vs. where it hurts.
