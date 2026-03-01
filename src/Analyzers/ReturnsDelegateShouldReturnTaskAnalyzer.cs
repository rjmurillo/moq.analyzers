using System.Diagnostics.CodeAnalysis;

namespace Moq.Analyzers;

/// <summary>
/// Returns() delegate on async method setup should return Task/ValueTask to match the mocked method's return type.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ReturnsDelegateShouldReturnTaskAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = "Moq: Returns() delegate type mismatch on async method";
    private static readonly LocalizableString Message = "Returns() delegate for async method '{0}' should return '{2}', not '{1}'. Use ReturnsAsync() or wrap with Task.FromResult().";
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

        if (!IsReturnsMethodCallWithSyncDelegate(invocation, context.SemanticModel, knownSymbols, out MemberAccessExpressionSyntax? memberAccess, out InvocationExpressionSyntax? setupInvocation))
        {
            return;
        }

        if (!TryGetMismatchInfo(setupInvocation, invocation, context.SemanticModel, knownSymbols, out string? methodName, out ITypeSymbol? expectedReturnType, out ITypeSymbol? delegateReturnType))
        {
            return;
        }

        // Report diagnostic spanning from "Returns" identifier through the closing paren
        int startPos = memberAccess.Name.SpanStart;
        int endPos = invocation.Span.End;
        Microsoft.CodeAnalysis.Text.TextSpan span = Microsoft.CodeAnalysis.Text.TextSpan.FromBounds(startPos, endPos);
        Location location = Location.Create(invocation.SyntaxTree, span);

        string actualDisplay = delegateReturnType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        string expectedDisplay = expectedReturnType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        Diagnostic diagnostic = location.CreateDiagnostic(Rule, methodName, actualDisplay, expectedDisplay);
        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsReturnsMethodCallWithSyncDelegate(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        MoqKnownSymbols knownSymbols,
        [NotNullWhen(true)] out MemberAccessExpressionSyntax? memberAccess,
        [NotNullWhen(true)] out InvocationExpressionSyntax? setupInvocation)
    {
        memberAccess = null;
        setupInvocation = null;

        if (invocation.Expression is not MemberAccessExpressionSyntax access)
        {
            return false;
        }

        // Query the invocation (not the MemberAccessExpressionSyntax) so Roslyn has argument context
        // for overload resolution. Fall back to CandidateSymbols for delegate overloads.
        SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(invocation);
        bool isReturnsMethod = symbolInfo.Symbol is IMethodSymbol method
            ? method.IsMoqReturnsMethod(knownSymbols)
            : symbolInfo.CandidateReason is CandidateReason.OverloadResolutionFailure
              && symbolInfo.CandidateSymbols
                  .OfType<IMethodSymbol>()
                  .Any(m => m.IsMoqReturnsMethod(knownSymbols));

        if (!isReturnsMethod)
        {
            return false;
        }

        if (!HasSyncDelegateArgument(invocation, semanticModel))
        {
            return false;
        }

        setupInvocation = access.Expression.FindSetupInvocation(semanticModel, knownSymbols);
        if (setupInvocation == null)
        {
            return false;
        }

        memberAccess = access;
        return true;
    }

    private static bool HasSyncDelegateArgument(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        if (invocation.ArgumentList.Arguments.Count == 0)
        {
            return false;
        }

        ExpressionSyntax firstArgument = invocation.ArgumentList.Arguments[0].Expression;

        // Lambdas and anonymous methods share AnonymousFunctionExpressionSyntax,
        // which exposes AsyncKeyword for sync/async detection.
        if (firstArgument is AnonymousFunctionExpressionSyntax anonymousFunction)
        {
            return !anonymousFunction.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword);
        }

        // Method groups require semantic resolution to distinguish from raw values.
        return IsMethodGroupExpression(firstArgument, semanticModel);
    }

    private static bool IsMethodGroupExpression(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        // Invocations (e.g., GetInt()) resolve to IMethodSymbol but are values, not method groups.
        if (expression is InvocationExpressionSyntax)
        {
            return false;
        }

        SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(expression);
        if (symbolInfo.Symbol is IMethodSymbol)
        {
            return true;
        }

        // Method groups with overloads fail resolution when no single overload matches the expected delegate type
        return symbolInfo.CandidateReason is CandidateReason.OverloadResolutionFailure or CandidateReason.MemberGroup
            && symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().Any();
    }

    private static bool TryGetMismatchInfo(
        InvocationExpressionSyntax setupInvocation,
        InvocationExpressionSyntax returnsInvocation,
        SemanticModel semanticModel,
        MoqKnownSymbols knownSymbols,
        [NotNullWhen(true)] out string? methodName,
        [NotNullWhen(true)] out ITypeSymbol? expectedReturnType,
        [NotNullWhen(true)] out ITypeSymbol? delegateReturnType)
    {
        methodName = null;
        expectedReturnType = null;
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

        // Get the delegate's return type from the Returns() argument
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
        expectedReturnType = returnType;
        return true;
    }

    private static ITypeSymbol? GetDelegateReturnType(InvocationExpressionSyntax returnsInvocation, SemanticModel semanticModel)
    {
        if (returnsInvocation.ArgumentList.Arguments.Count == 0)
        {
            return null;
        }

        ExpressionSyntax firstArgument = returnsInvocation.ArgumentList.Arguments[0].Expression;

        // For anonymous methods, prefer body analysis. Roslyn may infer the return type
        // from the target delegate type (e.g., Task<int>) for parameterless anonymous methods,
        // masking the actual body return type (e.g., int).
        if (firstArgument is AnonymousMethodExpressionSyntax { Body: BlockSyntax block })
        {
            return GetReturnTypeFromBlock(block, semanticModel);
        }

        // GetSymbolInfo resolves lambdas to IMethodSymbol even when type conversion fails.
        // Raw values resolve to ILocalSymbol/IFieldSymbol/etc., filtered by the type check.
        SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(firstArgument);
        if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
        {
            return methodSymbol.ReturnType;
        }

        // Method groups with type conversion errors may not resolve via Symbol.
        // Fall back to CandidateSymbols only when all candidates agree on the return type.
        IMethodSymbol[] candidates = symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().ToArray();
        if (candidates.Length > 0
            && candidates.All(c => SymbolEqualityComparer.Default.Equals(c.ReturnType, candidates[0].ReturnType)))
        {
            return candidates[0].ReturnType;
        }

        return null;
    }

    private static ITypeSymbol? GetReturnTypeFromBlock(BlockSyntax block, SemanticModel semanticModel)
    {
        // Find the first return statement in this block,
        // pruning nested functions so we don't pick up their returns.
        ReturnStatementSyntax? returnStatement = block
            .DescendantNodes(node =>
                node is not AnonymousFunctionExpressionSyntax
                && node is not LocalFunctionStatementSyntax)
            .OfType<ReturnStatementSyntax>()
            .FirstOrDefault();

        if (returnStatement?.Expression == null)
        {
            return null;
        }

        return semanticModel.GetTypeInfo(returnStatement.Expression).Type;
    }
}
