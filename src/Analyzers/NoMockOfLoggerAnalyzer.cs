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
    private static readonly LocalizableString Message = "ILogger should not be mocked; use NullLogger<T>.Instance or FakeLogger from Microsoft.Extensions.Diagnostics.Testing instead";
    private static readonly LocalizableString Description = "Mocking ILogger is unnecessary and fragile. Use NullLogger<T>.Instance for tests that ignore logging, or FakeLogger from Microsoft.Extensions.Diagnostics.Testing for tests that verify log output.";

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

        // Handle static method invocation: Mock.Of{T}() or MockRepository.Create{T}()
        else if (context.Operation is IInvocationOperation invocation &&
                 IsValidMockInvocation(invocation, knownSymbols, out mockedType))
        {
            diagnosticLocation = MockDetectionHelpers.GetDiagnosticLocation(context.Operation, invocation.Syntax);
        }
        else
        {
            // Operation is neither a Mock object creation nor a relevant mock invocation
            return;
        }

        if (mockedType != null && diagnosticLocation != null && IsLoggerType(mockedType, knownSymbols))
        {
            context.ReportDiagnostic(diagnosticLocation.CreateDiagnostic(Rule));
        }
    }

    /// <summary>
    /// Determines if the operation is a valid Mock.Of{T}() or MockRepository.Create{T}() invocation
    /// and extracts the mocked type.
    /// </summary>
    private static bool IsValidMockInvocation(IInvocationOperation invocation, MoqKnownSymbols knownSymbols, [NotNullWhen(true)] out ITypeSymbol? mockedType)
    {
        mockedType = null;

        bool isMockOf = MockDetectionHelpers.IsValidMockOfMethod(invocation.TargetMethod, knownSymbols);
        bool isMockRepositoryCreate = !isMockOf && invocation.TargetMethod.IsInstanceOf(knownSymbols.MockRepositoryCreate);

        if (!isMockOf && !isMockRepositoryCreate)
        {
            return false;
        }

        // Both Mock.Of{T}() and MockRepository.Create{T}() use a single type argument
        if (invocation.TargetMethod.TypeArguments.Length == 1)
        {
            mockedType = invocation.TargetMethod.TypeArguments[0];
            return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether the mocked type is ILogger or ILogger{T} using symbol-based comparison.
    /// </summary>
    private static bool IsLoggerType(ITypeSymbol mockedType, MoqKnownSymbols knownSymbols)
    {
        if (mockedType is not INamedTypeSymbol namedType)
        {
            return false;
        }

        INamedTypeSymbol originalDefinition = namedType.OriginalDefinition;

        if (knownSymbols.ILogger is not null &&
            SymbolEqualityComparer.Default.Equals(originalDefinition, knownSymbols.ILogger))
        {
            return true;
        }

        if (knownSymbols.ILogger1 is not null &&
            SymbolEqualityComparer.Default.Equals(originalDefinition, knownSymbols.ILogger1))
        {
            return true;
        }

        return false;
    }
}
