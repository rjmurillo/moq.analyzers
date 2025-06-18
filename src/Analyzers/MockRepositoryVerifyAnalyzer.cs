using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers;

/// <summary>
/// MockRepository instances should have Verify() called on them to verify all created mocks.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MockRepositoryVerifyAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = "Moq: MockRepository.Verify() should be called";
    private static readonly LocalizableString Message = "MockRepository.Verify() should be called to verify all mocks created through the repository";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.MockRepositoryVerifyNotCalled,
        Title,
        Message,
        DiagnosticCategory.Moq,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.MockRepositoryVerifyNotCalled}.md");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(RegisterCompilationStartAction);
    }

    /// <summary>
    /// Registers the operation action for MockRepository analysis if Moq is referenced.
    /// </summary>
    private static void RegisterCompilationStartAction(CompilationStartAnalysisContext context)
    {
        MoqKnownSymbols knownSymbols = new(context.Compilation);

        // Ensure Moq is referenced in the compilation
        if (!knownSymbols.IsMockReferenced())
        {
            return;
        }

        // Check that MockRepository type is available
        if (knownSymbols.MockRepository is null)
        {
            return;
        }

        context.RegisterOperationBlockStartAction(operationBlockContext =>
        {
            operationBlockContext.RegisterOperationAction(
                operationContext => AnalyzeOperation(operationContext, knownSymbols),
                OperationKind.Block);
        });
    }

    /// <summary>
    /// Analyzes a block of operations to find MockRepository instances that need Verify() called.
    /// </summary>
    private static void AnalyzeOperation(OperationAnalysisContext context, MoqKnownSymbols knownSymbols)
    {
        if (context.Operation is not IBlockOperation blockOperation)
        {
            return;
        }

        HashSet<ILocalSymbol> mockRepositoryVariables = new HashSet<ILocalSymbol>(SymbolEqualityComparer.Default);
        HashSet<ILocalSymbol> verifiedRepositories = new HashSet<ILocalSymbol>(SymbolEqualityComparer.Default);

        // First pass: Find MockRepository variable declarations and Verify() calls
        foreach (IOperation operation in GetAllOperations(blockOperation))
        {
            switch (operation)
            {
                case IVariableDeclaratorOperation declarator
                    when IsValidMockRepositoryDeclaration(declarator, knownSymbols):
                    if (declarator.Symbol is ILocalSymbol localSymbol)
                    {
                        mockRepositoryVariables.Add(localSymbol);
                    }

                    break;

                case IInvocationOperation invocation
                    when IsValidMockRepositoryVerifyCall(invocation, knownSymbols):
                    if (GetRepositorySymbolFromVerifyCall(invocation) is ILocalSymbol verifiedSymbol)
                    {
                        verifiedRepositories.Add(verifiedSymbol);
                    }

                    break;
            }
        }

        // Second pass: Check if any MockRepository variables have Create() calls but no Verify()
        HashSet<ILocalSymbol> repositoriesWithCreateCalls = new HashSet<ILocalSymbol>(SymbolEqualityComparer.Default);

        foreach (IOperation operation in GetAllOperations(blockOperation))
        {
            if (operation is IInvocationOperation invocation &&
                IsValidMockRepositoryCreateCall(invocation, knownSymbols) &&
                GetRepositorySymbolFromCreateCall(invocation) is ILocalSymbol repoSymbol)
            {
                repositoriesWithCreateCalls.Add(repoSymbol);
            }
        }

        // Report diagnostics for repositories with Create() calls but no Verify()
        foreach (ILocalSymbol repoSymbol in repositoriesWithCreateCalls)
        {
            if (!verifiedRepositories.Contains(repoSymbol))
            {
                Diagnostic diagnostic = context.Operation.CreateDiagnostic(Rule, repoSymbol.Name);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    /// <summary>
    /// Gets all operations from a block operation recursively.
    /// </summary>
    private static IEnumerable<IOperation> GetAllOperations(IBlockOperation blockOperation)
    {
        foreach (var operation in blockOperation.Operations)
        {
            yield return operation;

            foreach (var child in GetAllChildOperations(operation))
            {
                yield return child;
            }
        }
    }

    /// <summary>
    /// Gets all child operations recursively.
    /// </summary>
    private static IEnumerable<IOperation> GetAllChildOperations(IOperation operation)
    {
        foreach (var child in operation.ChildOperations)
        {
            yield return child;

            foreach (var grandchild in GetAllChildOperations(child))
            {
                yield return grandchild;
            }
        }
    }

    /// <summary>
    /// Determines if the operation is a valid MockRepository variable declaration.
    /// </summary>
    private static bool IsValidMockRepositoryDeclaration(IVariableDeclaratorOperation declarator, MoqKnownSymbols knownSymbols)
    {
        return declarator.Initializer?.Value is IObjectCreationOperation creation &&
               creation.Type?.IsInstanceOf(knownSymbols.MockRepository) == true;
    }

    /// <summary>
    /// Determines if the operation is a valid MockRepository.Verify() call.
    /// </summary>
    private static bool IsValidMockRepositoryVerifyCall(IInvocationOperation invocation, MoqKnownSymbols knownSymbols)
    {
        return invocation.TargetMethod.IsInstanceOf(knownSymbols.MockRepositoryVerify);
    }

    /// <summary>
    /// Determines if the operation is a valid MockRepository.Create() call.
    /// </summary>
    private static bool IsValidMockRepositoryCreateCall(IInvocationOperation invocation, MoqKnownSymbols knownSymbols)
    {
        return invocation.TargetMethod.IsInstanceOf(knownSymbols.MockRepositoryCreate);
    }

    /// <summary>
    /// Gets the repository symbol from a Verify() call.
    /// </summary>
    private static ILocalSymbol? GetRepositorySymbolFromVerifyCall(IInvocationOperation invocation)
    {
        return invocation.Instance switch
        {
            ILocalReferenceOperation localRef => localRef.Local,
            _ => null,
        };
    }

    /// <summary>
    /// Gets the repository symbol from a Create() call.
    /// </summary>
    private static ILocalSymbol? GetRepositorySymbolFromCreateCall(IInvocationOperation invocation)
    {
        return invocation.Instance switch
        {
            ILocalReferenceOperation localRef => localRef.Local,
            _ => null,
        };
    }
}
