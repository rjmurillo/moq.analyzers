using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers;

/// <summary>
/// MockRepository instances should have Verify() called on them to verify all created mocks.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MockRepositoryVerifyAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = "Moq: MockRepository.Verify() should be called";
    private static readonly LocalizableString Message = "MockRepository '{0}' should have Verify() called";
    private static readonly LocalizableString Description = "MockRepository.Verify() should be called to verify all mocks created through the repository.";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.MockRepositoryVerifyNotCalled,
        Title,
        Message,
        DiagnosticCategory.BestPractice,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description,
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

        context.RegisterOperationAction(
            operationContext => AnalyzeVariableDeclaration(operationContext, knownSymbols),
            OperationKind.VariableDeclarator);
    }

    /// <summary>
    /// Analyzes a variable declaration to find MockRepository instances that may need verification.
    /// </summary>
    private static void AnalyzeVariableDeclaration(OperationAnalysisContext context, MoqKnownSymbols knownSymbols)
    {
        if (context.Operation is not IVariableDeclaratorOperation declarator)
        {
            return;
        }

        // Check if this declares a MockRepository variable
        if (!IsValidMockRepositoryDeclaration(declarator, knownSymbols))
        {
            return;
        }

        // Get the containing member body root.
        IOperation containingMember = GetContainingMemberRoot(declarator);

        // Locals declared inside a lambda or local function within a field or property initializer
        // root at a (Field/Property)InitializerOperation, not a member body. Those initializer scopes
        // are not analyzed for Create/Verify pairing, so bail rather than raise a false positive.
        if (containingMember.Kind is not (OperationKind.MethodBody or OperationKind.ConstructorBody))
        {
            return;
        }

        // Check if there are Create() calls and Verify() calls for this repository in the same scope
        (bool hasCreateCalls, bool hasVerifyCalls) = GetCreateAndVerifyCallsForRepository(declarator.Symbol, containingMember, knownSymbols);

        // Report diagnostic if Create() calls exist but no Verify() calls
        if (hasCreateCalls && !hasVerifyCalls)
        {
            Location diagnosticLocation = GetDiagnosticLocationForVariableDeclarator(declarator);
            Diagnostic diagnostic = diagnosticLocation.CreateDiagnostic(Rule, declarator.Symbol.Name);
            context.ReportDiagnostic(diagnostic);
        }
    }

    /// <summary>
    /// Gets the containing member body root for an operation.
    /// </summary>
    private static IOperation GetContainingMemberRoot(IOperation operation)
    {
        IOperation current = operation;
        while (current.Parent is not null)
        {
            current = current.Parent;
        }

        return current;
    }

    /// <summary>
    /// Gets whether the specified repository has Create() and Verify() calls in the given member.
    /// </summary>
    private static (bool HasCreateCalls, bool HasVerifyCalls) GetCreateAndVerifyCallsForRepository(
        ILocalSymbol repositorySymbol,
        IOperation memberOperation,
        MoqKnownSymbols knownSymbols)
    {
        bool hasCreateCalls = false;
        bool hasVerifyCalls = false;

        foreach (IOperation operation in memberOperation.Descendants())
        {
            if (operation is not IInvocationOperation invocation)
            {
                continue;
            }

            hasCreateCalls = hasCreateCalls || IsCreateCallForRepository(invocation, repositorySymbol, knownSymbols);
            hasVerifyCalls = hasVerifyCalls || IsVerifyCallForRepository(invocation, repositorySymbol, knownSymbols);

            if (hasCreateCalls && hasVerifyCalls)
            {
                break;
            }
        }

        return (hasCreateCalls, hasVerifyCalls);
    }

    /// <summary>
    /// Determines if the invocation is a MockRepository.Create() call for the specified repository.
    /// </summary>
    private static bool IsCreateCallForRepository(
        IInvocationOperation invocation,
        ILocalSymbol repositorySymbol,
        MoqKnownSymbols knownSymbols)
    {
        return IsValidMockRepositoryCreateCall(invocation, knownSymbols) &&
               GetRepositorySymbolFromCreateCall(invocation)?.Equals(repositorySymbol, SymbolEqualityComparer.Default) == true;
    }

    /// <summary>
    /// Determines if the invocation is a MockRepository.Verify() call for the specified repository.
    /// </summary>
    private static bool IsVerifyCallForRepository(
        IInvocationOperation invocation,
        ILocalSymbol repositorySymbol,
        MoqKnownSymbols knownSymbols)
    {
        return IsValidMockRepositoryVerifyCall(invocation, knownSymbols) &&
               GetRepositorySymbolFromVerifyCall(invocation)?.Equals(repositorySymbol, SymbolEqualityComparer.Default) == true;
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

    /// <summary>
    /// Gets the diagnostic location for a variable declarator, targeting the variable identifier.
    /// </summary>
    private static Location GetDiagnosticLocationForVariableDeclarator(IVariableDeclaratorOperation declarator)
    {
        // Try to locate the variable identifier in the syntax tree to report the diagnostic at the correct location.
        // If that fails for any reason, report the diagnostic on the entire declarator.
        if (declarator.Syntax is VariableDeclaratorSyntax declaratorSyntax)
        {
            return declaratorSyntax.Identifier.GetLocation();
        }

        return declarator.Syntax.GetLocation();
    }
}
