# Roslyn Analyzer Best Practices

**Last Updated**: 2026-03-06
**Sessions Analyzed**: 1

## Constraints (HIGH confidence)

- `IsRootedInLambdaParameter` guard must only apply to leaf member operations (`IMemberReferenceOperation` or `IInvocationOperation`), not composite operations like `IBinaryOperation`. Blocking composites causes false negatives on chained comparisons (`&&`/`||`/`==`). See issue #1010, PR #1017. (Session 1, 2026-03-06)

## Preferences (MED confidence)

- Use `IMemberReferenceOperation` (consolidates property, field, event references) instead of separate `IPropertyReferenceOperation`/`IFieldReferenceOperation`/`IEventReferenceOperation` cases when only checking `Instance` for null (static vs instance). Reduces code duplication. (Session 1, 2026-03-06)
- Each `IOperation` terminal type exercises a distinct code path even when test structure looks similar. `ILocalReferenceOperation`, `IParameterReferenceOperation`, `IFieldReferenceOperation` all terminate the receiver chain walk differently. Do not merge or deduplicate such tests. (Session 1, 2026-03-06)

## Edge Cases (MED confidence)

- Ternary expressions (`IConditionalOperation`) are not a recognized receiver type in `IsRootedInLambdaParameter`. The guard returns `false`, causing the entire subtree to be skipped. This prevents false positives but means non-virtual members inside ternaries are not flagged (false negative trade-off). Documented as known limitation with test `ShouldNotFlagConditionalWithMixedSources`. (Session 1, 2026-03-06)
- `IParenthesizedOperation` is never emitted by the C# compiler in IOperation trees (VB.NET only). Safe to omit from C#-only analyzers marked with `[DiagnosticAnalyzer(LanguageNames.CSharp)]`. (Session 1, 2026-03-06)
