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
    private static readonly LocalizableString Message = "Setup member '{0}' should be overridable";
    private static readonly LocalizableString Description = "Setup should be used only for overridable members. Static, sealed, or non-virtual members cannot be mocked.";

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
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "Should be fixed. Ignoring for now to avoid additional churn as part of larger refactor.")]
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

        // 1. Check if the invoked method is a Moq Setup method
        if (!targetMethod.IsMoqSetupMethod(knownSymbols))
        {
            return;
        }

        // 2. Attempt to locate the member reference from the Setup expression argument.
        //    Typically, Moq setup calls have a single lambda argument like x => x.SomeMember.
        //    We'll extract that member reference or invocation to see whether it is overridable.
        ISymbol? mockedMemberSymbol = TryGetMockedMemberSymbol(invocationOperation);
        if (mockedMemberSymbol == null)
        {
            return;
        }

        // 3. Skip if the symbol is part of an interface, those are always "overridable".
        if (mockedMemberSymbol.ContainingType?.TypeKind == TypeKind.Interface)
        {
            return;
        }

        // 4. Check if symbol is a property or method, and if it is overridable or is returning a Task (which Moq allows).
        if (IsPropertyOrMethod(mockedMemberSymbol, knownSymbols))
        {
            return;
        }

        // 5. If we reach here, the member is neither overridable nor allowed by Moq
        //    So we report the diagnostic.
        //
        // NOTE: The location is on the invocationOperation, which is fairly broad
        Diagnostic diagnostic = invocationOperation.Syntax.CreateDiagnostic(Rule, mockedMemberSymbol.Name);
        context.ReportDiagnostic(diagnostic);
    }

    /// <summary>
    /// Determines whether a property or method is either
    /// <see cref="ValueTask"/>, <see cref="ValueTask{TResult}"/>, <see cref="Task"/>, or <see cref="Task{TResult}"/>
    /// - OR -
    /// if the <paramref name="mockedMemberSymbol"/> is overridable.
    /// </summary>
    /// <param name="mockedMemberSymbol">The mocked member symbol.</param>
    /// <param name="knownSymbols">The known symbols.</param>
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
                // If it's not a property or method, we do not issue a diagnostic
                return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to resolve the symbol representing the member (property or method)
    /// being referenced in the Setup(...) call. Returns null if it cannot be determined.
    /// </summary>
    private static ISymbol? TryGetMockedMemberSymbol(IInvocationOperation moqSetupInvocation)
    {
        // Usually the first argument to a Moq Setup(...) is a lambda expression like x => x.Property
        // or x => x.Method(...). We can look at moqSetupInvocation.Arguments[0].Value to see this.
        //
        // In almost all Moq setups, the first argument is the expression (lambda) to be analyzed.
        if (moqSetupInvocation.Arguments.Length == 0)
        {
            return null;
        }

        IOperation argumentOperation = moqSetupInvocation.Arguments[0].Value;

        // 1) Unwrap conversions (Roslyn often wraps lambdas in IConversionOperation).
        argumentOperation = argumentOperation.WalkDownImplicitConversion();

        if (argumentOperation is IAnonymousFunctionOperation lambdaOperation)
        {
            // If it's a simple lambda of the form x => x.SomeMember,
            // the body often ends up as an IPropertyReferenceOperation or IInvocationOperation.
            return lambdaOperation.Body.GetReferencedMemberSymbolFromLambda();
        }

        // Sometimes it might be a delegate creation or something else. Handle other patterns if needed.
        return null;
    }
}
