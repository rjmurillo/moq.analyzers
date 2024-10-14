using System.Runtime.CompilerServices;

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

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "AV1500:Member or local function contains too many statements", Justification = "Tracked in https://github.com/rjmurillo/moq.analyzers/issues/90")]
    private static void Analyze(SyntaxNodeAnalysisContext context)
    {
        InvocationExpressionSyntax setupInvocation = (InvocationExpressionSyntax)context.Node;

        if (setupInvocation.Expression is not MemberAccessExpressionSyntax memberAccessExpression
            || !context.SemanticModel.IsMoqSetupMethod(memberAccessExpression, context.CancellationToken))
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
                if (IsTaskResultProperty(propertySymbol, context))
                {
                    return;
                }

                if (IsMethodOverridable(propertySymbol))
                {
                    return;
                }

                if (propertySymbol.IsMethodReturnTypeTask())
                {
                    return;
                }

                break;
            case IMethodSymbol methodSymbol:
                if (IsMethodOverridable(methodSymbol) || methodSymbol.IsMethodReturnTypeTask())
                {
                    return;
                }

                break;
        }

        Diagnostic diagnostic = mockedMemberExpression.CreateDiagnostic(Rule);
        context.ReportDiagnostic(diagnostic);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsMethodOverridable(ISymbol methodSymbol)
    {
        return !methodSymbol.IsSealed && (methodSymbol.IsVirtual || methodSymbol.IsAbstract || methodSymbol.IsOverride);
    }

    private static bool IsTaskResultProperty(IPropertySymbol propertySymbol, SyntaxNodeAnalysisContext context)
    {
        // Check if the property is named "Result"
        if (!string.Equals(propertySymbol.Name, "Result", StringComparison.Ordinal))
        {
            return false;
        }

        // Check if the containing type is Task<T>
        INamedTypeSymbol? taskOfTType = context.SemanticModel.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");

        if (taskOfTType == null)
        {
            return false; // If Task<T> type cannot be found, we skip it
        }

        return SymbolEqualityComparer.Default.Equals(propertySymbol.ContainingType, taskOfTType);
    }
}
