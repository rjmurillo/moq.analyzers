# Bug Patterns and Anti-Patterns

Extracted from analysis of 34 bugs across 200 issues. These are the recurring failure modes.
Status reflects current codebase as of 2026-03-15.

## Pattern 1: Parenthesized Expression Blindness (7 issues)

Any analyzer walking InvocationExpressionSyntax chains MUST unwrap ParenthesizedExpressionSyntax.
This was a systemic gap across multiple analyzers, not isolated bugs.

- #887: Moq1203 false positive when Setup wrapped in parentheses
- #896: Multiple analyzers broken by parenthesized expressions
- Fix: Always use helpers that call .WalkDownParentheses() or equivalent unwrapping.

## Pattern 2: Per-Operation Allocation of Expensive Objects (8 issues, ALL FIXED)

Creating MoqKnownSymbols or WellKnownTypeProviders per-operation instead of per-compilation
was the single biggest performance footgun. All instances have been corrected.

- #972 (CLOSED): 10+ analyzers created MoqKnownSymbols per-operation. Fixed: all now create in RegisterCompilationStartAction.
- #971 (CLOSED): MoqKnownSymbols properties allocated on every access without caching. Fixed.
- #1022 (CLOSED): WellKnownTypeProviders created per operation in two analyzers. Fixed.
- #984: Recursive iterator allocation in MockRepositoryVerifyAnalyzer.GetAllChildOperations.
- Rule: Expensive objects MUST be created in RegisterCompilationStartAction and passed via closure. This is now the established pattern across all analyzers. Guard against regression in code review.

## Pattern 3: False Positives from Delegate Overloads (5 issues)

GetSymbolInfo on MemberAccessExpressionSyntax misses delegate overloads.
Lambda-parameter guards on composite operations (IBinaryOperation) cause false negatives.

- #1010: Moq1302 false positive with StatusCodes in LINQ to Mocks
- #911: Moq1206 false negative from delegate overload miss
- #910: Moq1203 false positive, delegate-based ReturnsAsync not recognized
- Rule: Test all invocation patterns including delegate overloads and chained calls.

## Pattern 4: Missing Null/Defensive Checks (4 issues)

Analyzers that throw crash the IDE. This is not optional.

- #994: DefaultIfNotSingle silently accepts null source
- #985: Missing constructor/parameter validation across multiple types
- #974: Null-forgiving operator on potentially null lambda extraction
- #977: Code fix providers silently return unchanged document on failure
- Rule: Every public/internal API must validate inputs. No null-forgiving operators on uncertain values.

## Pattern 5: String-Based Detection Fallbacks (1 issue, systemic)

- #981: String-based detection violates the symbol-based mandate (ADR-001)
- Rule: Never use string matching for Moq method/type names. Always resolve via MoqKnownSymbols.

## Pattern 6: CI/Infrastructure Silent Failures (5 issues)

- #976: Release workflow has silent publish failure
- #975: 16+ unpinned GitHub Actions across security-sensitive workflows
- #983: AnalyzerReleases files had swapped names
- Rule: CI failures must be loud. Pin all GitHub Actions to SHA. Verify release artifacts.

## Prevention Checklist

When writing or reviewing an analyzer:

1. Does it unwrap parenthesized expressions?
2. Are expensive objects created per-compilation, not per-operation? (Currently correct across all analyzers. Guard against regression.)
3. Does it handle delegate overloads and chained calls?
4. Are all null paths handled defensively?
5. Does it use symbol-based detection exclusively (no string matching)?
6. Has it been tested with real-world Moq usage patterns (not just happy path)?
