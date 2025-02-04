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
        Diagnostic diagnostic = invocationOperation.Syntax.CreateDiagnostic(Rule);
        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsPropertyOrMethod(ISymbol mockedMemberSymbol, MoqKnownSymbols knownSymbols)
    {
        switch (mockedMemberSymbol)
        {
            case IPropertySymbol propertySymbol:
                // Check if the property is Task<T>.Result and skip diagnostic if it is
                if (IsTaskOrValueResultProperty(propertySymbol, knownSymbols))
                {
                    return true;
                }

                if (propertySymbol.IsOverridable() || propertySymbol.IsMethodReturnTypeTask())
                {
                    return true;
                }

                break;

            case IMethodSymbol methodSymbol:
                if (methodSymbol.IsOverridable() || methodSymbol.IsMethodReturnTypeTask())
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

    private static bool IsTaskOrValueResultProperty(IPropertySymbol propertySymbol, MoqKnownSymbols knownSymbols)
    {
        return IsGenericResultProperty(propertySymbol, knownSymbols.Task1)
               || IsGenericResultProperty(propertySymbol, knownSymbols.ValueTask1);
    }

    /// <summary>
    /// Checks if a property is the 'Result' property on <see cref="Task{TResult}"/> or <see cref="ValueTask{TResult}"/>.
    /// </summary>
    private static bool IsGenericResultProperty(IPropertySymbol propertySymbol, INamedTypeSymbol? genericType)
    {
        // Check if the property is named "Result"
        if (!string.Equals(propertySymbol.Name, "Result", StringComparison.Ordinal))
        {
            return false;
        }

        return genericType != null &&

               // If Task<T> type cannot be found, we skip it
               SymbolEqualityComparer.Default.Equals(propertySymbol.ContainingType.OriginalDefinition, genericType);
    }
}
