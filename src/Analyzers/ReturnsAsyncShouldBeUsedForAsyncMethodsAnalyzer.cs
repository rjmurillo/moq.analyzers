namespace Moq.Analyzers;

/// <summary>
/// Async method setups should use ReturnsAsync instead of Returns with async lambda.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ReturnsAsyncShouldBeUsedForAsyncMethodsAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = "Moq: Invalid Returns usage with async method";
    private static readonly LocalizableString Message = "Async method '{0}' setups should use ReturnsAsync instead of Returns with async lambda";
    private static readonly LocalizableString Description = "Async method setups should use ReturnsAsync instead of Returns with async lambda.";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.ReturnsAsyncShouldBeUsedForAsyncMethods,
        Title,
        Message,
        DiagnosticCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description,
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
        if (!IsReturnsMethodCallWithAsyncLambda(invocation, context.SemanticModel, knownSymbols))
        {
            return;
        }

        // Find the Setup call that this Returns is chained from
        MemberAccessExpressionSyntax memberAccess = (MemberAccessExpressionSyntax)invocation.Expression;
        InvocationExpressionSyntax? setupInvocation = memberAccess.Expression.FindSetupInvocation(context.SemanticModel, knownSymbols);
        if (setupInvocation == null)
        {
            return;
        }

        // Check if the Setup is for an async method and get the method name
        string? methodName = GetAsyncMethodName(setupInvocation, context.SemanticModel, knownSymbols);
        if (methodName == null)
        {
            return;
        }

        // Report diagnostic on just the Returns(...) method call
        // Create a span from the Returns identifier through the end of the invocation
        int startPos = memberAccess.Name.SpanStart;
        int endPos = invocation.Span.End;
        Microsoft.CodeAnalysis.Text.TextSpan span = Microsoft.CodeAnalysis.Text.TextSpan.FromBounds(startPos, endPos);
        Location location = Location.Create(invocation.SyntaxTree, span);

        Diagnostic diagnostic = location.CreateDiagnostic(Rule, methodName);
        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsReturnsMethodCallWithAsyncLambda(InvocationExpressionSyntax invocation, SemanticModel semanticModel, MoqKnownSymbols knownSymbols)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax)
        {
            return false;
        }

        // Query the invocation (not the MemberAccessExpressionSyntax) so Roslyn has argument context
        // for overload resolution. Fall back to CandidateSymbols for delegate overloads.
        SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(invocation);
        bool isReturnsMethod = symbolInfo.Symbol is IMethodSymbol method
            ? method.IsMoqReturnsMethod(knownSymbols)
            : symbolInfo.CandidateSymbols
                .OfType<IMethodSymbol>()
                .Any(m => m.IsMoqReturnsMethod(knownSymbols));

        if (!isReturnsMethod)
        {
            return false;
        }

        // Check if the Returns call has an async lambda argument
        return HasAsyncLambdaArgument(invocation);
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

    private static string? GetAsyncMethodName(InvocationExpressionSyntax setupInvocation, SemanticModel semanticModel, MoqKnownSymbols knownSymbols)
    {
        // Check if this is a Setup method call
        if (setupInvocation.Expression is not MemberAccessExpressionSyntax setupMemberAccess)
        {
            return null;
        }

        SymbolInfo setupSymbolInfo = semanticModel.GetSymbolInfo(setupMemberAccess);
        if (setupSymbolInfo.Symbol is null || !setupSymbolInfo.Symbol.IsMoqSetupMethod(knownSymbols))
        {
            return null;
        }

        // Get the mocked method from the setup
        ExpressionSyntax? mockedMemberExpression = setupInvocation.FindMockedMemberExpressionFromSetupMethod();
        if (mockedMemberExpression == null)
        {
            return null;
        }

        SymbolInfo mockedSymbolInfo = semanticModel.GetSymbolInfo(mockedMemberExpression);
        if (mockedSymbolInfo.Symbol is not IMethodSymbol mockedMethod)
        {
            return null;
        }

        // Check if the mocked method returns Task or Task<T>
        if (!mockedMethod.ReturnType.IsTaskOrValueTaskType(knownSymbols))
        {
            return null;
        }

        // Return the method name
        return mockedMethod.Name;
    }
}
