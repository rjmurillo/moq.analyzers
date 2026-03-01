using System.Diagnostics.CodeAnalysis;

namespace Moq.Analyzers;

/// <summary>
/// Returns() delegate on async method setup should return Task/ValueTask to match the mocked method's return type.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ReturnsDelegateShouldReturnTaskAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = "Moq: Returns() delegate type mismatch on async method";
    private static readonly LocalizableString Message = "Returns() delegate for async method '{0}' should return a Task type, not '{1}'. Use ReturnsAsync() or return Task.FromResult().";
    private static readonly LocalizableString Description = "Returns() delegate on async method setup should return Task/ValueTask. Use ReturnsAsync() or wrap with Task.FromResult().";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.ReturnsDelegateMismatchOnAsyncMethod,
        Title,
        Message,
        DiagnosticCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.ReturnsDelegateMismatchOnAsyncMethod}.md");

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

        if (!IsReturnsMethodCallWithSyncDelegate(invocation, context.SemanticModel, knownSymbols))
        {
            return;
        }

        InvocationExpressionSyntax? setupInvocation = FindSetupInvocation(invocation, context.SemanticModel, knownSymbols);
        if (setupInvocation == null)
        {
            return;
        }

        if (!TryGetMismatchInfo(setupInvocation, invocation, context.SemanticModel, knownSymbols, out string? methodName, out ITypeSymbol? delegateReturnType))
        {
            return;
        }

        // Report diagnostic spanning from "Returns" identifier through the closing paren
        MemberAccessExpressionSyntax memberAccess = (MemberAccessExpressionSyntax)invocation.Expression;
        int startPos = memberAccess.Name.SpanStart;
        int endPos = invocation.Span.End;
        Microsoft.CodeAnalysis.Text.TextSpan span = Microsoft.CodeAnalysis.Text.TextSpan.FromBounds(startPos, endPos);
        Location location = Location.Create(invocation.SyntaxTree, span);

        Diagnostic diagnostic = location.CreateDiagnostic(Rule, methodName, delegateReturnType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsReturnsMethodCallWithSyncDelegate(InvocationExpressionSyntax invocation, SemanticModel semanticModel, MoqKnownSymbols knownSymbols)
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

        return HasSyncDelegateArgument(invocation);
    }

    private static bool HasSyncDelegateArgument(InvocationExpressionSyntax invocation)
    {
        if (invocation.ArgumentList.Arguments.Count == 0)
        {
            return false;
        }

        ExpressionSyntax firstArgument = invocation.ArgumentList.Arguments[0].Expression;

        // Must be a lambda or delegate. Raw values (not delegates) are a different overload.
        if (firstArgument is not LambdaExpressionSyntax lambda)
        {
            return false;
        }

        // Exclude async lambdas. Those are Moq1206's domain.
        return !lambda.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword);
    }

    private static InvocationExpressionSyntax? FindSetupInvocation(InvocationExpressionSyntax returnsInvocation, SemanticModel semanticModel, MoqKnownSymbols knownSymbols)
    {
        // Walk up the fluent chain to find Setup. Handles patterns like:
        // mock.Setup(...).Returns(...)
        // mock.Setup(...).Callback(...).Returns(...)
        if (returnsInvocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return null;
        }

        // Moq fluent chains are short (Setup.Callback.Returns at most 3-4 deep).
        // Guard against pathological syntax trees.
        const int maxChainDepth = 10;
        ExpressionSyntax current = memberAccess.Expression;

        for (int depth = 0; depth < maxChainDepth; depth++)
        {
            ExpressionSyntax unwrapped = current.WalkDownParentheses();

            if (unwrapped is not InvocationExpressionSyntax candidateInvocation)
            {
                return null;
            }

            if (candidateInvocation.Expression is not MemberAccessExpressionSyntax candidateMemberAccess)
            {
                return null;
            }

            SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(candidateMemberAccess);
            if (symbolInfo.Symbol != null && symbolInfo.Symbol.IsMoqSetupMethod(knownSymbols))
            {
                return candidateInvocation;
            }

            // Continue walking up the chain (past Callback, etc.)
            current = candidateMemberAccess.Expression;
        }

        return null;
    }

    private static bool TryGetMismatchInfo(
        InvocationExpressionSyntax setupInvocation,
        InvocationExpressionSyntax returnsInvocation,
        SemanticModel semanticModel,
        MoqKnownSymbols knownSymbols,
        [NotNullWhen(true)] out string? methodName,
        [NotNullWhen(true)] out ITypeSymbol? delegateReturnType)
    {
        methodName = null;
        delegateReturnType = null;

        // Get the mocked method from the Setup lambda
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

        // The mocked method must return Task<T> or ValueTask<T> (generic variants).
        // Non-generic Task/ValueTask have no inner type to mismatch against.
        ITypeSymbol returnType = mockedMethod.ReturnType;
        if (returnType is not INamedTypeSymbol { IsGenericType: true })
        {
            return false;
        }

        if (!returnType.IsTaskOrValueTaskType(knownSymbols))
        {
            return false;
        }

        // Get the delegate's return type from the lambda argument
        delegateReturnType = GetDelegateReturnType(returnsInvocation, semanticModel);
        if (delegateReturnType == null)
        {
            return false;
        }

        // If the delegate already returns a Task/ValueTask type, no mismatch
        if (delegateReturnType.IsTaskOrValueTaskType(knownSymbols))
        {
            return false;
        }

        methodName = mockedMethod.Name;
        return true;
    }

    private static ITypeSymbol? GetDelegateReturnType(InvocationExpressionSyntax returnsInvocation, SemanticModel semanticModel)
    {
        if (returnsInvocation.ArgumentList.Arguments.Count == 0)
        {
            return null;
        }

        ExpressionSyntax firstArgument = returnsInvocation.ArgumentList.Arguments[0].Expression;

        if (firstArgument is not LambdaExpressionSyntax lambda)
        {
            return null;
        }

        // Expression-bodied lambda: () => 42
        if (lambda.Body is ExpressionSyntax bodyExpression)
        {
            TypeInfo typeInfo = semanticModel.GetTypeInfo(bodyExpression);
            return typeInfo.Type;
        }

        // Block-bodied lambda with explicit return statement
        if (lambda.Body is BlockSyntax block)
        {
            ReturnStatementSyntax? returnStatement = FindFirstReturnStatement(block);

            if (returnStatement?.Expression == null)
            {
                return null;
            }

            TypeInfo returnTypeInfo = semanticModel.GetTypeInfo(returnStatement.Expression);
            return returnTypeInfo.Type;
        }

        return null;
    }

    private static ReturnStatementSyntax? FindFirstReturnStatement(BlockSyntax block)
    {
        foreach (StatementSyntax statement in block.Statements)
        {
            if (statement is ReturnStatementSyntax returnStatement)
            {
                return returnStatement;
            }
        }

        return null;
    }
}
