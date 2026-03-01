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
        DiagnosticCategory.Usage,
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

        // The lambda argument to SetupGet/SetupSet/SetupProperty contains the mocked member access.
        // If the lambda body is an invocation (method call), that is invalid for property setup.
        InvocationExpressionSyntax? mockedMethodCall =
            (invocationOperation.Syntax as InvocationExpressionSyntax).FindMockedMethodInvocationFromSetupMethod();

        if (mockedMethodCall == null)
        {
            return;
        }

        SemanticModel semanticModel = invocationOperation.SemanticModel!;
        ISymbol? mockedMethodSymbol = semanticModel.GetSymbolInfo(mockedMethodCall, context.CancellationToken).Symbol;

        if (mockedMethodSymbol == null)
        {
            return;
        }

        Diagnostic diagnostic = mockedMethodCall.CreateDiagnostic(Rule, mockedMethodSymbol.Name);
        context.ReportDiagnostic(diagnostic);
    }
}
