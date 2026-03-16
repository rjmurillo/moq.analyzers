# Moq API Surface Reference

Verified via `dotnet-inspect` against Moq 4.18.4 on 2026-03-15. This is the definitive reference for analyzer development. DO NOT rely on training data.

## Verification Tool

```bash
dotnet tool install -g dotnet-inspect
dotnet-inspect type --package Moq --all          # All types including internal
dotnet-inspect member "TypeName" --package Moq --all  # All members
```

## Complete Type Catalog (73 types)

### Public Classes (21)
- `Moq.It` (17 members): IsAny, Is (3), IsIn (3), IsInRange, IsNotIn (3), IsNotNull, IsRegex (2)
- `Moq.Mock` (60 members): Static helpers (Get, Of, Verify)
- `Moq.Mock<T>` (98 members): Core mock class
- `Moq.MockRepository` (12 members): Mock factory with shared verification
- `Moq.MockSequence` (6 members): Ordered verification
- `Moq.Protected.ItExpr` (6 members): IsAny, Is, IsInRange, IsNull, IsRegex (2). Returns Expression, NOT TValue.
- `Moq.Protected.ProtectedExtension` (1 member): .Protected() extension method
- `Moq.Times` (21 members): Verification count specification
- `Moq.CaptureMatch<T>`, `Moq.Capture`, `Moq.Match`, `Moq.Match<T>`, etc.

### Extension Method Classes (critical for analyzer detection)
- `Moq.ReturnsExtensions` (27 members): ReturnsAsync and related
- `Moq.GeneratedReturnsExtensions` (30 members): Generated Returns overloads
- `Moq.SequenceExtensions` (12 members): SetupSequence result extensions
- `Moq.MockExtensions` (2 members)
- `Moq.MockLegacyExtensions` (3 members)
- `Moq.ObsoleteMockExtensions` (5 members)

## Fluent Interface Hierarchy

### Setup Chain

Mock<T>.Setup(expr) returns ISetup<TMock> or ISetup<TMock, TResult>

IReturnsThrows<TMock, TResult> (0 own members, composes IReturns + IThrows)
- IReturns<TMock, TResult> (21 members: Returns x20, CallBase x1)
- IThrows (20 members: Throws overloads)

ICallback<TMock, TResult> (19 Callback overloads) returns IReturnsThrows<TMock, TResult>
ICallback (19 Callback overloads) returns ICallbackResult

### Return Types (what .Returns()/.Throws() etc. return)
- IReturnsResult<TMock> (0 own members, marker for completed setup)
- IThrowsResult (0 own members, marker for completed throws)
- ICallbackResult (0 own members, marker for completed callback)
- ICallBaseResult (0 own members, marker for completed callbase)

### Getter/Setter Setup Chain
- Mock<T>.SetupGet(expr) returns ISetupGetter<TMock, TProperty>
- Mock<T>.SetupSet(expr) returns ISetupSetter<TMock, TProperty>
- IReturnsGetter<TMock, TProperty> (3 members: Returns x2, CallBase x1)
- ICallbackGetter<TMock, TProperty> (1 member: Callback)
- ICallbackSetter<TProperty> (1 member: Callback)

### Sequential Setup Chain
- Mock<T>.SetupSequence(expr) returns ISetupSequentialResult<TResult> or ISetupSequentialAction
- ISetupSequentialResult<TResult> (6 members: Returns x2, Throws x3, CallBase)
- ISetupSequentialAction (4 members: Pass, Throws x3)

### Conditional Setup
- mock.When(condition) returns ISetupConditionResult<T> (5 members: Setup x2, SetupGet, SetupSet x2)

### Raises and Verification
- IRaise<T> (19 members: Raises overloads)
- IVerifies (2 members: Verifiable overloads)
- IOccurrence (2 members: AtMostOnce, AtMost)

## IReturns<TMock, TResult> - 20 Returns Overloads

All return IReturnsResult<TMock>:
1. Returns(TResult value) - direct value
2. Returns(Func<TResult>) - zero-arg factory
3. Returns<T1>(Func<T1, TResult>) - 1-arg factory
4. Returns<T1, T2>(Func<T1, T2, TResult>) - 2-arg factory
5-18. Returns<T1..T16>(Func<T1..T16, TResult>) - up to 16-arg factory
19. Returns(Delegate) - catch-all delegate
20. CallBase() - call base implementation

IMPORTANT: T1..T16 are ARGUMENT types, not return types. Returns<string>(s => ...) produces GenericNameSyntax in Roslyn.

## IProtectedMock<TMock> - 25 Members

- Setup(string, params object[]) x3 + Setup<TResult>(string, params object[]) x3 = 6 overloads
- SetupGet<TProperty>(string) x1
- SetupSet<TProperty>(string, object) x1
- SetupSequence(string, params object[]) x3 + SetupSequence<TResult>(string, params object[]) x3 = 6 overloads
- Verify(string, Times, params object[]) x4 + Verify<TResult>(string, Times, params object[]) x4 = 8 overloads
- VerifyGet<TProperty>(string, Times) x1
- VerifySet<TProperty>(string, Times, object) x1
- As<TAnalog>() x1 returns IProtectedAsMock<TMock, TAnalog>

### IProtectedAsMock<TMock, TAnalog> - 12 Members
Lambda-based protected setup/verify. Uses regular It matchers (NOT ItExpr).

## Mock<T> Key Method Return Types

| Method | Return Type |
|--------|-------------|
| Setup(expr) | ISetup<T> (void) or ISetup<T, TResult> (non-void) |
| SetupGet(expr) | ISetupGetter<T, TProperty> |
| SetupSet(expr) | ISetupSetter<T, TProperty> |
| SetupSequence(expr) | ISetupSequentialResult<TResult> or ISetupSequentialAction |
| SetupProperty(expr) | Mock<T> |
| SetupAdd(expr) | ISetup<T> |
| SetupRemove(expr) | ISetup<T> |
| Verify(expr) | void |
| VerifyGet(expr) | void |
| VerifySet(expr) | void |
| VerifyAdd(expr) | void |
| VerifyRemove(expr) | void |
| Raise(expr) | void |

## Moq.It vs Moq.Protected.ItExpr

| Matcher | It (returns TValue) | ItExpr (returns Expression) |
|---------|--------------------|-----------------------------|
| IsAny | IsAny<TValue>() | IsAny<TValue>() |
| Is | Is<TValue>(predicate) x3 | Is<TValue>(predicate) |
| IsIn | IsIn<TValue>(values) x3 | - |
| IsNotIn | IsNotIn<TValue>(values) x3 | - |
| IsInRange | IsInRange<TValue>(from, to, Range) | IsInRange<TValue>(from, to, Range) |
| IsNotNull | IsNotNull<TValue>() | IsNull<TValue>() |
| IsRegex | IsRegex(pattern) x2 | IsRegex(pattern) x2 |
| Ref | It.Ref<T>.IsAny (nested class) | ItExpr.Ref<T>.IsAny |

IMPORTANT: It.Ref<T> is a nested class. ContainingType is NOT Moq.It. Symbol detection must handle this.

## Version Differences: Moq 4.8.2 vs 4.18.4

Tests run against BOTH versions. Analyzers must handle both.

### Member Count Differences

| Type | 4.8.2 | 4.18.4 | Delta |
|------|-------|--------|-------|
| Moq.It | 10 methods | 17 methods | +7 (Is x2, IsIn x1, IsNotIn x1 added) |
| IReturns<TMock, TResult> | 19 Returns + CallBase | 20 Returns + CallBase | +1 (Delegate overload added) |
| IThrows | 2 Throws | 20 Throws | +18 (generic overloads T1..T16 added) |
| ICallback | 18 Callback | 19 Callback | +1 |
| ICallback<TMock, TResult> | 18 Callback | 19 Callback | +1 |
| IProtectedMock<TMock> | 14 members | 25 members | +11 (Setup x3, SetupSequence x2, Verify x6) |
| IProtectedAsMock<T, TAnalog> | 9 members | 12 members | +3 |
| ISetupSequentialResult<TResult> | 4 members | 6 members | +2 (Returns, Throws overloads) |
| ISetupSequentialAction | 3 members | 4 members | +1 (Pass added) |

### Key API Additions in 4.18.4 (not in 4.8.2)
- Returns(Delegate) catch-all overload (the one that causes Moq1208 issues)
- IThrows generic overloads Throws<T1>..Throws<T16>
- Protected Setup<TResult> generic overloads (3 added)
- Protected Verify<TResult> generic overloads (4 added)
- ISetupSequentialAction.Pass() method
- It.Is<TValue> additional overloads

### Stable Across Both Versions
- All fluent interface types exist in both versions (same namespaces, same arity)
- ItExpr: identical 5 methods in both versions
- IReturnsResult<TMock>, IThrowsResult, ICallbackResult: exist in both
- ISetupGetter, ISetupSetter: exist in both (arity 2)
- Mock<T>.Setup/Verify core methods: same signatures

### Test Version Configuration
Defined in tests/Moq.Analyzers.Test/Helpers/ReferenceAssemblyCatalog.cs:
- Net80: .NET 8.0 without Moq (tests early exit via IsMockReferenced)
- Net80WithOldMoq: Moq 4.8.2 (pre-4.13.1, before As<T>() internal change)
- Net80WithNewMoq: Moq 4.18.4 (most downloaded version)
- Net80WithNewMoqAndLogging: Moq 4.18.4 + Microsoft.Extensions.Logging.Abstractions 8.0.0
