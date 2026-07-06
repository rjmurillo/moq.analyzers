using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers;

/// <summary>
/// Mock should explicitly specify a behavior and not rely on the default.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SetExplicitMockBehaviorAnalyzer : MockBehaviorDiagnosticAnalyzerBase
{
    private static readonly LocalizableString Title = "Moq: Explicitly choose a mock behavior";
    private static readonly LocalizableString Message = "Explicitly choose a mocking behavior for {0} instead of relying on the default (Loose) behavior";
    private static readonly LocalizableString Description = "Explicitly choose a mocking behavior instead of relying on the default (Loose) behavior.";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.SetExplicitMockBehavior,
        Title,
        Message,
        DiagnosticCategory.BestPractice,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.SetExplicitMockBehavior}.md");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    /// <inheritdoc />
    private protected override DiagnosticDescriptor DiagnosticRule => Rule;

    /// <inheritdoc />
    private protected override void AnalyzeMockBehaviorArgument(
        OperationAnalysisContext context,
        IMethodSymbol target,
        IArgumentOperation? mockArgument,
        MoqKnownSymbols knownSymbols,
        string mockedTypeName)
    {
        System.Diagnostics.Debug.Assert(knownSymbols.MockBehavior is not null, "Base registration requires the MockBehavior symbol.");

        // Is the behavior set via a default value?
        if (mockArgument?.ArgumentKind == ArgumentKind.DefaultValue
            && MockBehaviorConstantValues.ConstantValueEquals(mockArgument.Value.WalkDownConversion().ConstantValue, knownSymbols.MockBehaviorDefault))
        {
            TryReportMockBehaviorDiagnostic(context, target, knownSymbols, Rule, DiagnosticEditProperties.EditType.Insert, mockedTypeName);
        }

        // NOTE: This logic can't handle indirection (e.g. var x = MockBehavior.Default; new Mock(x);). We can't use the constant value either,
        // as Loose and Default share the same enum value: `1`. Being more accurate I believe requires data flow analysis.
        //
        // The operation specifies a MockBehavior; is it MockBehavior.Default?
        if (mockArgument?.DescendantsAndSelf().OfType<IFieldReferenceOperation>().Any(argument => argument.Member.IsInstanceOf(knownSymbols.MockBehaviorDefault)) == true)
        {
            TryReportMockBehaviorDiagnostic(context, target, knownSymbols, Rule, DiagnosticEditProperties.EditType.Replace, mockedTypeName);
        }
    }
}
