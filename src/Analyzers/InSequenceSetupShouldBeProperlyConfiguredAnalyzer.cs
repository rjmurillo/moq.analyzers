using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers;

/// <summary>
/// InSequence setup should be properly configured.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class InSequenceSetupShouldBeProperlyConfiguredAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = "Moq: InSequence setup should be properly configured";
    private static readonly LocalizableString Message = "InSequence setup should use valid MockSequence parameter";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.InSequenceSetupShouldBeProperlyConfigured,
        Title,
        Message,
        DiagnosticCategory.Moq,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.InSequenceSetupShouldBeProperlyConfigured}.md");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "Simple analyzer implementation")]
    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        if (context.Operation is not IInvocationOperation invocationOperation)
        {
            return;
        }

        SemanticModel? semanticModel = invocationOperation.SemanticModel;

        if (semanticModel == null)
        {
            return;
        }

        MoqKnownSymbols knownSymbols = new(semanticModel.Compilation);
        IMethodSymbol targetMethod = invocationOperation.TargetMethod;

        // Check if the invoked method is a Moq InSequence method
        if (!targetMethod.IsMoqInSequenceMethod(knownSymbols))
        {
            return;
        }

        // Validate that InSequence has proper parameters
        if (!HasValidMockSequenceParameter(invocationOperation, knownSymbols))
        {
            Diagnostic diagnostic = invocationOperation.Syntax.CreateDiagnostic(Rule);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool HasValidMockSequenceParameter(IInvocationOperation invocationOperation, MoqKnownSymbols knownSymbols)
    {
        // InSequence should have exactly one parameter of type MockSequence
        if (invocationOperation.Arguments.Length != 1)
        {
            return false;
        }

        IArgumentOperation argument = invocationOperation.Arguments[0];
        if (argument.Value.Type == null)
        {
            return false;
        }

        // Check if the parameter type is MockSequence
        return SymbolEqualityComparer.Default.Equals(argument.Value.Type, knownSymbols.MockSequence);
    }
}
