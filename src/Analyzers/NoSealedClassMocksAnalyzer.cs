using System.Diagnostics.CodeAnalysis;
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
        if (!IsValidMockCreation(context.Operation, knownSymbols, out IObjectCreationOperation? creation))
        {
            return;
        }

        if (!TryGetMockedType(creation, out ITypeSymbol? mockedType))
        {
            return;
        }

        if (ShouldReportDiagnostic(mockedType))
        {
            Location location = GetDiagnosticLocation(context.Operation, creation);
            context.ReportDiagnostic(location.CreateDiagnostic(Rule));
        }
    }

    private static bool IsValidMockCreation(IOperation operation, MoqKnownSymbols knownSymbols, [NotNullWhen(true)] out IObjectCreationOperation? creation)
    {
        creation = operation as IObjectCreationOperation;
        return creation is not null &&
               creation.Type is not null &&
               creation.Constructor is not null &&
               creation.Type.IsInstanceOf(knownSymbols.Mock1);
    }

    private static bool TryGetMockedType(IObjectCreationOperation creation, [NotNullWhen(true)] out ITypeSymbol? mockedType)
    {
        mockedType = null;

        if (creation.Type is not INamedTypeSymbol namedType || namedType.TypeArguments.Length != 1)
        {
            return false;
        }

        mockedType = namedType.TypeArguments[0];
        return true;
    }

    private static bool ShouldReportDiagnostic(ITypeSymbol mockedType)
    {
        // Check if the mocked type is sealed (but allow delegates)
        // Note: All delegates in .NET are sealed by default, but they can still be mocked by Moq
        return mockedType.IsSealed && mockedType.TypeKind != TypeKind.Delegate;
    }

    private static Location GetDiagnosticLocation(IOperation operation, IObjectCreationOperation creation)
    {
        // Try to locate the type argument in the syntax tree to report the diagnostic at the correct location.
        // If that fails for any reason, report the diagnostic on the operation itself.
        TypeSyntax? typeArgument = operation.Syntax
            .DescendantNodes()
            .OfType<GenericNameSyntax>()
            .FirstOrDefault()?
            .TypeArgumentList?
            .Arguments
            .FirstOrDefault();

        return typeArgument?.GetLocation() ?? creation.Syntax.GetLocation();
    }
}
