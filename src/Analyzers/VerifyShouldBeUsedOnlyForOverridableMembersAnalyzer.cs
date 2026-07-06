using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers;

/// <summary>
/// Verify should be used only for overridable members.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class VerifyShouldBeUsedOnlyForOverridableMembersAnalyzer : MoqDiagnosticAnalyzerBase
{
    private static readonly LocalizableString Title = "Moq: Invalid verify parameter";
    private static readonly LocalizableString Message = "Verify should be used only for overridable members, but '{0}' is not overridable";
    private static readonly LocalizableString Description = "Verify should be used only for overridable members.";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.VerifyOnlyUsedForOverridableMembers,
        Title,
        Message,
        DiagnosticCategory.Correctness,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: Description,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.VerifyOnlyUsedForOverridableMembers}.md");

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

        IMethodSymbol targetMethod = invocationOperation.TargetMethod;

        if (!ShouldAnalyzeMethod(targetMethod, knownSymbols))
        {
            return;
        }

        if (targetMethod.IsInstanceOf(knownSymbols.Mock1VerifySet))
        {
            IArgumentOperation? setterArgument = MoqVerificationHelpers.GetArgumentForParameterOrdinal(invocationOperation, 0);
            IAnonymousFunctionOperation? lambda = setterArgument is not null
                ? MoqVerificationHelpers.ExtractLambdaFromArgument(setterArgument.Value)
                : null;
            ISymbol? verifySetMemberSymbol = lambda is not null
                ? MoqVerificationHelpers.ExtractPropertyFromVerifySetLambda(lambda)
                : null;

            if (verifySetMemberSymbol == null || IsVerifySetMemberAllowed(verifySetMemberSymbol))
            {
                return;
            }

            ReportDiagnostic(context, invocationOperation, verifySetMemberSymbol);
            return;
        }

        if (!MoqVerificationHelpers.TryGetNonOverridableMockedMember(invocationOperation, knownSymbols, out ISymbol? mockedMemberSymbol))
        {
            return;
        }

        ReportDiagnostic(context, invocationOperation, mockedMemberSymbol);
    }

    private static bool ShouldAnalyzeMethod(IMethodSymbol targetMethod, MoqKnownSymbols knownSymbols)
    {
        // Check if the invoked method is a Moq Verification method
        if (!targetMethod.IsMoqVerificationMethod(knownSymbols))
        {
            return false;
        }

        // VerifyNoOtherCalls doesn't take a lambda argument, so skip it
        return !targetMethod.IsInstanceOf(knownSymbols.Mock1VerifyNoOtherCalls);
    }

    private static bool IsVerifySetMemberAllowed(ISymbol mockedMemberSymbol)
    {
        return mockedMemberSymbol is IPropertySymbol propertySymbol && propertySymbol.IsOverridable();
    }

    private static void ReportDiagnostic(OperationAnalysisContext context, IInvocationOperation invocationOperation, ISymbol mockedMemberSymbol)
    {
        Diagnostic diagnostic = invocationOperation.Syntax.CreateDiagnostic(Rule, mockedMemberSymbol.Name);
        context.ReportDiagnostic(diagnostic);
    }
}
