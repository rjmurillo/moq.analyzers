# Code Coverage Gaps - Quick Reference

**Task:** rjmurillo/moq.analyzers#640  
**Status:** Assessment Complete  
**Next:** Implement tests for identified gaps

---

## Top Priority Files (Immediate Action Required)

### 🔴 Critical - Below 60% Coverage

1. **DiagnosticEditProperties** - 31.25% coverage (44 uncovered lines)
   - File: `src/Common/DiagnosticEditProperties.cs`
   - Tests needed: Property initialization, usage patterns, all edit properties
   - Impact: HIGH - Core infrastructure used across all analyzers

2. **EnumerableExtensions** - 35.19% coverage (70 uncovered lines)
   - File: `src/Common/EnumerableExtensions.cs`
   - Tests needed: All extension methods, edge cases, null handling
   - Impact: HIGH - Widely used utility methods

3. **MockBehaviorDiagnosticAnalyzerBase** - 52.83% coverage (50 uncovered lines)
   - File: `src/Analyzers/MockBehaviorDiagnosticAnalyzerBase.cs`
   - Tests needed: All inheritance paths, registration scenarios
   - Impact: HIGH - Base class for multiple analyzers

### 🟡 High Priority - 60-80% Coverage

4. **ArrayExtensions** - 73.33% coverage (8 uncovered lines)
   - Quick win - small number of uncovered lines

5. **EventSetupHandlerShouldMatchEventTypeAnalyzer** - 75.70% coverage (52 uncovered lines)
   - Event handler validation edge cases

6. **InvocationExpressionSyntaxExtensions** - 77.27% coverage (10 uncovered lines)
   - Quick win - small number of uncovered lines

7. **SemanticModelExtensions** - 77.87% coverage (54 uncovered lines)
   - Complex semantic analysis scenarios

8. **ReturnsAsyncShouldBeUsedForAsyncMethodsAnalyzer** - 79.12% coverage (38 uncovered lines)
   - Async/await validation edge cases

---

## Quick Statistics

| Metric | Current | Target | Gap |
|--------|---------|--------|-----|
| Line Coverage | 85.32% | 100% | 14.68% |
| Branch Coverage | 71.52% | 100% | 28.48% |
| Files < 100% | 34 | 0 | 34 |
| Total Uncovered Lines | ~528 | 0 | 528 |

---

## Fastest Path to 95% Coverage

Focus on these files in order - covering them gets you 90%+ overall coverage:

1. **DiagnosticEditProperties** (+2.5% estimated)
2. **EnumerableExtensions** (+4.0% estimated)
3. **MockBehaviorDiagnosticAnalyzerBase** (+2.8% estimated)
4. **EventSetupHandlerShouldMatchEventTypeAnalyzer** (+3.0% estimated)
5. **SemanticModelExtensions** (+3.1% estimated)
6. **ReturnsAsyncShouldBeUsedForAsyncMethodsAnalyzer** (+2.2% estimated)

**Combined Impact:** +17.6% coverage (reaches ~93% total)

---

## Test Creation Guidelines

### For Common Utilities (EnumerableExtensions, ArrayExtensions, etc.)
```csharp
[Fact]
public void MethodName_Scenario_ExpectedBehavior()
{
    // Arrange
    var input = ...;
    
    // Act
    var result = input.ExtensionMethod();
    
    // Assert
    Assert.NotNull(result);
}
```

### For Analyzers
```csharp
[Theory]
[InlineData("test case 1")]
[InlineData("test case 2")]
public async Task AnalyzerName_Scenario_ShouldReportDiagnostic(string code)
{
    await VerifyAnalyzerAsync<AnalyzerType>(code, referenceAssemblyGroup);
}
```

### For Extension Methods with Edge Cases
- Test with null inputs
- Test with empty collections
- Test with single element
- Test with multiple elements
- Test boundary conditions

---

## Files Needing Branch Coverage Attention

These files have significant branch coverage gaps:

| File | Branch Coverage | Priority |
|------|----------------|----------|
| DiagnosticEditProperties | 0% | CRITICAL |
| EnumerableExtensions | 28.57% | HIGH |
| ArrayExtensions | 50% | HIGH |
| MockBehaviorDiagnosticAnalyzerBase | 50% | HIGH |
| DiagnosticExtensions | 50% | MEDIUM |

**Action:** Add tests for:
- Conditional logic branches (if/else)
- Switch statements
- Null coalescing operators
- Try-catch blocks
- Early returns

---

## Key Uncovered Line Ranges (Top 3 Files)

### DiagnosticEditProperties
- Lines 56-60, 63-65, 68-70, 73-75, 78-82, 84-85
- **Pattern:** Property usage throughout the class
- **Test approach:** Create one test per property

### EnumerableExtensions
- Lines 7-10, 13-14, 30-33, 36-38, 41-43, 46-47, 49-53, 56-57, 59, 62-64, 66-67, 73-74, 94, 96
- **Pattern:** Multiple extension methods
- **Test approach:** Data-driven tests for each method

### MockBehaviorDiagnosticAnalyzerBase
- Lines 40-43, 46-50, 52-54, 71, 73-76, 84-85, 90-91, 102-103, 121-122
- **Pattern:** Analyzer lifecycle methods
- **Test approach:** Test derived analyzers to exercise base class

---

## Testing Checklist Template

For each file targeted for coverage improvement:

- [ ] Identify all public methods/properties
- [ ] Create test cases for happy path
- [ ] Create test cases for error paths
- [ ] Create test cases for edge cases (null, empty, boundary)
- [ ] Create test cases for all conditional branches
- [ ] Run coverage report to verify improvement
- [ ] Document any uncovered lines that are intentionally untestable

---

## Resources

- **Full Assessment:** `docs/coverage-assessment.md`
- **Coverage Report:** `artifacts/TestResults/coverage/Cobertura.xml`
- **HTML Reports:** `artifacts/TestResults/coverage/*.html`
- **Run Tests:** `dotnet test --settings ./build/targets/tests/test.runsettings`
- **View Coverage:** Open `artifacts/TestResults/coverage/index.html` in browser

---

## Estimated Effort

| Priority | Files | Estimated Tests | Estimated Hours |
|----------|-------|----------------|-----------------|
| Critical | 3 | 40-60 | 12-16 |
| High | 5 | 30-50 | 8-12 |
| Medium | 6 | 20-30 | 6-8 |
| Low | 20 | 40-60 | 10-15 |
| **Total** | **34** | **130-200** | **36-51** |

---

## Next Tasks (Based on Parent Issue #639)

1. ✅ Task 1: Assess coverage and identify gaps (THIS TASK - COMPLETE)
2. ⏭️ Task 2: Improve coverage for DiagnosticEditProperties
3. Task 3: Improve coverage for EnumerableExtensions
4. Task 4: Improve coverage for MockBehaviorDiagnosticAnalyzerBase
5. Task 5: Improve coverage for remaining high-priority files
6. Task 6: Improve coverage for medium-priority files
7. Task 7: Final push to 100% coverage
8. Task 8: Establish coverage gates in CI/CD

---

## Success Criteria

**This task is complete when:**
- ✅ Coverage report has been analyzed
- ✅ All files with < 100% coverage have been identified
- ✅ Uncovered lines have been documented
- ✅ Priority ranking has been established
- ✅ Next steps have been documented

**Overall goal (from #639):**
- Achieve 100% line coverage for Moq.Analyzers assembly
- Achieve 100% branch coverage for Moq.Analyzers assembly
- Establish automated coverage reporting
- Set up CI/CD gates to prevent coverage regression

