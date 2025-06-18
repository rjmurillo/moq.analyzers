namespace Moq.Analyzers;

/// <summary>
/// Async method setups should use ReturnsAsync instead of Returns with async lambda.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ReturnsAsyncShouldBeUsedForAsyncMethodsAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = "Moq: Invalid Returns usage with async method";
    private static readonly LocalizableString Message = "Async method setups should use ReturnsAsync instead of Returns with async lambda";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.ReturnsAsyncShouldBeUsedForAsyncMethods,
        Title,
        Message,
        DiagnosticCategory.Moq,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.ReturnsAsyncShouldBeUsedForAsyncMethods}.md");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        MoqKnownSymbols knownSymbols = new(context.SemanticModel.Compilation);

        InvocationExpressionSyntax invocation = (InvocationExpressionSyntax)context.Node;

        // Check if this is a Returns method call with async lambda
        if (!IsReturnsMethodCallWithAsyncLambda(invocation, context.SemanticModel))
        {
            return;
        }

        // Find the Setup call that this Returns is chained from
        InvocationExpressionSyntax? setupInvocation = FindSetupInvocation(invocation);
        if (setupInvocation == null)
        {
            return;
        }

        // Check if the Setup is for an async method
        if (!IsSetupForAsyncMethod(setupInvocation, context.SemanticModel, knownSymbols))
        {
            return;
        }

        Diagnostic diagnostic = invocation.CreateDiagnostic(Rule);
        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsReturnsMethodCallWithAsyncLambda(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return false;
        }

        // Check if this is a Returns method call
        SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(memberAccess);
        if (symbolInfo.Symbol is not IMethodSymbol method)
        {
            return false;
        }

        if (!string.Equals(method.Name, "Returns", StringComparison.Ordinal) || method.ContainingType == null)
        {
            return false;
        }

        // Check if this is from Moq namespace
        if (!method.ContainingType.ToDisplayString().StartsWith("Moq.", StringComparison.Ordinal))
        {
            return false;
        }

        // Check if the Returns call has an async lambda argument
        return HasAsyncLambdaArgument(invocation);
    }

    private static InvocationExpressionSyntax? FindSetupInvocation(SyntaxNode returnsInvocation)
    {
        // Walk up the syntax tree to find the Setup invocation
        SyntaxNode? current = returnsInvocation.Parent;

        while (current != null)
        {
            if (current is InvocationExpressionSyntax invocation &&
                invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
string.Equals(memberAccess.Name.Identifier.ValueText, "Setup", StringComparison.Ordinal))
            {
                return invocation;
            }

            current = current.Parent;
        }

        return null;
    }

    private static bool HasAsyncLambdaArgument(InvocationExpressionSyntax invocation)
    {
        if (invocation.ArgumentList.Arguments.Count == 0)
        {
            return false;
        }

        ExpressionSyntax firstArgument = invocation.ArgumentList.Arguments[0].Expression;

        // Check for async lambda expressions
        return firstArgument is LambdaExpressionSyntax lambda &&
               lambda.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword);
    }

    private static bool IsSetupForAsyncMethod(InvocationExpressionSyntax setupInvocation, SemanticModel semanticModel, MoqKnownSymbols knownSymbols)
    {
        // Check if this is a Setup method call
        if (setupInvocation.Expression is not MemberAccessExpressionSyntax setupMemberAccess)
        {
            return false;
        }

        SymbolInfo setupSymbolInfo = semanticModel.GetSymbolInfo(setupMemberAccess);
        if (setupSymbolInfo.Symbol is null || !setupSymbolInfo.Symbol.IsMoqSetupMethod(knownSymbols))
        {
            return false;
        }

        // Get the mocked method from the setup
        ExpressionSyntax? mockedMemberExpression = setupInvocation.FindMockedMemberExpressionFromSetupMethod();
        if (mockedMemberExpression == null)
        {
            return false;
        }

        SymbolInfo mockedSymbolInfo = semanticModel.GetSymbolInfo(mockedMemberExpression);
        if (mockedSymbolInfo.Symbol is not IMethodSymbol mockedMethod)
        {
            return false;
        }

        // Check if the mocked method returns Task or Task<T>
        return IsTaskType(mockedMethod.ReturnType);
    }

    private static bool IsTaskType(ITypeSymbol returnType)
    {
        if (returnType is not INamedTypeSymbol namedType)
        {
            return false;
        }

        // Check for Task or Task<T>
        string typeName = namedType.OriginalDefinition.ToDisplayString();
        return string.Equals(typeName, "System.Threading.Tasks.Task", StringComparison.Ordinal) ||
string.Equals(typeName, "System.Threading.Tasks.Task<T>", StringComparison.Ordinal) ||
string.Equals(typeName, "System.Threading.Tasks.ValueTask", StringComparison.Ordinal) ||
string.Equals(typeName, "System.Threading.Tasks.ValueTask<T>", StringComparison.Ordinal);
    }
}
