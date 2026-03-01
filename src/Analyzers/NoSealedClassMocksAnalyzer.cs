using Microsoft.CodeAnalysis.Operations;
using Moq.Analyzers.Common;

namespace Moq.Analyzers;

/// <summary>
/// Sealed classes cannot be mocked.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NoSealedClassMocksAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = "Moq: Sealed class mocked";
    private static readonly LocalizableString Message = "Sealed class '{0}' cannot be mocked";
    private static readonly LocalizableString Description = "Sealed classes cannot be mocked.";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.SealedClassCannotBeMocked,
        Title,
        Message,
        DiagnosticCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description,
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

    /// <summary>
    /// Registers the operation action for object creation and invocation if Moq is referenced and Mock{T} is available.
    /// </summary>
    private static void RegisterCompilationStartAction(CompilationStartAnalysisContext context)
    {
        MoqKnownSymbols knownSymbols = new(context.Compilation);

        // Ensure Moq is referenced in the compilation
        if (!knownSymbols.IsMockReferenced())
        {
            return;
        }

        // Check that Mock{T} type is available
        if (knownSymbols.Mock1 is null)
        {
            return;
        }

        context.RegisterOperationAction(
            operationAnalysisContext => Analyze(operationAnalysisContext, knownSymbols),
            OperationKind.ObjectCreation,
            OperationKind.Invocation);
    }

    /// <summary>
    /// Analyzes object creation and invocation operations to report diagnostics for sealed class mocks.
    /// </summary>
    private static void Analyze(OperationAnalysisContext context, MoqKnownSymbols knownSymbols)
    {
        ITypeSymbol? mockedType = null;
        Location? diagnosticLocation = null;

        // Handle object creation: new Mock{T}()
        if (context.Operation is IObjectCreationOperation creation &&
            MockDetectionHelpers.IsValidMockCreation(creation, knownSymbols, out mockedType))
        {
            diagnosticLocation = MockDetectionHelpers.GetDiagnosticLocation(context.Operation, creation.Syntax);
        }

        // Handle static method invocation: Mock.Of{T}()
        else if (context.Operation is IInvocationOperation invocation &&
                 MockDetectionHelpers.IsValidMockOfInvocation(invocation, knownSymbols, out mockedType))
        {
            diagnosticLocation = MockDetectionHelpers.GetDiagnosticLocation(context.Operation, invocation.Syntax);
        }
        else
        {
            // Operation is neither a Mock object creation nor a Mock.Of invocation that we need to analyze
            return;
        }

        if (mockedType != null && diagnosticLocation != null && ShouldReportDiagnostic(mockedType))
        {
            context.ReportDiagnostic(diagnosticLocation.CreateDiagnostic(Rule, mockedType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
        }
    }

    /// <summary>
    /// Determines whether a diagnostic should be reported for the mocked type based on its characteristics.
    /// </summary>
    /// <param name="mockedType">The type being mocked.</param>
    /// <returns>
    ///   Returns <see langword="true"/> when the mocked type is a sealed *reference* type (including nullable reference
    ///   types), <b>except arrays</b>. Returns <see langword="false"/> for delegates, arrays, and for all value types (structs / enums), including
    ///   <see cref="Nullable{T}"/>.
    /// </returns>
    private static bool ShouldReportDiagnostic(ITypeSymbol mockedType)
    {
        // Exclude delegates (all delegates are sealed, but Moq allows mocking them)
        if (mockedType.TypeKind == TypeKind.Delegate)
        {
            return false;
        }

        // Exclude nullable value types (Nullable{T})
        if (mockedType.OriginalDefinition?.SpecialType == SpecialType.System_Nullable_T)
        {
            return false;
        }

        // Exclude structs and enums
        if (mockedType.TypeKind == TypeKind.Struct || mockedType.TypeKind == TypeKind.Enum)
        {
            return false;
        }

        // For reference types, report if sealed
        return mockedType.IsSealed;
    }
}
