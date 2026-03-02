using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Moq.Analyzers.Common;

namespace Moq.Analyzers;

/// <summary>
/// ILogger should not be mocked. Use NullLogger or FakeLogger instead.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NoMockOfLoggerAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = "Moq: ILogger mocked";
    private static readonly LocalizableString Message = "ILogger should not be mocked; use {0} or FakeLogger from Microsoft.Extensions.Diagnostics.Testing instead";
    private static readonly LocalizableString Description = "Mocking ILogger is unnecessary and fragile. Use NullLogger.Instance (for ILogger) or NullLogger<T>.Instance (for ILogger<T>) for tests that ignore logging, or FakeLogger from Microsoft.Extensions.Diagnostics.Testing for tests that verify log output.";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.LoggerShouldNotBeMocked,
        Title,
        Message,
        DiagnosticCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.LoggerShouldNotBeMocked}.md");

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
    /// Registers the operation action for object creation and invocation if Moq is referenced,
    /// Mock{T} is available, and at least one ILogger type is resolvable.
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

        // Bail out early if neither ILogger nor ILogger{T} are referenced
        if (knownSymbols.ILogger is null && knownSymbols.ILogger1 is null)
        {
            return;
        }

        context.RegisterOperationAction(
            operationAnalysisContext => Analyze(operationAnalysisContext, knownSymbols),
            OperationKind.ObjectCreation,
            OperationKind.Invocation);
    }

    /// <summary>
    /// Analyzes object creation and invocation operations to report diagnostics for ILogger mocks.
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

        // Handle mock invocation: Mock.Of{T}() or MockRepository.Create{T}()
        else if (context.Operation is IInvocationOperation invocation &&
                 MockDetectionHelpers.IsValidMockInvocation(invocation, knownSymbols, out mockedType))
        {
            diagnosticLocation = MockDetectionHelpers.GetDiagnosticLocation(context.Operation, invocation.Syntax);
        }
        else
        {
            // Operation is neither a Mock object creation nor a relevant mock invocation
            return;
        }

        if (mockedType != null && diagnosticLocation != null && TryGetNullLoggerAlternative(mockedType, knownSymbols, out string? nullLoggerSuggestion))
        {
            context.ReportDiagnostic(diagnosticLocation.CreateDiagnostic(Rule, nullLoggerSuggestion));
        }
    }

    /// <summary>
    /// Determines whether the mocked type is ILogger or ILogger{T} using symbol-based comparison,
    /// and provides the appropriate NullLogger alternative.
    /// </summary>
    /// <param name="mockedType">The type being mocked.</param>
    /// <param name="knownSymbols">Well-known Moq and logging symbols from the compilation.</param>
    /// <param name="nullLoggerSuggestion">
    /// When this method returns <see langword="true"/>, contains the recommended NullLogger usage:
    /// <c>NullLogger.Instance</c> for non-generic ILogger, or
    /// <c>NullLogger&lt;T&gt;.Instance</c> for ILogger&lt;T&gt;.
    /// </param>
    /// <returns><see langword="true"/> if the mocked type is an ILogger variant; otherwise, <see langword="false"/>.</returns>
    private static bool TryGetNullLoggerAlternative(
        ITypeSymbol mockedType,
        MoqKnownSymbols knownSymbols,
        [NotNullWhen(true)] out string? nullLoggerSuggestion)
    {
        nullLoggerSuggestion = null;

        if (mockedType is not INamedTypeSymbol namedType)
        {
            return false;
        }

        INamedTypeSymbol originalDefinition = namedType.OriginalDefinition;

        if (knownSymbols.ILogger is not null &&
            SymbolEqualityComparer.Default.Equals(originalDefinition, knownSymbols.ILogger))
        {
            nullLoggerSuggestion = "NullLogger.Instance";
            return true;
        }

        if (knownSymbols.ILogger1 is not null &&
            SymbolEqualityComparer.Default.Equals(originalDefinition, knownSymbols.ILogger1))
        {
            nullLoggerSuggestion = "NullLogger<T>.Instance";
            return true;
        }

        return false;
    }
}
