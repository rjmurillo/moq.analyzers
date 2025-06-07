using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers;

/// <summary>
/// Mock.Of should not use complex expressions that may not work as expected.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MockOfComplexExpressionAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = "Moq: Complex expression in Mock.Of";
    private static readonly LocalizableString Message = "Mock.Of expressions should be simple property assignments; complex expressions may not work as expected";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.MockOfComplexExpression,
        Title,
        Message,
        DiagnosticCategory.Moq,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.MockOfComplexExpression}.md");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        if (context.Operation is not IInvocationOperation invocationOperation)
        {
            return;
        }

        SemanticModel? semanticModel = invocationOperation.SemanticModel;
        if (semanticModel == null)
        {
            return;
        }

        MoqKnownSymbols knownSymbols = new(semanticModel.Compilation);
        IMethodSymbol targetMethod = invocationOperation.TargetMethod;

        if (!targetMethod.IsMockOfMethod(knownSymbols))
        {
            return;
        }

        if (invocationOperation.Arguments.Length > 0)
        {
            ValidateMockOfExpression(context, invocationOperation);
        }
    }

    private static void ValidateMockOfExpression(OperationAnalysisContext context, IInvocationOperation invocationOperation)
    {
        IOperation argumentOperation = invocationOperation.Arguments[0].Value;
        argumentOperation = argumentOperation.WalkDownImplicitConversion();

        if (argumentOperation is IAnonymousFunctionOperation lambdaOperation &&
            HasComplexExpression(lambdaOperation.Body))
        {
            Diagnostic diagnostic = invocationOperation.Syntax.CreateDiagnostic(Rule);
            context.ReportDiagnostic(diagnostic);
        }
    }

    /// <summary>
    /// Determines if the lambda body contains complex expressions that may not work as expected in Mock.Of.
    /// </summary>
    /// <param name="operation">The operation to analyze.</param>
    /// <returns>True if the expression is complex and should trigger a warning.</returns>
    private static bool HasComplexExpression(IOperation? operation)
    {
        if (operation == null)
        {
            return false;
        }

        switch (operation.Kind)
        {
            // Simple property access or assignment is fine
            case OperationKind.PropertyReference:
                return false;

            // Method calls in Mock.Of expressions can be problematic
            case OperationKind.Invocation:
                return true;

            // Nested Mock.Of calls are complex
            case OperationKind.Binary when operation is IBinaryOperation binaryOp:
                // Check if either side contains method calls or other Mock.Of calls
                return ContainsMethodCallOrNestedMockOf(binaryOp.LeftOperand) ||
                       ContainsMethodCallOrNestedMockOf(binaryOp.RightOperand);

            default:
                // For other operations, recursively check children
                foreach (IOperation child in operation.ChildOperations)
                {
                    if (HasComplexExpression(child))
                    {
                        return true;
                    }
                }

                return false;
        }
    }

    /// <summary>
    /// Checks if an operation contains method calls or nested Mock.Of calls.
    /// </summary>
    private static bool ContainsMethodCallOrNestedMockOf(IOperation operation)
    {
        if (operation.Kind == OperationKind.Invocation)
        {
            return true;
        }

        foreach (IOperation child in operation.ChildOperations)
        {
            if (ContainsMethodCallOrNestedMockOf(child))
            {
                return true;
            }
        }

        return false;
    }
}
