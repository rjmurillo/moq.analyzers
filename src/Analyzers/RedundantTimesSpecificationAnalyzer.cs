using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers;

/// <summary>
/// Analyzer to detect redundant Times specifications in Moq verification calls.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class RedundantTimesSpecificationAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = "Moq: Redundant Times specification";
    private static readonly LocalizableString Message = "Redundant Times.AtLeastOnce() specification can be removed as it is the default for Verify calls";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.RedundantTimesSpecification,
        Title,
        Message,
        DiagnosticCategory.Moq,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.RedundantTimesSpecification}.md");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

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

        if (!ShouldAnalyzeMethod(targetMethod, knownSymbols))
        {
            return;
        }

        // Check if the method has a Times parameter
        if (!HasTimesParameter(invocationOperation, knownSymbols, out IArgumentOperation? timesArgument))
        {
            return;
        }

        // Check if the Times argument is Times.AtLeastOnce()
        if (IsRedundantTimesAtLeastOnce(timesArgument, knownSymbols))
        {
            ReportDiagnostic(context, timesArgument);
        }
    }

    private static bool ShouldAnalyzeMethod(IMethodSymbol targetMethod, MoqKnownSymbols knownSymbols)
    {
        // Check if the invoked method is a Moq Verification method
        return targetMethod.IsMoqVerificationMethod(knownSymbols);
    }

    private static bool HasTimesParameter(IInvocationOperation invocationOperation, MoqKnownSymbols knownSymbols, [NotNullWhen(true)] out IArgumentOperation? timesArgument)
    {
        timesArgument = null;

        // Look for a Times parameter in the arguments
        foreach (IArgumentOperation argument in invocationOperation.Arguments)
        {
            if (argument.Parameter?.Type != null &&
                SymbolEqualityComparer.Default.Equals(argument.Parameter.Type, knownSymbols.Times))
            {
                timesArgument = argument;
                return true;
            }
        }

        return false;
    }

    private static bool IsRedundantTimesAtLeastOnce(IArgumentOperation timesArgument, MoqKnownSymbols knownSymbols)
    {
        // Check if the argument is a call to Times.AtLeastOnce()
        if (timesArgument.Value is IInvocationOperation timesInvocation)
        {
            return timesInvocation.TargetMethod.IsInstanceOf(knownSymbols.TimesAtLeastOnce);
        }

        return false;
    }

    private static void ReportDiagnostic(OperationAnalysisContext context, IArgumentOperation timesArgument)
    {
        Diagnostic diagnostic = timesArgument.Syntax.CreateDiagnostic(Rule);
        context.ReportDiagnostic(diagnostic);
    }
}
