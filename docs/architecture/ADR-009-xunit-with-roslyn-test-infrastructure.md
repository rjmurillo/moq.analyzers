---
title: "ADR-009: xUnit with Roslyn Test Infrastructure for Analyzer Testing"
status: "Accepted"
date: "2026-03-15"
authors: "moq.analyzers maintainers"
tags: ["architecture", "decision", "testing", "xunit", "roslyn"]
supersedes: ""
superseded_by: ""
---

## Status

Accepted

## Context

Analyzer tests must verify two properties: diagnostics fire when expected (true positive), and diagnostics do not fire when they should not (no false positive). Each test must cover a specific C# code pattern and assert the exact set of diagnostics produced, including location and message.

Roslyn provides specialized test helpers in `Microsoft.CodeAnalysis.Testing`. These helpers compile inline C# source strings, run analyzers against the resulting compilation, and compare actual diagnostics to expected diagnostics in a single assertion. The expected diagnostics are marked inline in the source string using `{|DiagnosticId:Code|}` syntax, making the expected output visible at the point of the test.

xUnit is the team's preferred test framework for this project. MSTest and NUnit were not adopted at project inception and there is no motivation to introduce multiple test frameworks.

## Decision

All analyzer and code fix tests use xUnit with `CSharpAnalyzerVerifier<TAnalyzer>` and `CSharpCodeFixVerifier<TAnalyzer, TFix>` from Roslyn's test helpers. Test input is inline C# source strings with diagnostic markers. Test helpers and shared setup are in `tests/Moq.Analyzers.Test/Helpers/`.

## Consequences

### Positive

- **POS-001**: Tests are self-documenting. The expected diagnostic location is visible in the source string without consulting a separate fixture or mapping file.
- **POS-002**: Roslyn test helpers handle compilation, semantic analysis, and diagnostic comparison. Tests contain only the assertion-relevant code.
- **POS-003**: Both positive (diagnostic fires) and negative (no diagnostic fires) cases use the same infrastructure and source string format.
- **POS-004**: Code fix tests verify both the diagnostic trigger and the resulting transformed source in a single assertion.

### Negative

- **NEG-001**: Inline source strings in test methods can be long. Tests for complex patterns (generics, nested lambdas) require verbose string literals.
- **NEG-002**: Roslyn test helper versions must stay in sync with the `Microsoft.CodeAnalysis` version used by analyzers. Version mismatches produce confusing test failures.
- **NEG-003**: The diagnostic marker syntax (`{|DiagnosticId:Code|}`) is unfamiliar to contributors who have not worked with Roslyn test infrastructure before.

## Alternatives Considered

### MSTest

- **ALT-001**: **Description**: Use MSTest as the test framework.
- **ALT-002**: **Rejection Reason**: Team preference is xUnit. No technical advantage justifies maintaining two test frameworks. MSTest was not used at project inception.

### External Test Fixture Files

- **ALT-003**: **Description**: Store test C# source in `.cs` files in a test fixtures directory rather than inline strings.
- **ALT-004**: **Rejection Reason**: Separates the expected diagnostic markers from the source that triggers them. Harder to read and maintain. Requires a file loading convention that adds indirection without benefit.

### Manual Compilation and Assertion

- **ALT-005**: **Description**: Create `CSharpCompilation` objects manually, invoke analyzers, and assert on the resulting `Diagnostic[]`.
- **ALT-006**: **Rejection Reason**: Duplicates what Roslyn's test infrastructure already provides. Manual compilation setup is error-prone and verbose. The diagnostic comparison logic is non-trivial to implement correctly for all edge cases.

## Implementation Notes

- **IMP-001**: Each analyzer must have at least one positive test (diagnostic fires) and one negative test (no diagnostic fires) per documented scenario.
- **IMP-002**: Edge case coverage is required: generic type arguments, inheritance hierarchies, async methods, nullable reference types, `params` arrays, and explicit interface implementations where applicable.
- **IMP-003**: Shared verifier configuration (e.g., Moq package reference, language version) is encapsulated in helper classes in `tests/Moq.Analyzers.Test/Helpers/` to avoid duplication.
- **IMP-004**: Code fix tests use `CSharpCodeFixVerifier` and specify both the before-fix and after-fix source strings.

## References

- **REF-001**: `tests/Moq.Analyzers.Test/` -- test project
- **REF-002**: `tests/Moq.Analyzers.Test/Helpers/` -- shared test infrastructure
- **REF-003**: Microsoft.CodeAnalysis.Testing documentation: <https://github.com/dotnet/roslyn-sdk/tree/main/src/Microsoft.CodeAnalysis.Testing>
- **REF-004**: ADR-001 -- Symbol-Based Detection Over String Matching
