using System.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers;

/// <summary>
/// SetupGet/SetupSet/SetupProperty should be used for properties, not for methods.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NoMethodsInPropertySetupAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = "Moq: Property setup used for a method";
    private static readonly LocalizableString Message = "SetupGet/SetupSet/SetupProperty should be used for properties, not for methods like '{0}'";
    private static readonly LocalizableString Description = "SetupGet/SetupSet/SetupProperty should be used for properties, not for methods.";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.PropertySetupUsedForMethod,
        Title,
        Message,
        DiagnosticCategory.Correctness,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.PropertySetupUsedForMethod}.md");

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

        ImmutableArray<IMethodSymbol> propertySetupMethods = ImmutableArray.CreateRange([
            ..knownSymbols.Mock1SetupGet,
            ..knownSymbols.Mock1SetupSet,
            ..knownSymbols.Mock1SetupProperty]);

        if (propertySetupMethods.IsEmpty)
        {
            return;
        }

        context.RegisterOperationAction(
            operationAnalysisContext => Analyze(operationAnalysisContext, propertySetupMethods),
            OperationKind.Invocation);
    }

    private static void Analyze(OperationAnalysisContext context, ImmutableArray<IMethodSymbol> propertySetupMethods)
    {
        Debug.Assert(context.Operation is IInvocationOperation, "Expected IInvocationOperation");

        if (context.Operation is not IInvocationOperation invocationOperation)
        {
            return;
        }

        IMethodSymbol targetMethod = invocationOperation.TargetMethod;
        if (!targetMethod.IsInstanceOf(propertySetupMethods))
        {
            return;
        }

        if (invocationOperation.Arguments.Length == 0)
        {
            return;
        }

        // Extract the lambda argument by parameter ordinal (0), not source position,
        // so named-argument reordering (e.g., SetupProperty(initialValue: ..., propertyExpression: ...)) is handled.
        IArgumentOperation? lambdaArgument = null;
        foreach (IArgumentOperation argument in invocationOperation.Arguments)
        {
            if (argument.Parameter?.Ordinal == 0)
            {
                lambdaArgument = argument;
                break;
            }
        }

        if (lambdaArgument == null)
        {
            return;
        }

        IAnonymousFunctionOperation? lambdaOperation =
            MoqVerificationHelpers.ExtractLambdaFromArgument(lambdaArgument.Value);

        if (lambdaOperation == null)
        {
            return;
        }

        // If the lambda body resolves to a method symbol, the user incorrectly used a property
        // setup method (SetupGet/SetupSet/SetupProperty) for a method call.
        ISymbol? mockedMemberSymbol = lambdaOperation.Body.GetReferencedMemberSymbolFromLambda();
        if (mockedMemberSymbol is not IMethodSymbol)
        {
            return;
        }

        // Prefer the syntax of the mocked member reference for a precise diagnostic location.
        // Fall back to the invocation syntax to ensure the diagnostic is always reported.
        SyntaxNode diagnosticTarget = lambdaOperation.Body.GetReferencedMemberSyntaxFromLambda()
            ?? invocationOperation.Syntax;

        Diagnostic diagnostic = diagnosticTarget.CreateDiagnostic(Rule, mockedMemberSymbol.Name);
        context.ReportDiagnostic(diagnostic);
    }
}
