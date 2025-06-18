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
    private static readonly LocalizableString Message = "LINQ to Mocks expression should be valid and supported";

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

        // Look for the Mock.Of() methods
        ImmutableArray<IMethodSymbol> ofMethods = knownSymbols.MockOf;
        if (ofMethods.IsEmpty)
        {
            return;
        }

        context.RegisterOperationAction(
            operationAnalysisContext => Analyze(operationAnalysisContext, knownSymbols),
            OperationKind.Invocation);
    }

    private static void Analyze(OperationAnalysisContext context, MoqKnownSymbols knownSymbols)
    {
        if (context.Operation is not IInvocationOperation invocationOperation)
        {
            return;
        }

        IMethodSymbol targetMethod = invocationOperation.TargetMethod;
        if (!IsValidMockOfMethod(targetMethod, knownSymbols))
        {
            return;
        }

        // Look for lambda expressions in the arguments (LINQ to Mocks expressions)
        foreach (IArgumentOperation argument in invocationOperation.Arguments)
        {
            if (argument.Value is IAnonymousFunctionOperation lambdaOperation)
            {
                AnalyzeLambdaExpression(context, lambdaOperation, knownSymbols);
            }
        }
    }

    private static bool IsValidMockOfMethod(IMethodSymbol? targetMethod, MoqKnownSymbols knownSymbols)
    {
        if (targetMethod is null || !targetMethod.IsStatic)
        {
            return false;
        }

        if (!string.Equals(targetMethod.Name, "Of", StringComparison.Ordinal))
        {
            return false;
        }

        return targetMethod.ContainingType is not null &&
               targetMethod.ContainingType.Equals(knownSymbols.Mock, SymbolEqualityComparer.Default);
    }

    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "Analyzing lambda expressions requires multiple checks")]
    private static void AnalyzeLambdaExpression(OperationAnalysisContext context, IAnonymousFunctionOperation lambdaOperation, MoqKnownSymbols knownSymbols)
    {
        // The lambda body might be a single expression or a block
        if (lambdaOperation.Body is IBlockOperation blockOperation)
        {
            foreach (IOperation operation in blockOperation.Operations)
            {
                AnalyzeOperation(context, operation, knownSymbols);
            }
        }
        else
        {
            // Single expression lambda (e.g., r => r.IsAuthenticated == true)
            AnalyzeExpression(context, lambdaOperation.Body, knownSymbols);
        }
    }

    private static void AnalyzeOperation(OperationAnalysisContext context, IOperation operation, MoqKnownSymbols knownSymbols)
    {
        switch (operation)
        {
            case IReturnOperation returnOperation when returnOperation.ReturnedValue != null:
                AnalyzeExpression(context, returnOperation.ReturnedValue, knownSymbols);
                break;
            case IExpressionStatementOperation expressionStatement:
                AnalyzeExpression(context, expressionStatement.Operation, knownSymbols);
                break;
            default:
                // For simple lambda expressions without return statements
                AnalyzeExpression(context, operation, knownSymbols);
                break;
        }
    }

    private static void AnalyzeExpression(OperationAnalysisContext context, IOperation expression, MoqKnownSymbols knownSymbols)
    {
        switch (expression)
        {
            case IBinaryOperation binaryOp:
                AnalyzeBinaryOperation(context, binaryOp, knownSymbols);
                break;
            case IInvocationOperation invocation:
                AnalyzeInvocationInLinqExpression(context, invocation);
                break;
            case IPropertyReferenceOperation propertyRef:
                AnalyzePropertyReference(context, propertyRef);
                break;
        }
    }

    private static void AnalyzeBinaryOperation(OperationAnalysisContext context, IBinaryOperation binaryOperation, MoqKnownSymbols knownSymbols)
    {
        // Check left side of binary operation (e.g., r.IsAuthenticated in r.IsAuthenticated == true)
        if (binaryOperation.LeftOperand is IPropertyReferenceOperation propertyRef)
        {
            AnalyzePropertyReference(context, propertyRef);
        }
        else if (binaryOperation.LeftOperand is IInvocationOperation invocation)
        {
            AnalyzeInvocationInLinqExpression(context, invocation);
        }

        // Recursively analyze nested binary operations
        if (binaryOperation.LeftOperand is IBinaryOperation leftBinary)
        {
            AnalyzeBinaryOperation(context, leftBinary, knownSymbols);
        }

        if (binaryOperation.RightOperand is IBinaryOperation rightBinary)
        {
            AnalyzeBinaryOperation(context, rightBinary, knownSymbols);
        }

        // Check for nested Mock.Of calls in the right operand
        if (binaryOperation.RightOperand is IInvocationOperation rightInvocation && IsNestedMockOfCall(rightInvocation, knownSymbols))
        {
            // This might be too complex - warn about nested Mock.Of expressions
            context.ReportDiagnostic(rightInvocation.Syntax.GetLocation().CreateDiagnostic(Rule));
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
            context.ReportDiagnostic(propertyRef.Syntax.GetLocation().CreateDiagnostic(Rule));
        }
    }

    private static void AnalyzeInvocationInLinqExpression(OperationAnalysisContext context, IInvocationOperation invocation)
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
            context.ReportDiagnostic(invocation.Syntax.GetLocation().CreateDiagnostic(Rule));
        }
    }

    private static bool IsNestedMockOfCall(IInvocationOperation invocation, MoqKnownSymbols knownSymbols)
    {
        return invocation.TargetMethod.IsInstanceOf(knownSymbols.MockOf);
    }
}
