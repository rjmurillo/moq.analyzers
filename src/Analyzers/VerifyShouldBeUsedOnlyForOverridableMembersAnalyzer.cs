using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers;

/// <summary>
/// Verify should be used only for overridable members.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class VerifyShouldBeUsedOnlyForOverridableMembersAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = "Moq: Invalid verify parameter";
    private static readonly LocalizableString Message = "Verify should be used only for overridable members";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.VerifyOnlyUsedForOverridableMembers,
        Title,
        Message,
        DiagnosticCategory.Moq,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.VerifyOnlyUsedForOverridableMembers}.md");

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

        ISymbol? mockedMemberSymbol =
            targetMethod.IsInstanceOf(knownSymbols.Mock1VerifySet)
                ? MoqVerificationHelpers.ExtractPropertyFromVerifySetLambda(
                    MoqVerificationHelpers.ExtractLambdaFromArgument(invocationOperation.Arguments[0].Value)!)
                : MoqVerificationHelpers.TryGetMockedMemberSymbol(invocationOperation);

        if (mockedMemberSymbol == null || IsInterfaceMember(mockedMemberSymbol))
        {
            return;
        }

        if (IsMemberAllowedForVerification(mockedMemberSymbol, targetMethod, knownSymbols))
        {
            return;
        }

        ReportDiagnostic(context, invocationOperation);
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

    private static bool IsInterfaceMember(ISymbol mockedMemberSymbol)
    {
        return mockedMemberSymbol.ContainingType?.TypeKind == TypeKind.Interface;
    }

    private static bool IsMemberAllowedForVerification(ISymbol mockedMemberSymbol, IMethodSymbol targetMethod, MoqKnownSymbols knownSymbols)
    {
        // Special handling for VerifySet: must check property overridability for set accessor
        if (targetMethod.IsInstanceOf(knownSymbols.Mock1VerifySet))
        {
            return mockedMemberSymbol is IPropertySymbol propertySymbol && propertySymbol.IsOverridable();
        }

        return IsAllowedMockMember(mockedMemberSymbol, knownSymbols);
    }

    private static void ReportDiagnostic(OperationAnalysisContext context, IInvocationOperation invocationOperation)
    {
        Location diagnosticLocation = invocationOperation.Syntax.GetLocation();
        Diagnostic diagnostic = DiagnosticExtensions.CreateDiagnostic(invocationOperation.Syntax, Rule, diagnosticLocation);
        context.ReportDiagnostic(diagnostic);
    }

    /// <summary>
    /// Determines whether a member can be mocked.
    /// </summary>
    /// <param name="mockedMemberSymbol">The mocked member symbol.</param>
    /// <param name="knownSymbols">The known symbols.</param>
    /// <returns>
    /// Returns <see langword="true"/> when the diagnostic should not be triggered; otherwise <see langword="false" />.
    /// </returns>
    private static bool IsAllowedMockMember(ISymbol mockedMemberSymbol, MoqKnownSymbols knownSymbols)
    {
        switch (mockedMemberSymbol)
        {
            case IPropertySymbol propertySymbol:
                return propertySymbol.IsOverridable() || propertySymbol.IsTaskOrValueResultProperty(knownSymbols);

            case IMethodSymbol methodSymbol:
                return methodSymbol.IsOverridable();

            default:
                // If it's not a property or method, it can't be mocked. This includes fields and events.
                return false;
        }
    }
}
