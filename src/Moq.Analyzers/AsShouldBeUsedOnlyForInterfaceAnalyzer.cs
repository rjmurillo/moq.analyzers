using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers;

/// <summary>
/// Mock.As() should take interfaces only.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AsShouldBeUsedOnlyForInterfaceAnalyzer : DiagnosticAnalyzer
{
    internal const string RuleId = "Moq1300";
    private const string Title = "Moq: Invalid As type parameter";
    private const string Message = "Mock.As() should take interfaces only";

    private static readonly DiagnosticDescriptor Rule = new(
        RuleId,
        Title,
        Message,
        DiagnosticCategory.Moq,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/{RuleId}.md");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(static context =>
        {
            // Ensure Moq is referenced in the compilation
            ImmutableArray<INamedTypeSymbol> mockTypes = context.Compilation.GetMoqMock();
            if (mockTypes.IsEmpty)
            {
                return;
            }

            // Look for the Mock.As() method and provide it to Analyze to avoid looking it up multiple times.
            ImmutableArray<IMethodSymbol> asMethods = mockTypes
                .SelectMany(mockType => mockType.GetMembers("As"))
                .OfType<IMethodSymbol>()
                .Where(method => method.IsGenericMethod)
                .ToImmutableArray();
            if (asMethods.IsEmpty)
            {
                return;
            }

            context.RegisterOperationAction(context => Analyze(context, asMethods), OperationKind.Invocation);
        });
    }

    private static void Analyze(OperationAnalysisContext context, ImmutableArray<IMethodSymbol> wellKnownAsMethods)
    {
        if (context.Operation is not IInvocationOperation invocationOperation)
        {
            return;
        }

        IMethodSymbol targetMethod = invocationOperation.TargetMethod;
        if (!targetMethod.IsInstanceOf(wellKnownAsMethods))
        {
            return;
        }

        ImmutableArray<ITypeSymbol> typeArguments = targetMethod.TypeArguments;
        if (typeArguments.Length != 1)
        {
            return;
        }

        if (typeArguments[0] is ITypeSymbol { TypeKind: not TypeKind.Interface })
        {
            // Try to locate the type argument in the syntax tree to report the diagnostic at the correct location.
            // If that fails for any reason, report the diagnostic on the operation itself.
            NameSyntax? memberName = context.Operation.Syntax.DescendantNodes().OfType<MemberAccessExpressionSyntax>().Select(mae => mae.Name).DefaultIfNotSingle();
            Location location = memberName?.GetLocation() ?? invocationOperation.Syntax.GetLocation();

            context.ReportDiagnostic(Diagnostic.Create(Rule, location));
        }
    }
}
