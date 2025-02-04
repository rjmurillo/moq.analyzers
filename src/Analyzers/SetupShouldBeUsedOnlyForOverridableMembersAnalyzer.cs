using System.Diagnostics.CodeAnalysis;

namespace Moq.Analyzers;

/// <summary>
/// Setup should be used only for overridable members.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SetupShouldBeUsedOnlyForOverridableMembersAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = "Moq: Invalid setup parameter";
    private static readonly LocalizableString Message = "Setup should be used only for overridable members";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.SetupOnlyUsedForOverridableMembers,
        Title,
        Message,
        DiagnosticCategory.Moq,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.SetupOnlyUsedForOverridableMembers}.md");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.InvocationExpression);
    }

    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "Should be fixed. Ignoring for now to avoid additional churn as part of larger refactor.")]
    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        InvocationExpressionSyntax setupInvocation = (InvocationExpressionSyntax)context.Node;

        MoqKnownSymbols knownSymbols = new(context.SemanticModel.Compilation);

        if (setupInvocation.Expression is not MemberAccessExpressionSyntax memberAccessExpression)
        {
            return;
        }

        SymbolInfo memberAccessSymbolInfo = context.SemanticModel.GetSymbolInfo(memberAccessExpression, context.CancellationToken);
        if (memberAccessSymbolInfo.Symbol is null || !context.SemanticModel.IsMoqSetupMethod(knownSymbols, memberAccessSymbolInfo.Symbol, context.CancellationToken))
        {
            return;
        }

        ExpressionSyntax? mockedMemberExpression = setupInvocation.FindMockedMemberExpressionFromSetupMethod();
        if (mockedMemberExpression == null)
        {
            return;
        }

        SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo(mockedMemberExpression, context.CancellationToken);
        ISymbol? symbol = symbolInfo.Symbol;

        if (symbol is null)
        {
            return;
        }

        // Skip if it's part of an interface
        if (symbol.ContainingType.TypeKind == TypeKind.Interface)
        {
            return;
        }

        switch (symbol)
        {
            case IPropertySymbol propertySymbol:
                // Check if the property is Task<T>.Result and skip diagnostic if it is
                if (IsTaskOrValueResultProperty(propertySymbol, knownSymbols))
                {
                    return;
                }

                if (propertySymbol.IsOverridable() || propertySymbol.IsMethodReturnTypeTask())
                {
                    return;
                }

                break;
            case IMethodSymbol methodSymbol:
                if (methodSymbol.IsOverridable() || methodSymbol.IsMethodReturnTypeTask())
                {
                    return;
                }

                break;
        }

        Diagnostic diagnostic = mockedMemberExpression.CreateDiagnostic(Rule);
        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsTaskOrValueResultProperty(IPropertySymbol propertySymbol, MoqKnownSymbols knownSymbols)
    {
        return IsGenericResultProperty(propertySymbol, knownSymbols.Task1)
               || IsGenericResultProperty(propertySymbol, knownSymbols.ValueTask1);
    }

    private static bool IsGenericResultProperty(IPropertySymbol propertySymbol, INamedTypeSymbol? genericType)
    {
        // Check if the property is named "Result"
        if (!string.Equals(propertySymbol.Name, "Result", StringComparison.Ordinal))
        {
            return false;
        }

        return genericType != null &&

               // If Task<T> type cannot be found, we skip it
               SymbolEqualityComparer.Default.Equals(propertySymbol.ContainingType.OriginalDefinition, genericType);
    }
}
