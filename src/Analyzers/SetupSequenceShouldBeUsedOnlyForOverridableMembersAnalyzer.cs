using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers;

/// <summary>
/// SetupSequence should be used only for overridable members.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SetupSequenceShouldBeUsedOnlyForOverridableMembersAnalyzer : MoqDiagnosticAnalyzerBase
{
    private static readonly LocalizableString Title = "Moq: Invalid SetupSequence parameter";
    private static readonly LocalizableString Message = "SetupSequence should be used only for overridable members, but '{0}' is not overridable";
    private static readonly LocalizableString Description = "SetupSequence should be used only for overridable members.";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.SetupSequenceOnlyUsedForOverridableMembers,
        Title,
        Message,
        DiagnosticCategory.Correctness,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        Description,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.SetupSequenceOnlyUsedForOverridableMembers}.md");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    private protected override void RegisterCompilationActions(CompilationStartAnalysisContext context, MoqKnownSymbols knownSymbols)
    {
        context.RegisterOperationAction(
            operationContext => AnalyzeInvocation(operationContext, knownSymbols),
            OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, MoqKnownSymbols knownSymbols)
    {
        if (context.Operation is not IInvocationOperation invocationOperation)
        {
            return;
        }

        if (!invocationOperation.TargetMethod.IsMoqSetupSequenceMethod(knownSymbols))
        {
            return;
        }

        ISymbol? mockedMemberSymbol = MoqVerificationHelpers.TryGetMockedMemberSymbol(invocationOperation);
        if (mockedMemberSymbol is null
            || mockedMemberSymbol.IsOverridableOrAllowedMockMember(knownSymbols))
        {
            return;
        }

        // Use the specific member syntax for a more precise diagnostic location when available
        SyntaxNode? memberSyntax = MoqVerificationHelpers.TryGetMockedMemberSyntax(invocationOperation);
        Location diagnosticLocation = memberSyntax?.GetLocation() ?? invocationOperation.Syntax.GetLocation();

        Diagnostic diagnostic = diagnosticLocation.CreateDiagnostic(Rule, mockedMemberSymbol.ToDisplayString());
        context.ReportDiagnostic(diagnostic);
    }
}
