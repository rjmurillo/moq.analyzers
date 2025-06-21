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

        // 1. Check if the invoked method is a Moq Verification method
        if (!targetMethod.IsMoqVerificationMethod(knownSymbols))
        {
            return;
        }

        // 2. VerifyNoOtherCalls doesn't take a lambda argument, so skip it
        if (targetMethod.IsInstanceOf(knownSymbols.Mock1VerifyNoOtherCalls))
        {
            return;
        }

        // 3. Attempt to locate the member reference from the Verify expression argument.
        //    For VerifySet, we need to extract the property being set from the Action<T> lambda.
        ISymbol? mockedMemberSymbol = TryGetMockedMemberSymbol(invocationOperation, knownSymbols);
        if (mockedMemberSymbol == null)
        {
            return;
        }

        // 4. Skip if the symbol is part of an interface, those are always "overridable".
        if (mockedMemberSymbol.ContainingType?.TypeKind == TypeKind.Interface)
        {
            return;
        }

        // 5. Check if symbol is a property or method, and if it is overridable or is returning a Task (which Moq allows).
        // Special handling for VerifySet: must check property overridability for set accessor
        if (targetMethod.IsInstanceOf(knownSymbols.Mock1VerifySet))
        {
            if (mockedMemberSymbol is IPropertySymbol propertySymbol && propertySymbol.IsOverridable())
            {
                return;
            }

            // If not overridable, fall through to report diagnostic
        }
        else
        {
            if (IsAllowedMockMember(mockedMemberSymbol, knownSymbols))
            {
                return;
            }
        }

        // 6. If we reach here, the member is neither overridable nor allowed by Moq
        //    So we report the diagnostic.
        //
        // NOTE: The location is on the invocationOperation, which is fairly broad
        // For VerifySet, try to report the diagnostic on the property being set for precise span
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

    /// <summary>
    /// Attempts to resolve the symbol representing the member (property or method)
    /// being referenced in the Verify(...) or VerifySet(...) call. Returns null if it cannot be determined.
    /// </summary>
    private static ISymbol? TryGetMockedMemberSymbol(IInvocationOperation moqVerifyInvocation, MoqKnownSymbols knownSymbols)
    {
#pragma warning disable S125 // Sections of code should not be commented out
        // Usually the first argument to a Moq Verify(...) is a lambda expression like x => x.Property
        // or x => x.Method(...). For VerifySet, it's an Action<T> lambda: x => { x.Property = ...; }
#pragma warning restore S125 // Sections of code should not be commented out
        if (moqVerifyInvocation.Arguments.Length == 0)
        {
            return null;
        }

        // The lambda is always at index 0 for all Moq verification methods
        IOperation argumentOperation = moqVerifyInvocation.Arguments[0].Value;
        argumentOperation = argumentOperation.WalkDownImplicitConversion();

        // Handle delegate conversions (e.g., VerifySet(x => { ... }))
        if (argumentOperation is IDelegateCreationOperation delegateCreation &&
            delegateCreation.Target is IAnonymousFunctionOperation lambdaOp)
        {
            argumentOperation = lambdaOp;
        }

        if (argumentOperation is IAnonymousFunctionOperation lambdaOperation)
        {
            // For VerifySet, the lambda body is a block with an assignment statement.
            if (moqVerifyInvocation.TargetMethod.IsInstanceOf(knownSymbols.Mock1VerifySet))
            {
                ImmutableArray<IOperation> bodyOps = lambdaOperation.Body.Operations;
                foreach (IOperation op in bodyOps)
                {
                    if (op is IExpressionStatementOperation exprStmt)
                    {
                        IAssignmentOperation? assignOp = exprStmt.Operation as IAssignmentOperation
                            ?? exprStmt.Operation as ISimpleAssignmentOperation;

                        if (assignOp?.Target is IPropertyReferenceOperation propRef)
                        {
                            return propRef.Property;
                        }
                    }
                }

                return null;
            }

            // For Verify/VerifyGet, use the existing logic
            return lambdaOperation.Body.GetReferencedMemberSymbolFromLambda();
        }

        // Sometimes it might be a delegate creation or something else. Handle other patterns if needed.
        return null;
    }
}
