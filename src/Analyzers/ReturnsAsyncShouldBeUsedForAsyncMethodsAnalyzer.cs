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
        InvocationExpressionSyntax? setupInvocation = FindSetupInvocation(invocation);
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
        // We can safely cast here because IsReturnsMethodCallWithAsyncLambda already verified this is a MemberAccessExpressionSyntax
        MemberAccessExpressionSyntax memberAccess = (MemberAccessExpressionSyntax)invocation.Expression;

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

        if (!method.IsMoqReturnsMethod(knownSymbols))
        {
            return false;
        }

        // Check if the Returns call has an async lambda argument
        return HasAsyncLambdaArgument(invocation);
    }

    private static InvocationExpressionSyntax? FindSetupInvocation(InvocationExpressionSyntax returnsInvocation)
    {
        // The pattern is: mock.Setup(...).Returns(...)
        // The returnsInvocation is the entire chain, so we need to examine its structure
        if (returnsInvocation.Expression is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Expression is InvocationExpressionSyntax setupInvocation &&
            setupInvocation.Expression is MemberAccessExpressionSyntax setupMemberAccess &&
            string.Equals(setupMemberAccess.Name.Identifier.ValueText, "Setup", StringComparison.Ordinal))
        {
            return setupInvocation;
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
