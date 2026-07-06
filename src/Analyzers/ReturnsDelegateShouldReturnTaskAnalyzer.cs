using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Moq.Analyzers;

/// <summary>
/// Returns() delegate on async method setup should return Task/ValueTask to match the mocked method's return type.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ReturnsDelegateShouldReturnTaskAnalyzer : MoqDiagnosticAnalyzerBase
{
    private static readonly LocalizableString Title = "Moq: Returns() delegate type mismatch on async method";
    private static readonly LocalizableString Message = "Returns() delegate for async method '{0}' should return '{2}', not '{1}'. Use ReturnsAsync() or wrap with Task.FromResult().";
    private static readonly LocalizableString Description = "Returns() delegate on async method setup should return Task/ValueTask. Use ReturnsAsync() or wrap with Task.FromResult().";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.ReturnsDelegateMismatchOnAsyncMethod,
        Title,
        Message,
        DiagnosticCategory.Correctness,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.ReturnsDelegateMismatchOnAsyncMethod}.md");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    private protected override void RegisterCompilationActions(CompilationStartAnalysisContext context, MoqKnownSymbols knownSymbols)
    {
        context.RegisterSyntaxNodeAction(
            syntaxNodeContext => Analyze(syntaxNodeContext, knownSymbols),
            SyntaxKind.InvocationExpression);
    }

    private static void Analyze(SyntaxNodeAnalysisContext context, MoqKnownSymbols knownSymbols)
    {
        InvocationExpressionSyntax invocation = (InvocationExpressionSyntax)context.Node;

        if (!IsReturnsMethodCallWithSyncDelegate(
            invocation,
            context.SemanticModel,
            knownSymbols,
            out MemberAccessExpressionSyntax? memberAccess,
            out InvocationExpressionSyntax? setupInvocation,
            context.CancellationToken))
        {
            return;
        }

        if (!TryGetMismatchInfo(
            setupInvocation,
            invocation,
            context.SemanticModel,
            knownSymbols,
            out string? methodName,
            out ITypeSymbol? expectedReturnType,
            out ITypeSymbol? delegateReturnType,
            context.CancellationToken))
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
        [NotNullWhen(true)] out InvocationExpressionSyntax? setupInvocation,
        CancellationToken cancellationToken)
    {
        memberAccess = null;
        setupInvocation = null;

        // ADR-001 permits this name check only as a pre-filter; the symbol check below is authoritative.
        if (invocation.Expression is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Returns" } access)
        {
            return false;
        }

        // Query the invocation (not the MemberAccessExpressionSyntax) so Roslyn has argument context
        // for overload resolution. Fall back to CandidateSymbols for delegate overloads.
        SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(invocation, cancellationToken);
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

        if (!HasSyncDelegateArgument(invocation, semanticModel, cancellationToken))
        {
            return false;
        }

        setupInvocation = access.Expression.FindSetupInvocation(semanticModel, knownSymbols, cancellationToken);
        if (setupInvocation == null)
        {
            return false;
        }

        memberAccess = access;
        return true;
    }

    private static bool HasSyncDelegateArgument(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
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
        return IsMethodGroupExpression(firstArgument, semanticModel, cancellationToken);
    }

    private static bool IsMethodGroupExpression(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        // Invocations (e.g., GetInt()) resolve to IMethodSymbol but are values, not method groups.
        if (expression is InvocationExpressionSyntax)
        {
            return false;
        }

        SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(expression, cancellationToken);
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
        [NotNullWhen(true)] out ITypeSymbol? delegateReturnType,
        CancellationToken cancellationToken)
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

        SymbolInfo mockedSymbolInfo = semanticModel.GetSymbolInfo(mockedMemberExpression, cancellationToken);
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
        delegateReturnType = GetDelegateReturnType(returnsInvocation, semanticModel, cancellationToken);
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

    private static ITypeSymbol? GetDelegateReturnType(
        InvocationExpressionSyntax returnsInvocation,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        if (returnsInvocation.ArgumentList.Arguments.Count == 0)
        {
            return null;
        }

        ExpressionSyntax firstArgument = returnsInvocation.ArgumentList.Arguments[0].Expression;

        // For anonymous functions with explicit Returns<T1..TN> type arguments, prefer body
        // analysis. Roslyn target-types the function against the selected delegate and can
        // report Task<int> even when the body returns int.
        if (HasExplicitReturnsTypeArguments(returnsInvocation)
            && firstArgument is AnonymousFunctionExpressionSyntax anonymousFunction)
        {
            return GetReturnTypeFromAnonymousFunction(anonymousFunction, semanticModel, cancellationToken);
        }

        // For anonymous methods, prefer body analysis. Roslyn may infer the return type
        // from the target delegate type (e.g., Task<int>) for parameterless anonymous methods,
        // masking the actual body return type (e.g., int).
        if (firstArgument is AnonymousMethodExpressionSyntax { Body: BlockSyntax block })
        {
            return GetReturnTypeFromBlock(block, semanticModel, cancellationToken);
        }

        // GetSymbolInfo resolves lambdas to IMethodSymbol even when type conversion fails.
        // Raw values resolve to ILocalSymbol/IFieldSymbol/etc., filtered by the type check.
        SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(firstArgument, cancellationToken);
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

    private static bool HasExplicitReturnsTypeArguments(InvocationExpressionSyntax returnsInvocation)
    {
        return returnsInvocation.Expression is MemberAccessExpressionSyntax
        {
            Name: GenericNameSyntax,
        };
    }

    private static ITypeSymbol? GetReturnTypeFromAnonymousFunction(
        AnonymousFunctionExpressionSyntax anonymousFunction,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        Debug.Assert(!anonymousFunction.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword), "Async lambdas are handled by Moq1206.");

        if (anonymousFunction is AnonymousMethodExpressionSyntax { Body: BlockSyntax anonymousMethodBlock })
        {
            return GetReturnTypeFromBlock(anonymousMethodBlock, semanticModel, cancellationToken);
        }

        // AnonymousMethodExpressionSyntax (handled above) and LambdaExpressionSyntax are the
        // only two AnonymousFunctionExpressionSyntax subtypes, so what remains is a lambda.
        Debug.Assert(anonymousFunction is LambdaExpressionSyntax, "Only anonymous methods and lambdas derive from AnonymousFunctionExpressionSyntax.");
        LambdaExpressionSyntax lambda = (LambdaExpressionSyntax)anonymousFunction;

        if (lambda.Block is BlockSyntax lambdaBlock)
        {
            return GetReturnTypeFromBlock(lambdaBlock, semanticModel, cancellationToken);
        }

        // A lambda without a block body always has an expression body.
        Debug.Assert(lambda.ExpressionBody is not null, "A block-less lambda has an expression body.");
        return semanticModel.GetTypeInfo(lambda.ExpressionBody!, cancellationToken).Type;
    }

    private static ITypeSymbol? GetReturnTypeFromBlock(
        BlockSyntax block,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
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

        return semanticModel.GetTypeInfo(returnStatement.Expression, cancellationToken).Type;
    }
}
