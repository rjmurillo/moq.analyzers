using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers;

/// <summary>
/// LINQ to Mocks expressions should be valid and supported.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class LinqToMocksExpressionShouldBeValidAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = "Moq: Invalid LINQ to Mocks expression";
    private static readonly LocalizableString Message = "LINQ to Mocks expression contains non-virtual member '{0}' that cannot be mocked";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.LinqToMocksExpressionShouldBeValid,
        Title,
        Message,
        DiagnosticCategory.Moq,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.LinqToMocksExpressionShouldBeValid}.md");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(RegisterCompilationStartAction);
    }

    private static void RegisterCompilationStartAction(CompilationStartAnalysisContext context)
    {
        MoqKnownSymbols knownSymbols = new(context.Compilation);

        // Ensure Moq is referenced in the compilation
        if (!knownSymbols.IsMockReferenced())
        {
            return;
        }

        context.RegisterOperationAction(
            operationAnalysisContext => Analyze(operationAnalysisContext),
            OperationKind.Invocation);
    }

    private static void Analyze(OperationAnalysisContext context)
    {
        if (context.Operation is not IInvocationOperation invocationOperation)
        {
            return;
        }

        IMethodSymbol targetMethod = invocationOperation.TargetMethod;
        if (!IsValidMockOfMethod(targetMethod))
        {
            return;
        }

        // Look for lambda expressions in the arguments (LINQ to Mocks expressions)
        foreach (IArgumentOperation argument in invocationOperation.Arguments)
        {
            if (argument.Value is IAnonymousFunctionOperation lambdaOperation)
            {
                AnalyzeLambdaExpression(context, lambdaOperation);
            }
        }
    }

    private static bool IsValidMockOfMethod(IMethodSymbol? targetMethod)
    {
        if (targetMethod is null)
        {
            return false;
        }

        // Simple check for Mock.Of method
        return string.Equals(targetMethod.Name, "Of", StringComparison.Ordinal) &&
               string.Equals(targetMethod.ContainingType?.Name, "Mock", StringComparison.Ordinal);
    }

    private static void AnalyzeLambdaExpression(OperationAnalysisContext context, IAnonymousFunctionOperation lambdaOperation)
    {
        // Recursively walk through the lambda body to find member accesses
        WalkOperation(context, lambdaOperation.Body);
    }

    private static void WalkOperation(OperationAnalysisContext context, IOperation operation)
    {
        switch (operation.Kind)
        {
            case OperationKind.PropertyReference:
                if (operation is IPropertyReferenceOperation propertyRef)
                {
                    AnalyzePropertyReference(context, propertyRef);
                }

                break;

            case OperationKind.Invocation:
                if (operation is IInvocationOperation invocation)
                {
                    AnalyzeMethodInvocation(context, invocation);
                }

                break;
        }

        // Recursively process child operations
        foreach (IOperation childOperation in operation.ChildOperations)
        {
            WalkOperation(context, childOperation);
        }
    }

    private static void AnalyzePropertyReference(OperationAnalysisContext context, IPropertyReferenceOperation propertyRef)
    {
        if (propertyRef.Property.ContainingType?.TypeKind == TypeKind.Interface)
        {
            // Interface properties are always fine for mocking
            return;
        }

        // Check if the property is virtual or abstract
        if (!propertyRef.Property.IsVirtual && !propertyRef.Property.IsAbstract && !propertyRef.Property.IsOverride)
        {
            // Non-virtual properties cannot be mocked properly in LINQ to Mocks
            var diagnostic = propertyRef.Syntax.GetLocation().CreateDiagnostic(Rule, propertyRef.Property.Name);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static void AnalyzeMethodInvocation(OperationAnalysisContext context, IInvocationOperation invocation)
    {
        if (invocation.TargetMethod.ContainingType?.TypeKind == TypeKind.Interface)
        {
            // Interface methods are always fine for mocking
            return;
        }

        // Check if the method is virtual or abstract
        if (!invocation.TargetMethod.IsVirtual && !invocation.TargetMethod.IsAbstract && !invocation.TargetMethod.IsOverride)
        {
            // Non-virtual methods cannot be mocked properly in LINQ to Mocks
            var diagnostic = invocation.Syntax.GetLocation().CreateDiagnostic(Rule, invocation.TargetMethod.Name);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
