using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers;

/// <summary>
/// Setup sequence methods should not be mixed with standard setup on the same member.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SequenceSetupAfterStandardSetupAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = "Moq: Sequence setup conflicts with standard setup";
    private static readonly LocalizableString Message = "SetupSequence and Setup should not be used on the same member";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.SequenceSetupAfterStandardSetup,
        Title,
        Message,
        DiagnosticCategory.Moq,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.SequenceSetupAfterStandardSetup}.md");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

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

        if (!IsMoqSequenceMethod(targetMethod, knownSymbols))
        {
            return;
        }

        CheckForConflictingSetups(context, invocationOperation, knownSymbols);
    }

    private static bool IsMoqSequenceMethod(IMethodSymbol method, MoqKnownSymbols knownSymbols)
    {
        return knownSymbols.Mock1SetupSequence.Any(s => method.IsInstanceOf(s));
    }

    private static void CheckForConflictingSetups(OperationAnalysisContext context, IInvocationOperation sequenceOperation, MoqKnownSymbols knownSymbols)
    {
        // This is a simplified implementation that warns about potential conflicts
        // A more complete implementation would track actual member access patterns
        // across multiple setup calls in the same method/block
        if (HasPotentialStandardSetupConflict(sequenceOperation, knownSymbols))
        {
            Diagnostic diagnostic = sequenceOperation.Syntax.CreateDiagnostic(Rule);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool HasPotentialStandardSetupConflict(IInvocationOperation sequenceOperation, MoqKnownSymbols knownSymbols)
    {
        // Look for other Setup calls in the same containing block
        // This is a simplified heuristic - a full implementation would need more sophisticated analysis
        IOperation? containingBlock = sequenceOperation.Parent;
        while (containingBlock != null && containingBlock is not IBlockOperation)
        {
            containingBlock = containingBlock.Parent;
        }

        if (containingBlock is IBlockOperation block)
        {
            return HasStandardSetupInBlock(block, knownSymbols);
        }

        return false;
    }

    private static bool HasStandardSetupInBlock(IBlockOperation block, MoqKnownSymbols knownSymbols)
    {
        foreach (IOperation operation in block.Operations)
        {
            if (ContainsStandardSetup(operation, knownSymbols))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsStandardSetup(IOperation operation, MoqKnownSymbols knownSymbols)
    {
        if (operation is IInvocationOperation invocation)
        {
            return knownSymbols.Mock1Setup.Any(s => invocation.TargetMethod.IsInstanceOf(s));
        }

        foreach (IOperation child in operation.ChildOperations)
        {
            if (ContainsStandardSetup(child, knownSymbols))
            {
                return true;
            }
        }

        return false;
    }
}
