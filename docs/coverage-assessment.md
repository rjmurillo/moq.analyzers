# Code Coverage Assessment for Moq.Analyzers Assembly

**Date:** 2025-10-10  
**Task:** rjmurillo/moq.analyzers#640 (Sub-task of #639)  
**Goal:** Assess current code coverage and identify gaps in the Moq.Analyzers assembly

---

## Executive Summary

The Moq.Analyzers assembly currently has:
- **Line Coverage:** 85.32%
- **Branch Coverage:** 71.52%
- **Complexity:** 1502
- **Gap to 100%:** 14.68% line coverage, 28.48% branch coverage

This assessment identifies 34 classes with less than 100% coverage, totaling approximately 528 uncovered lines and significant branch coverage gaps.

---

## Overall Coverage Statistics

| Metric | Value | Gap to 100% |
|--------|-------|-------------|
| Line Coverage | 85.32% | 14.68% |
| Branch Coverage | 71.52% | 28.48% |
| Total Classes Analyzed | 34 | - |
| Classes at 100% Coverage | 0 | 34 |

---

## Priority Coverage Gaps

### Critical Priority (< 60% Line Coverage)

These files have the most significant coverage gaps and should be prioritized first:

#### 1. DiagnosticEditProperties (31.25% line, 0% branch)
- **File:** `src/Common/DiagnosticEditProperties.cs`
- **Uncovered Lines:** 44
- **Impact:** HIGH - Core diagnostic infrastructure used across multiple analyzers
- **Uncovered Regions:**
  - Lines 56-60: Property initialization/usage
  - Lines 63-65: Property handling
  - Lines 68-70: Property handling
  - Lines 73-75: Property handling
  - Lines 78-82: Property handling
  - Lines 84-85: Property handling
- **Recommendation:** Add comprehensive unit tests for all diagnostic edit properties

#### 2. EnumerableExtensions (35.19% line, 28.57% branch)
- **File:** `src/Common/EnumerableExtensions.cs`
- **Uncovered Lines:** 70
- **Impact:** HIGH - Utility methods used throughout the codebase
- **Uncovered Regions:**
  - Lines 7-10: Core enumeration logic
  - Lines 13-14: Extension methods
  - Lines 30-33: Filtering/manipulation
  - Lines 36-38: Collection operations
  - Lines 41-43: Collection operations
  - Lines 46-47: Collection operations
  - Lines 49-53: Complex operations
  - Lines 56-57: Edge cases
  - Line 59: Edge case handling
  - Lines 62-64: Error handling
  - Lines 66-67: Error handling
  - Lines 73-74: Boundary conditions
  - Lines 94, 96: Specific operations
- **Recommendation:** Create data-driven tests for all extension method scenarios

#### 3. MockBehaviorDiagnosticAnalyzerBase (52.83% line, 50% branch)
- **File:** `src/Analyzers/MockBehaviorDiagnosticAnalyzerBase.cs`
- **Uncovered Lines:** 50
- **Impact:** HIGH - Base class for multiple behavior analyzers
- **Uncovered Regions:**
  - Lines 40-43: Initialization logic
  - Lines 46-50: Core analyzer setup
  - Lines 52-54: Registration
  - Line 71: Error handling
  - Lines 73-76: Analysis logic
  - Lines 84-85: Diagnostic reporting
  - Lines 90-91: Diagnostic reporting
  - Lines 102-103: Edge cases
  - Lines 121-122: Cleanup/finalization
- **Recommendation:** Test all inheritance scenarios and analyzer registration paths

### High Priority (60-80% Line Coverage)

#### 4. ArrayExtensions (73.33% line, 50% branch)
- **File:** `src/Common/ArrayExtensions.cs`
- **Uncovered Lines:** 8
- **Impact:** MEDIUM - Utility methods for array operations
- **Uncovered Regions:**
  - Lines 31-32: Array operations (duplicate entries)
  - Lines 36-37: Array operations (duplicate entries)

#### 5. EventSetupHandlerShouldMatchEventTypeAnalyzer (75.70% line, 68.42% branch)
- **File:** `src/Analyzers/EventSetupHandlerShouldMatchEventTypeAnalyzer.cs`
- **Uncovered Lines:** 52
- **Impact:** HIGH - Event handler validation
- **Uncovered Regions:**
  - Lines 46-47, 75-76, 82-83: Event type checking
  - Lines 99-100, 104-105, 109-110, 114-115: Delegate validation
  - Lines 120-121, 134-135, 140: Error scenarios
  - Lines 146-148, 165, 171-173: Edge cases

#### 6. InvocationExpressionSyntaxExtensions (77.27% line, 55.56% branch)
- **File:** `src/Common/InvocationExpressionSyntaxExtensions.cs`
- **Uncovered Lines:** 10
- **Impact:** MEDIUM - Syntax analysis utilities
- **Uncovered Regions:**
  - Lines 38-39, 44-46: Expression parsing

#### 7. SemanticModelExtensions (77.87% line, 59.38% branch)
- **File:** `src/Common/SemanticModelExtensions.cs`
- **Uncovered Lines:** 54
- **Impact:** HIGH - Core semantic analysis utilities
- **Uncovered Regions:**
  - Lines 20-21, 26-27: Model initialization
  - Lines 69, 71, 85, 87, 158: Symbol resolution
  - Lines 184-188, 190-191, 194: Complex analysis
  - Lines 217-218, 224-225, 230-231: Type analysis
  - Lines 237-238, 248-249: Additional scenarios

#### 8. ReturnsAsyncShouldBeUsedForAsyncMethodsAnalyzer (79.12% line, 61.11% branch)
- **File:** `src/Analyzers/ReturnsAsyncShouldBeUsedForAsyncMethodsAnalyzer.cs`
- **Uncovered Lines:** 38
- **Impact:** HIGH - Async/await pattern validation
- **Uncovered Regions:**
  - Lines 49-50, 56-57: Return type checking
  - Lines 84-85, 109: Task analysis
  - Lines 115-116, 130-131, 136-137: Async validation
  - Lines 143-144, 149-150, 155-156: Edge cases

### Medium Priority (80-90% Line Coverage)

#### 9. MoqVerificationHelpers (80.56% line, 59.09% branch)
- **File:** `src/Common/MoqVerificationHelpers.cs`
- **Uncovered Lines:** 14
- **Impact:** HIGH - Shared verification logic
- **Uncovered Regions:**
  - Lines 47-48, 50: Lambda extraction
  - Lines 61-62: Member symbol resolution
  - Lines 80-81: Syntax navigation

#### 10. DiagnosticExtensions (80.95% line, 50% branch)
- **File:** `src/Common/DiagnosticExtensions.cs`
- **Uncovered Lines:** 8
- **Impact:** MEDIUM - Diagnostic utilities

#### 11. IMethodSymbolExtensions (82.35% line, 75% branch)
- **File:** `src/Common/IMethodSymbolExtensions.cs`
- **Uncovered Lines:** 18
- **Impact:** MEDIUM - Method symbol analysis

#### 12. RaisesEventArgumentsShouldMatchEventSignatureAnalyzer (82.81% line, 63.64% branch)
- **File:** `src/Analyzers/RaisesEventArgumentsShouldMatchEventSignatureAnalyzer.cs`
- **Uncovered Lines:** 22
- **Impact:** MEDIUM - Event argument validation

#### 13. IOperationExtensions (83.64% line, 76.19% branch)
- **File:** `src/Common/IOperationExtensions.cs`
- **Uncovered Lines:** 18
- **Impact:** MEDIUM - Operation tree utilities

#### 14. AsShouldBeUsedOnlyForInterfaceAnalyzer (85.96% line, 63.64% branch)
- **File:** `src/Analyzers/AsShouldBeUsedOnlyForInterfaceAnalyzer.cs`
- **Uncovered Lines:** 16
- **Impact:** MEDIUM - Interface validation

### Lower Priority (90-100% Line Coverage)

The following analyzers have relatively good coverage but still have gaps:

- LinqToMocksExpressionShouldBeValidAnalyzer (87.68%)
- MoqKnownSymbols (87.72%)
- MethodSetupShouldSpecifyReturnValueAnalyzer (88.10%)
- SetupShouldNotIncludeAsyncResultAnalyzer (88.24%)
- MockGetShouldNotTakeLiteralsAnalyzer (89.39%)
- MockRepositoryVerifyAnalyzer (90.24%)
- SetupSequenceShouldBeUsedOnlyForOverridableMembersAnalyzer (90.32%)
- NoSealedClassMocksAnalyzer (90.43%)
- EventSyntaxExtensions (91.35%)
- CallbackSignatureShouldMatchMockedMethodAnalyzer (91.87%)
- SetExplicitMockBehaviorAnalyzer (92.45%)
- RedundantTimesSpecificationAnalyzer (92.75%)
- SetupShouldBeUsedOnlyForOverridableMembersAnalyzer (93.33%)
- ISymbolExtensions (93.71%)
- VerifyShouldBeUsedOnlyForOverridableMembersAnalyzer (94.20%)
- ConstructorArgumentsShouldMatchAnalyzer (94.21%)
- RaiseEventArgumentsShouldMatchEventSignatureAnalyzer (95.06%)
- SetStrictMockBehaviorAnalyzer (96.55%)

---

## Coverage Gaps by Category

### By Component Type

| Component Type | Average Coverage | Count | Priority |
|----------------|-----------------|-------|----------|
| Common Utilities | 73.2% | 12 | HIGH |
| Analyzers | 88.4% | 22 | MEDIUM |

### By Impact Level

| Impact | Line Coverage Range | Files | Total Uncovered Lines |
|--------|-------------------|-------|----------------------|
| CRITICAL | < 60% | 3 | 164 |
| HIGH | 60-80% | 5 | 172 |
| MEDIUM | 80-90% | 6 | 100 |
| LOW | 90-100% | 20 | 192 |

---

## Recommended Action Plan

### Phase 1: Critical Gaps (Target: Achieve 70%+ coverage)
**Priority Files:**
1. DiagnosticEditProperties (31.25% → 90%+)
2. EnumerableExtensions (35.19% → 90%+)
3. MockBehaviorDiagnosticAnalyzerBase (52.83% → 90%+)

**Approach:**
- Create comprehensive unit tests for all properties in DiagnosticEditProperties
- Implement data-driven tests for all EnumerableExtensions methods
- Test all inheritance paths and registration scenarios in MockBehaviorDiagnosticAnalyzerBase

**Estimated Impact:** +10% overall line coverage

### Phase 2: High-Impact Utilities (Target: Achieve 85%+ coverage)
**Priority Files:**
1. ArrayExtensions (73.33% → 100%)
2. EventSetupHandlerShouldMatchEventTypeAnalyzer (75.70% → 95%+)
3. InvocationExpressionSyntaxExtensions (77.27% → 95%+)
4. SemanticModelExtensions (77.87% → 90%+)
5. ReturnsAsyncShouldBeUsedForAsyncMethodsAnalyzer (79.12% → 95%+)

**Approach:**
- Test all edge cases and error paths
- Add tests for complex scenarios (nested expressions, multiple type parameters)
- Cover all branch paths in conditional logic

**Estimated Impact:** +8% overall line coverage

### Phase 3: Medium-Priority Components (Target: Achieve 95%+ coverage)
**Priority Files:**
1. MoqVerificationHelpers (80.56% → 100%)
2. DiagnosticExtensions (80.95% → 100%)
3. IMethodSymbolExtensions (82.35% → 100%)
4. All other files 80-90% coverage

**Approach:**
- Add tests for remaining uncovered branches
- Test null handling and error conditions
- Cover edge cases in symbol resolution

**Estimated Impact:** +3% overall line coverage

### Phase 4: Polish (Target: Achieve 100% coverage)
**Priority:** All remaining files

**Approach:**
- Systematically address each remaining gap
- Focus on branch coverage improvements
- Test error handling and exceptional paths

**Estimated Impact:** Final push to 100%

---

## Technical Considerations

### Testing Infrastructure Gaps

1. **Missing Test Patterns:**
   - Comprehensive error path testing
   - Edge case scenarios for utility methods
   - Branch coverage for conditional logic

2. **Test Helper Opportunities:**
   - Create test helpers for DiagnosticEditProperties testing
   - Build data-driven test patterns for extension methods
   - Develop fixtures for complex semantic model scenarios

3. **Coverage Tooling:**
   - Current coverage report is comprehensive
   - HTML reports available in `artifacts/TestResults/coverage/`
   - Cobertura XML at `artifacts/TestResults/coverage/Cobertura.xml`

### Complexity Considerations

Files with high complexity (>100) that need coverage attention:
- EnumerableExtensions: Complex LINQ operations
- SemanticModelExtensions: Deep Roslyn API usage
- EventSetupHandlerShouldMatchEventTypeAnalyzer: Complex event validation logic

---

## Key Findings Summary

1. **Overall Health:** The Moq.Analyzers assembly has reasonable coverage (85.32%) but significant gaps remain

2. **Critical Gaps:** Three files with <60% coverage represent the most significant risk
   - DiagnosticEditProperties (31.25%)
   - EnumerableExtensions (35.19%)
   - MockBehaviorDiagnosticAnalyzerBase (52.83%)

3. **Utility vs Analyzer Coverage:** Common utility files have lower average coverage (73.2%) than analyzers (88.4%), suggesting utilities are under-tested

4. **Branch Coverage Concern:** Overall branch coverage (71.52%) is significantly lower than line coverage, indicating missing edge case and error path testing

5. **High-Impact Opportunities:**
   - Covering the top 3 critical files could add ~10% to overall coverage
   - Addressing the top 8 files could achieve 90%+ overall coverage
   - Common utilities need the most attention

---

## Next Steps

1. ✅ **Completed:** Coverage assessment and gap identification
2. **Next Task:** Create detailed test plans for Phase 1 (Critical Gaps)
3. **Future Tasks:**
   - Implement missing tests (Phase 1-4)
   - Verify coverage improvements
   - Establish coverage gates in CI/CD

---

## References

- **Coverage Report Location:** `artifacts/TestResults/coverage/Cobertura.xml`
- **HTML Reports:** `artifacts/TestResults/coverage/*.html`
- **Parent Issue:** rjmurillo/moq.analyzers#639 (Achieve 100% Code Coverage)
- **This Task:** rjmurillo/moq.analyzers#640 (Coverage Assessment)

---

## Appendix: Complete Coverage Data

### All Classes with <100% Coverage (Sorted by Coverage)

| Class | Line Coverage | Branch Coverage | Uncovered Lines |
|-------|--------------|-----------------|-----------------|
| DiagnosticEditProperties | 31.25% | 0.00% | 44 |
| EnumerableExtensions | 35.19% | 28.57% | 70 |
| MockBehaviorDiagnosticAnalyzerBase | 52.83% | 50.00% | 50 |
| ArrayExtensions | 73.33% | 50.00% | 8 |
| EventSetupHandlerShouldMatchEventTypeAnalyzer | 75.70% | 68.42% | 52 |
| InvocationExpressionSyntaxExtensions | 77.27% | 55.56% | 10 |
| SemanticModelExtensions | 77.87% | 59.38% | 54 |
| ReturnsAsyncShouldBeUsedForAsyncMethodsAnalyzer | 79.12% | 61.11% | 38 |
| MoqVerificationHelpers | 80.56% | 59.09% | 14 |
| DiagnosticExtensions | 80.95% | 50.00% | 8 |
| IMethodSymbolExtensions | 82.35% | 75.00% | 18 |
| RaisesEventArgumentsShouldMatchEventSignatureAnalyzer | 82.81% | 63.64% | 22 |
| IOperationExtensions | 83.64% | 76.19% | 18 |
| AsShouldBeUsedOnlyForInterfaceAnalyzer | 85.96% | 63.64% | 16 |
| LinqToMocksExpressionShouldBeValidAnalyzer | 87.68% | 82.93% | 34 |
| MoqKnownSymbols | 87.72% | 47.44% | 14 |
| MethodSetupShouldSpecifyReturnValueAnalyzer | 88.10% | 73.33% | 20 |
| SetupShouldNotIncludeAsyncResultAnalyzer | 88.24% | 63.89% | 16 |
| MockGetShouldNotTakeLiteralsAnalyzer | 89.39% | 75.00% | 14 |
| MockRepositoryVerifyAnalyzer | 90.24% | 78.57% | 24 |
| SetupSequenceShouldBeUsedOnlyForOverridableMembersAnalyzer | 90.32% | 78.57% | 12 |
| NoSealedClassMocksAnalyzer | 90.43% | 67.24% | 22 |
| EventSyntaxExtensions | 91.35% | 82.00% | 18 |
| CallbackSignatureShouldMatchMockedMethodAnalyzer | 91.87% | 74.24% | 20 |
| SetExplicitMockBehaviorAnalyzer | 92.45% | 84.38% | 8 |
| RedundantTimesSpecificationAnalyzer | 92.75% | 77.27% | 10 |
| SetupShouldBeUsedOnlyForOverridableMembersAnalyzer | 93.33% | 88.46% | 8 |
| ISymbolExtensions | 93.71% | 81.03% | 18 |
| VerifyShouldBeUsedOnlyForOverridableMembersAnalyzer | 94.20% | 85.71% | 8 |
| ConstructorArgumentsShouldMatchAnalyzer | 94.21% | 86.30% | 30 |
| RaiseEventArgumentsShouldMatchEventSignatureAnalyzer | 95.06% | 81.82% | 8 |
| SetStrictMockBehaviorAnalyzer | 96.55% | 86.84% | 4 |

**Total Uncovered Lines Across All Classes:** 528+

