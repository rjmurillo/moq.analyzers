# Core Architectural Patterns

Patterns used consistently across all 24 analyzers and 5 fixers. Follow these when adding new components.

## WellKnown Types Pattern

Resolves and caches Moq framework symbols once per compilation via `RegisterCompilationStartAction`.

```csharp
// In Initialize():
context.RegisterCompilationStartAction(compilationContext =>
{
    MoqKnownSymbols knownSymbols = new(compilationContext.Compilation);
    if (!knownSymbols.IsMockReferenced()) return; // Moq not referenced in this project

    compilationContext.RegisterOperationAction(
        ctx => AnalyzeOperation(ctx, knownSymbols),
        OperationKind.Invocation);
});

// In AnalyzeOperation():
// knownSymbols is received as a parameter — created once, reused across all operations
if (typeSymbol.Equals(knownSymbols.Mock1, SymbolEqualityComparer.Default)) {
    // This is Moq.Mock<T>
}
```

Files: `src/Common/WellKnown/MoqKnownSymbols.cs`, `KnownSymbols.cs`, `MoqKnownSymbolExtensions.cs`

## Early Exit Pattern

Exit immediately when the code under analysis is not relevant. Order checks cheapest-first.

```csharp
// 1. Check if Moq is even referenced (uses IsMockReferenced(), not a null check on a property)
if (!knownSymbols.IsMockReferenced()) return;
// 2. Name pre-filter (performance hint only — NOT a semantic check; must be followed by step 3)
if (invocation.TargetMethod.ContainingType.Name != "Mock") return;
// 3. Full symbol comparison (expensive — only do if cheap checks pass)
if (!invocation.TargetMethod.ContainingType.Equals(knownSymbols.Mock1, SymbolEqualityComparer.Default)) return;
// 4. Perform actual analysis
```

## MoqKnownSymbols Lifetime

Create once inside `RegisterCompilationStartAction`; pass through call chains via closure or parameter. Do NOT create inside per-operation callbacks.

```csharp
context.RegisterCompilationStartAction(compilationContext =>
{
    MoqKnownSymbols knownSymbols = new(compilationContext.Compilation);
    if (!knownSymbols.IsMockReferenced()) return;
    compilationContext.RegisterOperationAction(
        ctx => AnalyzeInvocation(ctx, knownSymbols), // knownSymbols captured once
        OperationKind.Invocation);
});
```

## Diagnostic Location Precision

Highlight the specific problem token, not the whole expression.

```csharp
// Highlight the method name, not the entire invocation
var location = invocationSyntax.Expression.GetLocation();
// Highlight "SealedClass" in: new Mock<SealedClass>()
var location = objectCreation.Type.GetLocation();
```

## Template Method Base Class

`MockBehaviorDiagnosticAnalyzerBase` handles Roslyn registration and argument extraction. Derived classes implement business rules only. Use this pattern when multiple analyzers share the same analysis trigger.

## Analyzer Registration Boilerplate

```csharp
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class YourAnalyzer : DiagnosticAnalyzer
{
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(compilationContext =>
        {
            MoqKnownSymbols knownSymbols = new(compilationContext.Compilation);
            if (!knownSymbols.IsMockReferenced()) return;
            compilationContext.RegisterOperationAction(
                ctx => Analyze(ctx, knownSymbols),
                OperationKind.Invocation);
        });
    }
}
```

## Code Fix Registration Boilerplate

```csharp
[ExportCodeFixProvider(LanguageNames.CSharp)]
public class YourFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => [DiagnosticIds.YourRule];

    public override FixAllProvider? GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context) { ... }
}
```
