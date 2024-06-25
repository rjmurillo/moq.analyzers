using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers;

internal static class CompilationExtensions
{
    public static ImmutableArray<INamedTypeSymbol> GetTypesByMetadataNames(this Compilation compilation, ReadOnlySpan<string> metadataNames)
    {
        ImmutableArray<INamedTypeSymbol>.Builder builder = ImmutableArray.CreateBuilder<INamedTypeSymbol>(metadataNames.Length);

        foreach (string metadataName in metadataNames)
        {
            INamedTypeSymbol? type = compilation.GetTypeByMetadataName(metadataName);
            if (type is not null)
            {
                builder.Add(type);
            }
        }

        return builder.ToImmutable();
    }
}

/// <summary>
/// Mock.As() should take interfaces only.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AsShouldBeUsedOnlyForInterfaceAnalyzer2 : DiagnosticAnalyzer
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
            ImmutableArray<INamedTypeSymbol> mockTypes = context.Compilation.GetTypesByMetadataNames([WellKnownTypeNames.MoqMock, WellKnownTypeNames.MoqMock1]);

            if (mockTypes.IsEmpty)
            {
                return;
            }

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

        if (!wellKnownAsMethods.Any(asMethod => asMethod.Equals(targetMethod.OriginalDefinition, SymbolEqualityComparer.Default)))
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
            NameSyntax? memberName = context.Operation.Syntax.DescendantNodes().OfType<MemberAccessExpressionSyntax>().Select(mae => mae.Name).FirstOrDefault();

            Location location = memberName?.GetLocation() ?? invocationOperation.Syntax.GetLocation();

            context.ReportDiagnostic(Diagnostic.Create(Rule, location));
        }
    }
}

//internal static class SyntaxNodeExtensions
//{
//    //public static bool TryGetSyntaxByName(this SyntaxNode parentNode, SemanticModel semanticModel, string name, out SyntaxNode? node)
//    //{
//    //    parentNode.DescendantNodesAndSelf()
//    //        //.OfType<TypeSyntax>()
//    //        .Where(n => semanticModel.GetDeclaredSymbol()
//    //}

//    public static IEnumerable<SyntaxNode> GetSyntaxNodesThatMatchSymbol(this SyntaxNode node, SemanticModel semanticModel, ISymbol symbol)
//    {
//        foreach (SyntaxNode descendant in node.DescendantNodesAndSelf())
//        {
//            if (semanticModel.GetSymbolInfo(descendant).Symbol?.Equals(symbol, SymbolEqualityComparer.Default) ?? false)
//            {
//                yield return descendant;
//            }
//        }
//    }
//}
