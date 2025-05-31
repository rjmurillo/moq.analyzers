using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers;

/// <summary>
/// Protected members should be mocked using Protected() setup instead of direct access.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ProtectedMockMemberReferenceShouldUseProtectedSetupAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = "Moq: Protected members should use Protected() setup";
    private static readonly LocalizableString Message = "Protected members should be mocked using Protected() setup instead of direct access";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.ProtectedMockMemberReferenceShouldUseProtectedSetup,
        Title,
        Message,
        DiagnosticCategory.Moq,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.ProtectedMockMemberReferenceShouldUseProtectedSetup}.md");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(RegisterCompilationStartAction);
    }

    private static void RegisterCompilationStartAction(CompilationStartAnalysisContext context)
    {
        MoqKnownSymbols knownSymbols = new(context.Compilation);

        // Ensure Moq is referenced in the compilation
        if (!knownSymbols.IsMockReferenced())
        {
            return;
        }

        context.RegisterOperationAction(
            operationAnalysisContext => AnalyzeInvocation(operationAnalysisContext),
            OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        if (context.Operation is not IInvocationOperation invocationOperation)
        {
            return;
        }

        // Check if this is a Setup method call
        if (!IsSetupMethodCall(invocationOperation))
        {
            return;
        }

        // Look for lambda expressions in the Setup method arguments
        foreach (IArgumentOperation argument in invocationOperation.Arguments)
        {
            if (argument.Value is IAnonymousFunctionOperation lambda)
            {
                AnalyzeLambdaForProtectedMemberAccess(context, lambda);
            }
        }
    }

    private static bool IsSetupMethodCall(IInvocationOperation invocationOperation)
    {
        IMethodSymbol targetMethod = invocationOperation.TargetMethod;

        // Check if it's a Setup method on Mock<T>
        return string.Equals(targetMethod.Name, "Setup", StringComparison.Ordinal) &&
               targetMethod.ContainingType != null &&
               targetMethod.ContainingType.IsGenericType &&
               string.Equals(targetMethod.ContainingType.ConstructedFrom?.ToDisplayString(), "Moq.Mock<T>", StringComparison.Ordinal);
    }

    private static void AnalyzeLambdaForProtectedMemberAccess(OperationAnalysisContext context, IAnonymousFunctionOperation lambda)
    {
        foreach (IOperation descendant in lambda.Body.DescendantsAndSelf())
        {
            if (descendant is IMemberReferenceOperation memberRef)
            {
                // Check if the member being referenced is protected
                if (IsProtectedMember(memberRef.Member))
                {
                    context.ReportDiagnostic(memberRef.Syntax.GetLocation().CreateDiagnostic(Rule));
                }
            }
            else if (descendant is IInvocationOperation invocation &&
                     invocation.TargetMethod.DeclaredAccessibility == Accessibility.Protected)
            {
                context.ReportDiagnostic(invocation.Syntax.GetLocation().CreateDiagnostic(Rule));
            }
        }
    }

    private static bool IsProtectedMember(ISymbol member)
    {
        return member.DeclaredAccessibility == Accessibility.Protected;
    }
}
