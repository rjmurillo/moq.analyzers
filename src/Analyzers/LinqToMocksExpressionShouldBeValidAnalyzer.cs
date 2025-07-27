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

        // Check if this is a Mock.Of invocation
        if (!IsValidMockOfInvocation(invocationOperation, knownSymbols))
        {
            return;
        }

        // Analyze lambda expressions in the arguments
        AnalyzeMockOfArguments(context, invocationOperation);
    }

    /// <summary>
    /// Determines if the operation is a valid Mock.Of() invocation.
    /// </summary>
    private static bool IsValidMockOfInvocation(IInvocationOperation invocation, MoqKnownSymbols knownSymbols)
    {
        IMethodSymbol targetMethod = invocation.TargetMethod;

        // Check if this is a static method call to Mock.Of()
        if (!targetMethod.IsStatic || !string.Equals(targetMethod.Name, "Of", StringComparison.Ordinal))
        {
            return false;
        }

        return targetMethod.ContainingType is not null &&
               targetMethod.ContainingType.Equals(knownSymbols.Mock, SymbolEqualityComparer.Default);
    }

    private static void AnalyzeMockOfArguments(OperationAnalysisContext context, IInvocationOperation invocationOperation)
    {
        // Look for lambda expressions in the arguments (LINQ to Mocks expressions)
        foreach (IArgumentOperation argument in invocationOperation.Arguments)
        {
            IOperation argumentValue = argument.Value.WalkDownImplicitConversion();

            if (argumentValue is IAnonymousFunctionOperation lambdaOperation)
            {
                AnalyzeLambdaExpression(context, lambdaOperation);
            }
        }
    }

    private static void AnalyzeLambdaExpression(OperationAnalysisContext context, IAnonymousFunctionOperation lambdaOperation)
    {
        // For LINQ to Mocks, we need to handle more complex expressions like: x => x.Property == "value"
        // The lambda body is often a binary expression where the left operand is the member we want to check
        AnalyzeLambdaBody(context, lambdaOperation, lambdaOperation.Body);
    }

    private static void AnalyzeLambdaBody(OperationAnalysisContext context, IAnonymousFunctionOperation lambdaOperation, IOperation? body)
    {
        if (body == null)
        {
            return;
        }

        switch (body)
        {
            case IBlockOperation blockOp when blockOp.Operations.Length == 1:
                // Handle block lambdas with return statements
                AnalyzeLambdaBody(context, lambdaOperation, blockOp.Operations[0]);
                break;

            case IReturnOperation returnOp:
                // Handle return statements
                AnalyzeLambdaBody(context, lambdaOperation, returnOp.ReturnedValue);
                break;

            case IBinaryOperation binaryOp:
                // Handle binary expressions like equality comparisons
                AnalyzeMemberOperations(context, lambdaOperation, binaryOp.LeftOperand);
                AnalyzeMemberOperations(context, lambdaOperation, binaryOp.RightOperand);
                break;

            case IPropertyReferenceOperation propertyRef:
                // Direct property reference
                AnalyzeMemberSymbol(context, propertyRef.Property, lambdaOperation);
                break;

            case IInvocationOperation methodOp:
                // Direct method invocation
                AnalyzeMemberSymbol(context, methodOp.TargetMethod, lambdaOperation);
                break;

            case IFieldReferenceOperation fieldRef:
                // Direct field reference
                AnalyzeMemberSymbol(context, fieldRef.Field, lambdaOperation);
                break;

            default:
                // For other complex expressions, try to recursively find member references
                foreach (IOperation childOperation in body.ChildOperations)
                {
                    AnalyzeLambdaBody(context, lambdaOperation, childOperation);
                }

                break;
        }
    }

    private static void AnalyzeMemberOperations(OperationAnalysisContext context, IAnonymousFunctionOperation lambdaOperation, IOperation? operation)
    {
        if (operation == null)
        {
            return;
        }

        // Don't recursively analyze nested Mock.Of calls to avoid false positives
        if (operation is IInvocationOperation invocation)
        {
            MoqKnownSymbols knownSymbols = new(context.Operation.SemanticModel!.Compilation);
            if (IsValidMockOfInvocation(invocation, knownSymbols))
            {
                return; // Skip analyzing nested Mock.Of calls
            }
        }

        // Recursively analyze the operation to find member references
        AnalyzeLambdaBody(context, lambdaOperation, operation);
    }

    private static void AnalyzeMemberSymbol(OperationAnalysisContext context, ISymbol memberSymbol, IAnonymousFunctionOperation lambdaOperation)
    {
        // Only report diagnostics if the member is not from an interface
        if (memberSymbol.ContainingType?.TypeKind == TypeKind.Interface)
        {
            return;
        }

        // Only report diagnostics if the lambda is semantically valid (no compiler errors in the member access span)
        Location? memberLocation = GetMemberReferenceLocation(lambdaOperation, memberSymbol, context.Operation.SemanticModel);
        if (memberLocation == null)
        {
            return;
        }

        if (context.Operation.SemanticModel is not null)
        {
            ImmutableArray<Diagnostic> diagnostics = context.Operation.SemanticModel.GetDiagnostics(memberLocation.SourceSpan, context.CancellationToken);
            if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            {
                return;
            }
        }

        // Only report diagnostics for non-virtual, non-abstract, non-override members
        switch (memberSymbol)
        {
            case IPropertySymbol propertySymbol:
                if (!ShouldReportForProperty(propertySymbol))
                {
                    return;
                }

                break;
            case IMethodSymbol methodSymbol:
                if (!ShouldReportForMethod(methodSymbol))
                {
                    return;
                }

                break;
            case IFieldSymbol:
                // Always report for fields (fields are never virtual/abstract/override)
                break;
            default:
                // For any other symbol types, do not report
                return;
        }

        context.ReportDiagnostic(memberLocation.CreateDiagnostic(Rule, memberSymbol.Name));
    }

    private static bool ShouldReportForProperty(IPropertySymbol property)
    {
        // Report diagnostic if property is not virtual, abstract, or override
        return property is { IsVirtual: false, IsAbstract: false, IsOverride: false };
    }

    private static bool ShouldReportForMethod(IMethodSymbol method)
    {
        // Report diagnostic if method is not virtual, abstract, or override
        return method is { IsVirtual: false, IsAbstract: false, IsOverride: false };
    }

    /// <summary>
    /// Attempts to find the specific syntax location of the member reference within the lambda using symbol-based matching.
    /// </summary>
    private static Location? GetMemberReferenceLocation(IAnonymousFunctionOperation lambdaOperation, ISymbol memberSymbol, SemanticModel? semanticModel)
    {
        SyntaxNode syntax = lambdaOperation.Syntax;

        // 1. Try InvocationExpressionSyntax (for method calls)
        Location? location = syntax.FindLocation<InvocationExpressionSyntax>(memberSymbol, semanticModel);
        if (location != null)
        {
            return location;
        }

        // 2. Try MemberAccessExpressionSyntax (for property/field access)
        location = syntax.FindLocation<MemberAccessExpressionSyntax>(memberSymbol, semanticModel);
        if (location != null)
        {
            return location;
        }

        // Note for future maintainers:
        // The fallback to IdentifierNameSyntax is intentionally omitted. In the context of Moq LINQ-to-Mocks expressions,
        // member references are always accessed via the lambda parameter (e.g., x => x.Property or x => x.Method()),
        // which are represented as MemberAccessExpressionSyntax or InvocationExpressionSyntax. A bare identifier (IdentifierNameSyntax)
        // would only occur for static members or locals, which are not relevant for this analyzer and cannot be mocked.
        // No valid or invalid Moq usage can reach this code path, so it is not implemented and this is not an invalid omission.
        return null;
    }
}
