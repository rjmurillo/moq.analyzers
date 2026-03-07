# Roslyn Analyzer Best Practices (vs. moq.analyzers gaps)

## Source
Analysis conducted 2026-03-06. Researched dotnet/roslyn-analyzers via DeepWiki.
Trigger: issue #1010 (Moq1302 false positive on static member in binary expression).
GetCaptures investigation also conducted 2026-03-06. Confirmed inapplicable (internal API, different problem).

## The Three-Phase Pattern (upstream enforces this structurally)

1. **Register** for broad OperationKind (e.g., PropertyReference, MethodReference)
2. **Guard**: Check symbol identity AND `operation.Instance` to confirm receiver is the entity under analysis. Return early if not.
3. **Validate**: Only now emit diagnostics.

moq.analyzers Moq1302 collapsed phases 2+3, walking all descendants without a receiver guard.

## Key Receiver Guard (prevents the entire class of issue #1010)

```csharp
// Before flagging any member access in a lambda body:
if (operation.Instance is not IParameterReferenceOperation paramRef) return;
if (!SymbolEqualityComparer.Default.Equals(paramRef.Parameter, lambdaParameter)) return;
```

Static members have `operation.Instance == null`. A null check alone prevents flagging static constants.

## Lambda Scope Tracking

Upstream uses `IOperationExtensions.GetCaptures(lambda)` to enumerate only what the lambda captures. Only those symbols are tracked.

### Why GetCaptures is not applicable to moq.analyzers

`GetCaptures` is `internal` in `Analyzer.Utilities.Extensions` (not callable). It solves a different problem: it reports closed-over variables from enclosing scopes. moq.analyzers needs to verify a receiver chain terminates in the lambda parameter. These are orthogonal operations.

Our approach: `IsRootedInLambdaParameter` (in `src/Common/IOperationExtensions.cs`) walks the receiver chain via `Instance` properties. Zero-allocation, handles static members (Instance==null), chained access, conversions, parentheses, events. Correct primitive for our use case.

### Other analyzers assessed for lambda-walking risk

| Analyzer | Pattern | Risk |
|----------|---------|------|
| `LinqToMocksExpressionShouldBeValidAnalyzer` | Full tree walk via ChildOperations | Fixed (guard added) |
| `SetupShouldBeUsedOnlyForOverridableMembersAnalyzer` | Single-extraction via TraverseOperation | Low |
| `VerifyShouldBeUsedOnlyForOverridableMembersAnalyzer` | Single-extraction via MoqVerificationHelpers | Low |
| `CallbackSignatureShouldMatchMockedMethodAnalyzer` | Syntax-level only | None |
| All others | No lambda body walking | None |

## AnalysisEntity Pattern (upstream)

`AnalysisEntity` encapsulates symbol + instance location. Upstream uses it to distinguish "same property on same instance" vs "same property on different instance". See `PreferDictionaryTryMethodsOverContainsKeyGuardAnalyzer` for example.

## Test Structure Gap

| Practice | dotnet/roslyn-analyzers | moq.analyzers |
|----------|------------------------|---------------|
| Explicit left/right operand negative tests | Yes (`[Theory]` both sides) | Missing (exposed issue #1010) |
| Static member negative test | Yes | Missing in Moq1302 pre-fix |
| Combinatorial `[Theory]` + `[InlineData]` | Consistent | Minimal |

### Upstream naming convention for negative tests:
- `LeftNotCountComparison_NoDiagnosticAsync`
- `RightNotCountComparison_NoDiagnosticAsync`

## DataFlowOperationVisitor Scope Tracking (upstream)

`DataFlowOperationVisitor.ProcessOutOfScopeLocalsAndFlowCaptures` stops tracking symbols when they leave scope. This prevents stale data from influencing analysis of inner lambdas or nested blocks. moq.analyzers does not have equivalent scope tracking.

## Concrete Upstream Examples

- `PreferDictionaryTryMethodsOverContainsKeyGuardAnalyzer`: checks both property symbol AND instance equality before comparing dictionary accesses across statements.
- `DoNotUseCountWhenAnyCanBeUsed`: uses `[Theory]` with explicit `LeftNotCountComparison_NoDiagnosticAsync` / `RightNotCountComparison_NoDiagnosticAsync` test methods for both binary operand sides.

## Priority Actions for moq.analyzers

**P0 (done in issue #1010 fix):**
- Added `IsRootedInLambdaParameter` guard to `IOperationExtensions.cs` (shared utility)
- Added 13 boundary tests covering static const, static property, enum, reversed operands, chained &&, static method, external instance property, ternary, null-coalescing, chained access
- Threaded `MoqKnownSymbols` to eliminate per-call allocation

**P1 (structural, medium effort):**
- Document three-phase pattern (Register, Guard, Validate) as coding standard
- Add `[Theory]` left/right operand coverage to all binary-expression analyzers
- All new standalone tests must use `[Theory]` with both Moq versions (4.8.2, 4.18.4)

**P2 (audit, medium effort):**
- Audit callback validators, parameter matchers, any lambda-body-descending analyzer for missing receiver checks
- Evaluate `GetLambdaCaptures` utility mirroring upstream `IOperationExtensions.GetCaptures`
- Known false negative: `IConditionalOperation` subtrees skipped entirely (ternary expressions inside Mock.Of)
