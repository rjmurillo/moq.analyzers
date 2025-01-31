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
    private static readonly LocalizableString Message = "Setup should be used only for overridable members";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.SetupOnlyUsedForOverridableMembers,
        Title,
        Message,
        DiagnosticCategory.Moq,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.SetupOnlyUsedForOverridableMembers}.md");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        // Instead of registering a syntax node action on InvocationExpression,
        // we now register an operation action on IInvocationOperation.
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "Should be fixed. Ignoring for now to avoid additional churn as part of larger refactor.")]
    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        IInvocationOperation invocationOperation = (IInvocationOperation)context.Operation;
        SemanticModel? semanticModel = invocationOperation.SemanticModel;

        if (semanticModel == null)
        {
            return;
        }

        MoqKnownSymbols knownSymbols = new(semanticModel.Compilation);
        IMethodSymbol targetMethod = invocationOperation.TargetMethod;

        // 1. Check if the invoked method is a Moq Setup method
        if (!semanticModel.IsMoqSetupMethod(knownSymbols, targetMethod, context.CancellationToken))
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
        switch (mockedMemberSymbol)
        {
            case IPropertySymbol propertySymbol:
                // If the property is Task<T>.Result, skip diagnostic
                if (IsTaskResultProperty(propertySymbol, knownSymbols))
                {
                    return;
                }

                if (propertySymbol.IsOverridable() || propertySymbol.IsMethodReturnTypeTask())
                {
                    return;
                }

                break;

            case IMethodSymbol methodSymbol:
                if (methodSymbol.IsOverridable() || methodSymbol.IsMethodReturnTypeTask())
                {
                    return;
                }

                break;

            default:
                // If it's not a property or method, we do not issue a diagnostic
                return;
        }

        // 5. If we reach here, the member is neither overridable nor allowed by Moq
        //    So we report the diagnostic.
        //
        // NOTE: The location is on the invocationOperation, which is fairly broad
        Diagnostic diagnostic = invocationOperation.Syntax.CreateDiagnostic(Rule);
        context.ReportDiagnostic(diagnostic);
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
        argumentOperation = argumentOperation.UnwrapConversion();

        if (argumentOperation is IAnonymousFunctionOperation lambdaOperation)
        {
            // If it's a simple lambda of the form x => x.SomeMember,
            // the body often ends up as an IPropertyReferenceOperation or IInvocationOperation.
            return lambdaOperation.Body.TryGetReferencedMemberSymbolFromLambda();
        }

        // Sometimes it might be a delegate creation or something else. Handle other patterns if needed.
        return null;
    }

    /// <summary>
    /// Checks if a property is the 'Result' property on <see cref="Task{TResult}"/>.
    /// </summary>
    private static bool IsTaskResultProperty(IPropertySymbol propertySymbol, MoqKnownSymbols knownSymbols)
    {
        // Check if the property is named "Result"
        if (!string.Equals(propertySymbol.Name, "Result", StringComparison.Ordinal))
        {
            return false;
        }

        // Check if the containing type is Task<T>
        INamedTypeSymbol? taskOfTType = knownSymbols.Task1;

        if (taskOfTType == null)
        {
            return false; // If Task<T> type cannot be found, we skip it
        }

        return SymbolEqualityComparer.Default.Equals(propertySymbol.ContainingType, taskOfTType);
    }
}
