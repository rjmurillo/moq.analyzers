# Core Architectural Patterns

Patterns used consistently across all 23 analyzers and 5 fixers. Follow these when adding new components.

## WellKnown Types Pattern

Resolves and caches Moq framework symbols once per compilation.

```csharp
// In Initialize():
context.RegisterOperationAction(AnalyzeOperation, OperationKind.Invocation);

// In AnalyzeOperation():
var moqSymbols = MoqKnownSymbols.Create(context.Compilation);
if (moqSymbols.Mock == null) return; // Moq not referenced in this project

if (typeSymbol.Equals(moqSymbols.Mock, SymbolEqualityComparer.Default)) {
    // This is Moq.Mock<T>
}
```

Files: `src/Common/WellKnown/MoqKnownSymbols.cs`, `KnownSymbols.cs`, `MoqKnownSymbolExtensions.cs`

## Early Exit Pattern

Exit immediately when the code under analysis is not relevant. Order checks cheapest-first.

```csharp
// 1. Check if Moq is even referenced
if (moqSymbols.Mock == null) return;
// 2. Check containing type (cheaper than full symbol comparison)
if (invocation.TargetMethod.ContainingType.Name != "Mock") return;
// 3. Full symbol comparison (expensive — only do if cheap checks pass)
if (!invocation.TargetMethod.ContainingType.Equals(moqSymbols.Mock, SymbolEqualityComparer.Default)) return;
// 4. Perform actual analysis
```

## MoqKnownSymbols Lifetime

Create once at the entry point; pass through call chains. Do NOT create per-operation.

```csharp
// Correct:
private static void AnalyzeInvocation(OperationAnalysisContext context) {
    var moqSymbols = MoqKnownSymbols.Create(context.Compilation); // once
    ValidateMember(operation, moqSymbols);                         // passed down
}

// Wrong — allocates on every operation:
private static void ValidateMember(IOperation op, OperationAnalysisContext context) {
    var moqSymbols = MoqKnownSymbols.Create(context.Compilation); // repeated allocation
}
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
        context.ConfigureGeneratedCodeAnalysisMode(GeneratedCodeAnalysisMode.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(Analyze, OperationKind.Invocation);
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
