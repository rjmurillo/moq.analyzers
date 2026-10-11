using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers;

/// <summary>
/// Mock should explicitly specify Strict behavior.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SetStrictMockBehaviorAnalyzer : MockBehaviorDiagnosticAnalyzerBase
{
    private static readonly LocalizableString Title = "Moq: Set MockBehavior to Strict";
    private static readonly LocalizableString Message = "Explicitly set the Strict mocking behavior for '{0}'";
    private static readonly LocalizableString Description = "Explicitly set the Strict mocking behavior.";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.SetStrictMockBehavior,
        Title,
        Message,
        DiagnosticCategory.BestPractice,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: Description,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.SetStrictMockBehavior}.md");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    /// <inheritdoc />
    private protected override DiagnosticDescriptor DiagnosticRule => Rule;

    /// <inheritdoc />
    private protected override void AnalyzeMockBehaviorArgument(
        OperationAnalysisContext context,
        IMethodSymbol target,
        IParameterSymbol mockParameter,
        IArgumentOperation? mockArgument,
        MoqKnownSymbols knownSymbols)
    {
        System.Diagnostics.Debug.Assert(knownSymbols.MockBehavior is not null, "Base registration requires the MockBehavior symbol.");

        // Is the behavior set via a default value?
        if (mockArgument?.ArgumentKind == ArgumentKind.DefaultValue
            && MockBehaviorConstantValues.ConstantValueEquals(mockArgument.Value.WalkDownConversion().ConstantValue, knownSymbols.MockBehaviorDefault)
            && TryReportMockBehaviorDiagnostic(context, mockParameter, Rule, DiagnosticEditProperties.EditType.Insert))
        {
            return;
        }

        // NOTE: This logic can't handle indirection (e.g. var x = MockBehavior.Default; new Mock(x);)
        //
        // The operation specifies a MockBehavior; is it MockBehavior.Strict?
        if (mockArgument is null
            || (!MockBehaviorConstantValues.ConstantValueEquals(mockArgument.Value.WalkDownConversion().ConstantValue, knownSymbols.MockBehaviorStrict)
                && !ContainsFieldReference(mockArgument, knownSymbols.MockBehaviorStrict)))
        {
            TryReportMockBehaviorDiagnostic(context, mockParameter, Rule, DiagnosticEditProperties.EditType.Replace);
        }
    }
}
