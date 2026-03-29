# Critical Mistakes Prevention Guide

Hard-won lessons from the v0.5.0 implementation session (2026-03-15). Every item here caused real bugs or near-misses. Future agents MUST read this before modifying analyzers.

## STOP: Before Writing Any Analyzer Code

1. **Research the target Moq API first.** Your training data is stale. Use DeepWiki (mcp__claude_ai_deepwiki__ask_question with repo "devlooped/moq"), Perplexity, and dotnet-inspect to verify:
   - Exact type names, namespaces, and generic arity
   - All method overloads (Moq often has 16+ overloads per method)
   - Version differences between Moq 4.8 and 4.18+
   - The Moq Quickstart wiki: <https://github.com/devlooped/moq/wiki/Quickstart>

2. **Verify symbols resolve.** After adding types to MoqKnownSymbols, write a test that asserts the symbol is non-null from a Moq compilation. Phantom symbols (types that don't exist) silently produce null comparisons that always return false.

## Specific Moq API Traps

### Returns() has 20 overloads (verified via dotnet-inspect on Moq 4.18.4)

IReturns<TMock, TResult> has 20 Returns overloads + 1 CallBase. All return IReturnsResult<TMock>.

- Returns(TResult value)
- Returns(Func<TResult>)
- Returns<T1>(Func<T1, TResult>) through Returns<T1..T16>(...)
- Returns(Delegate) (catch-all)
- The T1..T16 generics are ARGUMENT types, not return types
- Returns<string>(s => ...) produces GenericNameSyntax in Roslyn, not IdentifierNameSyntax

### IProtectedMock<TMock> has 25 members (verified via dotnet-inspect)

- Setup: 6 overloads, returns ISetup<TMock>
- SetupGet: 1 overload (string), returns ISetupGetter<TMock, TProperty>
- SetupSet: 1 overload (string, object), returns ISetupSetter<TMock, TProperty>
- SetupSequence: 6 overloads, returns ISetupSequentialAction
- Verify: 8 overloads, returns void
- VerifyGet: 1 overload (string, Times)
- VerifySet: 1 overload (string, Times, object)
- As: 1 overload, returns IProtectedAsMock<TMock, TAnalog>

### ItExpr has 5 distinct method names, 6 total members (verified via dotnet-inspect)

- IsAny, Is, IsInRange, IsNull, IsRegex (2 overloads)
- All return Expression, not T (unlike Moq.It which returns TValue)

### It has 7 distinct method names, 17 total members (verified via dotnet-inspect)

- IsAny, Is (3 overloads), IsIn (3), IsInRange, IsNotIn (3), IsNotNull, IsRegex (2)
- All return TValue

### Use dotnet-inspect for verification

Install: `dotnet tool install -g dotnet-inspect`

Inspect: `dotnet-inspect type --package Moq --all` (lists all types including internal)

Members: `dotnet-inspect member "IReturns<TMock, TResult>" --package Moq --all`

This is the definitive source of truth for Moq's API surface.

### Moq fluent interface types (verified 2026-03-15)

EXISTS:

- Moq.Language.IReturns<TMock, TResult> (arity 2)
- Moq.Language.IThrows (arity 0)
- Moq.Language.Flow.ISetup<TMock> (arity 1)
- Moq.Language.Flow.IReturnsResult<TMock> (arity 1) - actual return type of .Returns()
- Moq.Language.Flow.IThrowsResult (arity 0) - actual return type of .Throws()
- Moq.Language.Flow.ISetupGetter<TMock, TProperty> (arity 2)
- Moq.Language.Flow.ISetupSetter<TMock, TProperty> (arity 2)

DO NOT EXIST (phantom - confirmed by dotnet-inspect AND MoqKnownSymbolsTests Assert.Null):

- IReturns (non-generic) - DOES NOT EXIST. MoqKnownSymbols.IReturns resolves to null. Test: MoqKnownSymbolsTests.cs line 116 Assert.Null(symbols.IReturns)
- IReturns<T> (arity 1) - DOES NOT EXIST. MoqKnownSymbols.IReturns1 resolves to null. Test: MoqKnownSymbolsTests.cs line 123 Assert.Null(symbols.IReturns1)
- WARNING: MoqKnownSymbols.cs still defines properties for these phantom types. The properties exist in C# code but resolve to null at runtime. Do not be fooled by their existence in the source. The tests prove they are null.

### Protected API

- ItExpr is in Moq.Protected namespace, returns Expression<T> not T
- It.Ref<T> is a nested class with different containing type than Moq.It
- IProtectedMock<T> has Setup, SetupGet, SetupSet, SetupSequence, Verify, VerifyGet, VerifySet

### Roslyn target-type inference trap

When explicit generic args are present (e.g., Returns<string>(lambda)), Roslyn infers the lambda return type from the target delegate, NOT from the body. GetSymbolInfo returns the inferred type, masking the actual body return type. Must analyze lambda body directly for accurate type checking.

## Testing Requirements (NON-NEGOTIABLE)

Every analyzer change needs:

- **Positive tests**: Code that SHOULD trigger the diagnostic
- **Negative tests**: Code that should NOT trigger (valid usage, edge cases)
- **Doppelganger tests**: User-defined classes resembling Moq types must not trigger
- **Literal value tests**: Non-matcher arguments (strings, ints, bools) must not trigger matcher-specific diagnostics
- **Completeness tests**: Use reflection to ensure no analyzer is missing tests (pit of success)

The v0.4.1 and v0.4.2 releases were caused by missing negative tests for simple user reproduction cases.

## Verification Protocol (TRUST NOTHING)

- Never trust agent claims about "dead code" or "unreachable branches" without reading the actual Moq API
- Always verify MoqKnownSymbols resolve to non-null
- Always run build with PedanticMode AND all tests before claiming done
- Documentation (README, docs/rules/, AnalyzerReleases) is part of done for new analyzers
- Verify `dotnet --version` matches global.json before running commands. Multiple SDK installs (e.g., snap vs manual) can cause version mismatch. Run `dotnet --info` to check.
