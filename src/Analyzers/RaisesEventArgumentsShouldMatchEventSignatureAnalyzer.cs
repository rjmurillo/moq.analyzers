#pragma warning disable CS0436 // Type conflicts with imported type

namespace Moq.Analyzers;

/// <summary>
/// Raises event arguments should match the event delegate signature.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class RaisesEventArgumentsShouldMatchEventSignatureAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = "Moq: Raises event arguments should match event signature";
    private static readonly LocalizableString Message = "Raises event arguments should match the event delegate signature";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.RaisesEventArgumentsShouldMatchEventSignature,
        Title,
        Message,
        DiagnosticCategory.Moq,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.RaisesEventArgumentsShouldMatchEventSignature}.md");

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
        InvocationExpressionSyntax invocation = (InvocationExpressionSyntax)context.Node;

        // Check if this is a Raises method call
        if (!IsRaisesMethodCall(invocation))
        {
            return;
        }

        if (!TryGetRaisesMethodArguments(invocation, context.SemanticModel, out ArgumentSyntax[] eventArguments, out ITypeSymbol[] expectedParameterTypes))
        {
            return;
        }

        EventSyntaxExtensions.ValidateEventArgumentTypes(context, eventArguments, expectedParameterTypes, invocation, Rule);
    }

    private static bool IsRaisesMethodCall(InvocationExpressionSyntax invocation)
    {
        // Check if the method being called is named "Raises"
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess ||
!string.Equals(memberAccess.Name.Identifier.ValueText, "Raises", StringComparison.Ordinal))
        {
            return false;
        }

        // Additional validation could be added here to ensure it's a Moq Raises method
        // For now, we'll rely on the method name
        return true;
    }

    private static bool TryGetRaisesMethodArguments(InvocationExpressionSyntax invocation, SemanticModel semanticModel, out ArgumentSyntax[] eventArguments, out ITypeSymbol[] expectedParameterTypes)
    {
        return EventSyntaxExtensions.TryGetEventMethodArguments(
            invocation,
            semanticModel,
            out eventArguments,
            out expectedParameterTypes,
            (sm, selector) =>
            {
                bool success = EventSyntaxExtensions.TryGetEventTypeFromLambdaSelector(sm, selector, out ITypeSymbol? eventType);
                return (success, eventType);
            });
    }
}
