using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers;

/// <summary>
/// Setup should be used only for overridable members.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SetupShouldBeUsedOnlyForOverridableMembersAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = "Moq: Invalid setup parameter";
    private static readonly LocalizableString Message = "Setup should be used only for overridable members, but '{0}' is not overridable";
    private static readonly LocalizableString Description = "Setup should be used only for overridable members.";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.SetupOnlyUsedForOverridableMembers,
        Title,
        Message,
        DiagnosticCategory.Usage,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: Description,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.SetupOnlyUsedForOverridableMembers}.md");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(RegisterCompilationStartAction);
    }

    private static void RegisterCompilationStartAction(CompilationStartAnalysisContext context)
    {
        MoqKnownSymbols knownSymbols = new(context.Compilation);

        if (!knownSymbols.IsMockReferenced())
        {
            return;
        }

        context.RegisterOperationAction(
            operationAnalysisContext => AnalyzeInvocation(operationAnalysisContext, knownSymbols),
            OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, MoqKnownSymbols knownSymbols)
    {
        if (context.Operation is not IInvocationOperation invocationOperation)
        {
            return;
        }

        if (!IsSetupOnNonOverridableMember(invocationOperation, knownSymbols, out ISymbol? mockedMemberSymbol))
        {
            return;
        }

        Diagnostic diagnostic = invocationOperation.Syntax.CreateDiagnostic(Rule, mockedMemberSymbol.ToDisplayString());
        context.ReportDiagnostic(diagnostic);
    }

    /// <summary>
    /// Determines whether the invocation is a Moq Setup call targeting a non-overridable member.
    /// </summary>
    /// <param name="invocationOperation">The invocation operation to analyze.</param>
    /// <param name="knownSymbols">A <see cref="MoqKnownSymbols"/> instance for resolving well-known types.</param>
    /// <param name="mockedMemberSymbol">When this method returns <see langword="true"/>, contains the non-overridable member symbol.</param>
    /// <returns><see langword="true"/> if the invocation targets a non-overridable member; otherwise <see langword="false"/>.</returns>
    private static bool IsSetupOnNonOverridableMember(
        IInvocationOperation invocationOperation,
        MoqKnownSymbols knownSymbols,
        [NotNullWhen(true)] out ISymbol? mockedMemberSymbol)
    {
        mockedMemberSymbol = null;
        IMethodSymbol targetMethod = invocationOperation.TargetMethod;

        // Check if the invoked method is a Moq Setup method.
        if (!targetMethod.IsMoqSetupMethod(knownSymbols) && !targetMethod.IsMoqEventSetupMethod(knownSymbols))
        {
            return false;
        }

        // Attempt to locate the member reference from the Setup expression argument.
        ISymbol? candidate = MoqVerificationHelpers.TryGetMockedMemberSymbol(invocationOperation);
        if (candidate is null
            || candidate.ContainingType?.TypeKind == TypeKind.Interface
            || IsPropertyOrMethod(candidate, knownSymbols))
        {
            return false;
        }

        mockedMemberSymbol = candidate;
        return true;
    }

    /// <summary>
    /// Determines whether a property or method is either
    /// <see cref="ValueTask"/>, <see cref="ValueTask{TResult}"/>, <see cref="Task"/>, or <see cref="Task{TResult}"/>
    /// - OR -
    /// if the <paramref name="mockedMemberSymbol"/> is overridable.
    /// </summary>
    /// <param name="mockedMemberSymbol">The mocked member symbol.</param>
    /// <param name="knownSymbols">A <see cref="MoqKnownSymbols"/> instance for resolving well-known types.</param>
    /// <returns>
    /// Returns <see langword="true"/> when the diagnostic should not be triggered; otherwise <see langword="false" />.
    /// </returns>
    private static bool IsPropertyOrMethod(ISymbol mockedMemberSymbol, MoqKnownSymbols knownSymbols)
    {
        switch (mockedMemberSymbol)
        {
            case IPropertySymbol propertySymbol:
                // Check if the property is Task<T>.Result and skip diagnostic if it is
                if (propertySymbol.IsTaskOrValueResultProperty(knownSymbols))
                {
                    return true;
                }

                if (propertySymbol.IsOverridable())
                {
                    return true;
                }

                break;

            case IMethodSymbol methodSymbol:
                if (methodSymbol.IsOverridable())
                {
                    return true;
                }

                break;

            default:
                // If it's not a property or method, it's not overridable
                return false;
        }

        return false;
    }
}
