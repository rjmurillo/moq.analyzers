# Roslyn Analyzer Best Practices

**Last Updated**: 2026-03-15
**Sessions Analyzed**: 1

## IOperation-Specific Guidance

### Lambda-Parameter Guards

Lambda-parameter guards apply ONLY to leaf operations (IMemberReferenceOperation, IInvocationOperation), NOT composites (IBinaryOperation). Applying guards to composites causes false negatives for chained comparisons.

### IMemberReferenceOperation

Use IMemberReferenceOperation to consolidate property/field/event checks into a single handler. Do not merge tests for distinct IOperation terminal types.

### IParenthesizedOperation

IParenthesizedOperation is VB.NET-only. C# parenthesized expressions are transparent in the IOperation tree. Do NOT check for IParenthesizedOperation in C# analyzers.

### Ternary Expression Handling

Ternary expressions produce IConditionalOperation. Both WhenTrue and WhenFalse branches must be checked.

## Moq Type Resolution (VERIFIED 2026-03-15)

### Verified Against Moq Source (github.com/devlooped/moq)

**Fluent interfaces that EXIST:**

- `Moq.Language.IReturns<TMock, TResult>` (arity 2) - setup fluent chain
- `Moq.Language.IThrows` (arity 0) - throws configuration
- `Moq.Language.Flow.ISetup<TMock>` (arity 1) - setup result
- `Moq.Language.Flow.IReturnsResult<TMock>` (arity 1) - ACTUAL return type of .Returns()
- `Moq.Language.Flow.IThrowsResult` (arity 0) - ACTUAL return type of .Throws()
- `Moq.Language.Flow.ISetupGetter<TMock, TProperty>` (arity 2)
- `Moq.Language.Flow.ISetupSetter<TMock, TProperty>` (arity 2)

**Types that DO NOT EXIST (phantom symbols):**

- `IReturns` (non-generic, arity 0) - DOES NOT EXIST
- `IReturns<T>` (arity 1) - DOES NOT EXIST
- `ICallback` (non-generic) - removed from check set
- `ICallback<T>` (arity 1) - removed from check set

**Protected API types (Moq.Protected namespace):**

- `Moq.Protected.IProtectedMock<T>` - exposes Setup, SetupGet, SetupSet, SetupSequence, Verify, VerifyGet, VerifySet
- `Moq.Protected.ItExpr` - returns Expression<T>, not T. Using Moq.It in string-based protected setups passes default(T), causing silent matcher failures.
- `It.Ref<T>` is a nested class with different containing type symbol than `Moq.It`. Standard containingType checks miss it.

### Key Rule: AllInterfaces Walk

Roslyn's `INamedTypeSymbol.AllInterfaces` provides full transitive closure. Use `ConstructedFrom` to strip type arguments before comparison with open generic symbols.

## Analyzer Testing Requirements

### Positive AND Negative Tests Are Mandatory

Every analyzer change MUST have:

- Positive tests: code that SHOULD trigger the diagnostic
- Negative tests: code that should NOT trigger the diagnostic

The v0.4.1/v0.4.2 releases were caused by missing negative tests. Users reported simple reproduction cases (parenthesized expressions, ReturnsAsync chaining, delegate overloads) that tests did not cover.

### Test Patterns

- Use `{|DiagnosticId:code|}` markers for expected diagnostics
- Test against both Moq 4.8.2 and 4.18.4 via `.WithMoqReferenceAssemblyGroups()`
- Include doppelganger tests (user-defined classes that resemble Moq types should NOT trigger)
- Include literal value tests (non-matcher arguments should NOT trigger matcher-specific diagnostics)
- Use reflection-based completeness tests to guard against gaps (pit of success)

### Coverage Target

100% block coverage for new analyzer code. Defensive null guards that are provably unreachable should be documented, not tested with impossible scenarios.
