using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers;

/// <summary>
/// MockRepository should have Verify() called to verify all created mocks.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MockRepositoryVerifyMissingAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = "Moq: MockRepository.Verify() should be called";
    private static readonly LocalizableString Message = "MockRepository should have Verify() called to verify created mocks";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.MockRepositoryVerifyMissing,
        Title,
        Message,
        DiagnosticCategory.Moq,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.MockRepositoryVerifyMissing}.md");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterOperationAction(AnalyzeVariableDeclaration, OperationKind.VariableDeclaration);
    }

    private static void AnalyzeVariableDeclaration(OperationAnalysisContext context)
    {
        if (context.Operation is not IVariableDeclarationOperation variableDeclaration)
        {
            return;
        }

        SemanticModel? semanticModel = variableDeclaration.SemanticModel;
        if (semanticModel == null)
        {
            return;
        }

        MoqKnownSymbols knownSymbols = new(semanticModel.Compilation);

        foreach (IVariableDeclaratorOperation declarator in variableDeclaration.Declarators)
        {
            if (declarator.Initializer?.Value is IObjectCreationOperation objectCreation &&
                IsMockRepositoryCreation(objectCreation, knownSymbols))
            {
                CheckForMissingVerify(context, declarator, knownSymbols);
            }
        }
    }

    private static bool IsMockRepositoryCreation(IObjectCreationOperation objectCreation, MoqKnownSymbols knownSymbols)
    {
        return knownSymbols.MockRepository != null &&
               SymbolEqualityComparer.Default.Equals(objectCreation.Type?.OriginalDefinition, knownSymbols.MockRepository);
    }

    private static void CheckForMissingVerify(OperationAnalysisContext context, IVariableDeclaratorOperation declarator, MoqKnownSymbols knownSymbols)
    {
        // Find the containing method or block
        IOperation? containingMethod = declarator.Parent;
        while (containingMethod is not null and not IMethodBodyOperation and not IBlockOperation)
        {
            containingMethod = containingMethod.Parent;
        }

        if (containingMethod == null)
        {
            return;
        }

        // Check if there's a Verify() call on this repository variable in the same scope
        if (declarator.Symbol?.Name != null && !HasRepositoryVerifyCall(containingMethod, declarator.Symbol.Name, knownSymbols))
        {
            Diagnostic diagnostic = declarator.Syntax.CreateDiagnostic(Rule);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool HasRepositoryVerifyCall(IOperation operation, string repositoryVariableName, MoqKnownSymbols knownSymbols)
    {
        if (operation is IInvocationOperation invocation &&
            string.Equals(invocation.TargetMethod.Name, "Verify", StringComparison.Ordinal))
        {
            // Check if the invocation is on our repository variable
            if (invocation.Instance is IFieldReferenceOperation fieldRef &&
                string.Equals(fieldRef.Member.Name, repositoryVariableName, StringComparison.Ordinal))
            {
                return true;
            }

            if (invocation.Instance is ILocalReferenceOperation localRef &&
                string.Equals(localRef.Local.Name, repositoryVariableName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        foreach (IOperation child in operation.ChildOperations)
        {
            if (HasRepositoryVerifyCall(child, repositoryVariableName, knownSymbols))
            {
                return true;
            }
        }

        return false;
    }
}
