using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers;

/// <summary>
/// Sealed classes cannot be mocked.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NoSealedClassMocksAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = "Moq: Sealed class mocked";
    private static readonly LocalizableString Message = "Sealed classes cannot be mocked";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.SealedClassCannotBeMocked,
        Title,
        Message,
        DiagnosticCategory.Moq,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.SealedClassCannotBeMocked}.md");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(RegisterCompilationStartAction);
    }

    private static void RegisterCompilationStartAction(CompilationStartAnalysisContext context)
    {
        MoqKnownSymbols knownSymbols = new(context.Compilation);

        // Ensure Moq is referenced in the compilation
        if (!knownSymbols.IsMockReferenced())
        {
            return;
        }

        // Check that Mock<T> type is available
        if (knownSymbols.Mock1 is null)
        {
            return;
        }

        context.RegisterOperationAction(
            operationAnalysisContext => Analyze(operationAnalysisContext, knownSymbols),
            OperationKind.ObjectCreation);
    }

    private static void Analyze(OperationAnalysisContext context, MoqKnownSymbols knownSymbols)
    {
        if (context.Operation is not IObjectCreationOperation creation)
        {
            return;
        }

        // Check if this is creating a Mock<T> instance
        if (creation.Type is null ||
            creation.Constructor is null ||
            !creation.Type.IsInstanceOf(knownSymbols.Mock1))
        {
            return;
        }

        // Get the type arguments of Mock<T>
        if (creation.Type is not INamedTypeSymbol namedType ||
            namedType.TypeArguments.Length != 1)
        {
            return;
        }

        ITypeSymbol mockedType = namedType.TypeArguments[0];

        // Check if the mocked type is sealed (but allow delegates)
        // Note: All delegates in .NET are sealed by default, but they can still be mocked by Moq
        if (mockedType.IsSealed && mockedType.TypeKind != TypeKind.Delegate)
        {
            // Try to locate the type argument in the syntax tree to report the diagnostic at the correct location.
            // If that fails for any reason, report the diagnostic on the operation itself.
            TypeSyntax? typeArgument = context.Operation.Syntax
                .DescendantNodes()
                .OfType<GenericNameSyntax>()
                .FirstOrDefault()?
                .TypeArgumentList?
                .Arguments
                .FirstOrDefault();

            Location location = typeArgument?.GetLocation() ?? creation.Syntax.GetLocation();
            context.ReportDiagnostic(location.CreateDiagnostic(Rule));
        }
    }
}
