---
title: "ADR-007: Prefer RegisterOperationAction Over RegisterSyntaxNodeAction"
status: "Accepted"
date: "2026-03-15"
authors: "moq.analyzers maintainers"
tags: ["architecture", "decision", "roslyn", "analysis"]
supersedes: ""
superseded_by: ""
---

## Status

Accepted

## Context

Roslyn provides two primary registration points for analyzers:

- `RegisterSyntaxNodeAction`: fires on specific `SyntaxKind` values. The callback receives a syntax node. Semantic information requires additional `SemanticModel` calls.
- `RegisterOperationAction`: fires on specific `OperationKind` values. The callback receives an `IOperation` node with resolved symbols, types, and constant values already attached.

The `IOperation` tree is the semantic representation of the code. It abstracts over syntax variations: a method call written with or without parentheses, with named arguments or positional, in expression or statement context, all produce the same `IInvocationOperation`. Syntax analysis must handle each variation manually or risk missing cases.

Moq usage patterns involve object creation (`new Mock<T>()`), method invocations (`.Setup()`, `.Verify()`), and property accesses. All of these have direct `IOperation` equivalents.

## Decision

Analyzers use `context.RegisterOperationAction()` with `OperationKind` values as the primary registration mechanism. `RegisterSyntaxNodeAction` is used only when the check genuinely requires raw syntax structure that is not represented in the `IOperation` tree.

## Consequences

### Positive

- **POS-001**: Resolved symbols and types are available directly on `IOperation` nodes. No additional `SemanticModel` lookups are needed for the common case.
- **POS-002**: Syntax variations (different whitespace, parenthesization, comment placement, conditional expressions) are transparent. The same operation handler covers all surface forms.
- **POS-003**: Analyzers are shorter and have fewer branches. Reduced complexity lowers the risk of missed cases.
- **POS-004**: `IOperation` handles both C# and VB.NET in theory, providing a path to multi-language support without rewriting analyzers.

### Negative

- **NEG-001**: Some edge cases require examining raw syntax (e.g., checking for specific token patterns or trivia). These cases still require `RegisterSyntaxNodeAction` or mixed analysis.
- **NEG-002**: `IOperation` is a more abstract model. Contributors less familiar with Roslyn's operation tree need time to learn the relevant `OperationKind` values and `IOperation` subtypes.
- **NEG-003**: Not all syntax constructs have `IOperation` equivalents. Attribute analysis and some declaration patterns require syntax-level analysis.

## Alternatives Considered

### Syntax-Only Analysis

- **ALT-001**: **Description**: Use `RegisterSyntaxNodeAction` exclusively with `SemanticModel` calls for all semantic checks.
- **ALT-002**: **Rejection Reason**: Requires manual semantic lookups for every check. Each syntax variation (different calling convention, argument order, etc.) must be handled explicitly. Prone to missed cases and higher cyclomatic complexity.

### Mixed Syntax and Semantic Without IOperation

- **ALT-003**: **Description**: Use `RegisterSyntaxNodeAction` with inline `SemanticModel.GetSymbolInfo()` and `SemanticModel.GetTypeInfo()` calls.
- **ALT-004**: **Rejection Reason**: Verbose and error-prone. Results in duplicated semantic lookup patterns across analyzers. Produces the same information as `IOperation` at higher cost and with more code.

## Implementation Notes

- **IMP-001**: Use `OperationKind.Invocation` for method call analysis, `OperationKind.ObjectCreation` for constructor analysis, and `OperationKind.SimpleAssignment` / `OperationKind.PropertyReference` for property patterns. **Base-class delegation**: Analyzers that inherit from a base class (e.g., `MockBehaviorDiagnosticAnalyzerBase`) satisfy this decision when the base class registers the operation action. The derived analyzer does not need its own registration.
- **IMP-002**: When `RegisterSyntaxNodeAction` is required, document in the analyzer class why `IOperation` was insufficient for that specific check.
- **IMP-003**: New analyzers must default to `RegisterOperationAction`. Use of `RegisterSyntaxNodeAction` requires justification in the PR description.
- **IMP-004**: Analyzers using `RegisterSyntaxNodeAction` (as of 2026-03-15). Each must document via comment why `IOperation` was insufficient per IMP-002.

  | Analyzer | Reason |
  |---|---|
  | `ConstructorArgumentsShouldMatchAnalyzer` | Uses ObjectCreationExpression + InvocationExpression; `IOperation` cannot distinguish implicit vs explicit object creation |
  | `EventSetupHandlerShouldMatchEventTypeAnalyzer` | Inspects `+=`/`-=` operator tokens (`SyntaxKind.PlusEqualsToken`) on event handler arguments; these assignment operator tokens are not exposed through `IOperation` for invocation arguments |
  | `RaiseEventArgumentsShouldMatchEventSignatureAnalyzer` | Walks `InvocationExpressionSyntax` to resolve chained Raise/event argument structure; relies on syntax shape of method call chains not represented in `IOperation` |
  | `RaisesEventArgumentsShouldMatchEventSignatureAnalyzer` | Same as `RaiseEventArgumentsShouldMatchEventSignatureAnalyzer` for the `Raises` variant |
  | `ReturnsAsyncShouldBeUsedForAsyncMethodsAnalyzer` | Detects `async` keyword on lambda syntax (`lambda.AsyncKeyword`); `IAnonymousFunctionOperation` does not expose the async modifier as a queryable property |
  | `ReturnsDelegateShouldReturnTaskAnalyzer` | Checks absence of `AsyncKeyword` on anonymous function syntax to identify sync delegates; same `IAnonymousFunctionOperation` limitation as above |
  | `SetupShouldNotIncludeAsyncResultAnalyzer` | Matches chained `.Setup(x => x.Method().Result)` patterns via syntax tree shape; the `.Result` access inside a lambda argument requires syntax-level walking not available through `IOperation` |

## References

- **REF-001**: Roslyn IOperation documentation: <https://github.com/dotnet/roslyn/blob/main/docs/compilers/CSharp/IOperation.md>
- **REF-002**: ADR-001 -- Symbol-Based Detection Over String Matching
- **REF-003**: `src/Analyzers/` -- existing analyzer implementations demonstrating the pattern
