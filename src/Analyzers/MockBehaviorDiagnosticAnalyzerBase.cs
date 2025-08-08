using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers;

/// <summary>
/// Serves as a base class for diagnostic analyzers that analyze mock behavior in Moq.
/// </summary>
/// <remarks>
/// This abstract class provides common functionality for analyzing Moq's MockBehavior, such as registering
/// compilation start actions and defining the core analysis logic to be implemented by derived classes.
/// </remarks>
public abstract class MockBehaviorDiagnosticAnalyzerBase : DiagnosticAnalyzer
{
    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(RegisterCompilationStartAction);
    }

    internal abstract void AnalyzeCore(OperationAnalysisContext context, IMethodSymbol target, ImmutableArray<IArgumentOperation> arguments, MoqKnownSymbols knownSymbols);

    /// <summary>
    /// Attempts to report a diagnostic for a MockBehavior parameter issue.
    /// </summary>
    /// <param name="context">The operation analysis context.</param>
    /// <param name="method">The method to check for MockBehavior parameter.</param>
    /// <param name="knownSymbols">The known Moq symbols.</param>
    /// <param name="rule">The diagnostic rule to report.</param>
    /// <param name="editType">The type of edit for the code fix.</param>
    /// <returns>True if a diagnostic was reported; otherwise, false.</returns>
    internal bool TryReportMockBehaviorDiagnostic(
        OperationAnalysisContext context,
        IMethodSymbol method,
        MoqKnownSymbols knownSymbols,
        DiagnosticDescriptor rule,
        DiagnosticEditProperties.EditType editType)
    {
        // NOTE: This condition is impractical to test in isolation as it represents
        // a scenario where a method that should have MockBehavior parameters doesn't.
        // Testing this would require creating inconsistent symbol information that
        // doesn't reflect real compilation scenarios.
        if (!method.TryGetParameterOfType(knownSymbols.MockBehavior!, out IParameterSymbol? parameterMatch, cancellationToken: context.CancellationToken))
        {
            return false;
        }

        ImmutableDictionary<string, string?> properties = new DiagnosticEditProperties
        {
            TypeOfEdit = editType,
            EditPosition = parameterMatch.Ordinal,
        }.ToImmutableDictionary();

        context.ReportDiagnostic(context.Operation.CreateDiagnostic(rule, properties));
        return true;
    }

    /// <summary>
    /// Attempts to handle missing MockBehavior parameter by checking for overloads that accept it.
    /// </summary>
    /// <param name="context">The operation analysis context.</param>
    /// <param name="mockParameter">The MockBehavior parameter (should be null to trigger overload check).</param>
    /// <param name="target">The target method to check for overloads.</param>
    /// <param name="knownSymbols">The known Moq symbols.</param>
    /// <param name="rule">The diagnostic rule to report.</param>
    /// <returns>True if a diagnostic was reported; otherwise, false.</returns>
    internal bool TryHandleMissingMockBehaviorParameter(
        OperationAnalysisContext context,
        IParameterSymbol? mockParameter,
        IMethodSymbol target,
        MoqKnownSymbols knownSymbols,
        DiagnosticDescriptor rule)
    {
        // NOTE: This complex condition is impractical to test as it requires constructing
        // specific combinations of method overloads and parameter configurations that
        // don't easily occur in typical test scenarios. The method serves as a helper
        // for derived analyzers with specific overload detection logic.
        // If the target method doesn't have a MockBehavior parameter, check if there's an overload that does
        return mockParameter is null
            && target.TryGetOverloadWithParameterOfType(knownSymbols.MockBehavior!, out IMethodSymbol? methodMatch, out _, cancellationToken: context.CancellationToken)
            && TryReportMockBehaviorDiagnostic(context, methodMatch, knownSymbols, rule, DiagnosticEditProperties.EditType.Insert);
    }

    private void RegisterCompilationStartAction(CompilationStartAnalysisContext context)
    {
        MoqKnownSymbols knownSymbols = new(context.Compilation);

        // Ensure Moq is referenced in the compilation
        // NOTE: This early return is impractical to test as it requires a compilation environment
        // where Moq is not referenced, but the analyzer infrastructure expects Moq to be present.
        // The condition serves as a defensive check for edge cases in the build environment.
        if (!knownSymbols.IsMockReferenced())
        {
            return;
        }

        // Look for the MockBehavior type and provide it to Analyze to avoid looking it up multiple times.
        // NOTE: This condition is impractical to test as it represents a scenario where
        // Moq is referenced but MockBehavior type cannot be resolved. This would indicate
        // an inconsistent or corrupted compilation state that doesn't occur in normal usage.
        if (knownSymbols.MockBehavior is null)
        {
            return;
        }

        context.RegisterOperationAction(context => AnalyzeObjectCreation(context, knownSymbols), OperationKind.ObjectCreation);

        context.RegisterOperationAction(context => AnalyzeInvocation(context, knownSymbols), OperationKind.Invocation);
    }

    private void AnalyzeObjectCreation(OperationAnalysisContext context, MoqKnownSymbols knownSymbols)
    {
        // NOTE: This early return in IObjectCreationOperation check is impractical to test
        // because the analyzer framework only calls this method with IObjectCreationOperation.
        // The check serves as a defensive guard against potential framework changes.
        if (context.Operation is not IObjectCreationOperation creation)
        {
            return;
        }

        if (creation.Type is null ||
            creation.Constructor is null ||
            !(creation.Type.IsInstanceOf(knownSymbols.Mock1) || creation.Type.IsInstanceOf(knownSymbols.MockRepository)))
        {
            // We could expand this check to include any method that accepts a MockBehavior parameter.
            // Leaving it narrowly scoped for now to avoid false positives and potential performance problems.
            return;
        }

        AnalyzeCore(context, creation.Constructor, creation.Arguments, knownSymbols);
    }

    private void AnalyzeInvocation(OperationAnalysisContext context, MoqKnownSymbols knownSymbols)
    {
        // NOTE: This early return in IInvocationOperation check is impractical to test
        // because the analyzer framework only calls this method with IInvocationOperation.
        // The check serves as a defensive guard against potential framework changes.
        if (context.Operation is not IInvocationOperation invocation)
        {
            return;
        }

        if (!invocation.TargetMethod.IsInstanceOf(knownSymbols.MockOf, out IMethodSymbol? match))
        {
            // We could expand this check to include any method that accepts a MockBehavior parameter.
            // Leaving it narrowly scoped for now to avoid false positives and potential performance problems.
            return;
        }

        AnalyzeCore(context, match, invocation.Arguments, knownSymbols);
    }
}
