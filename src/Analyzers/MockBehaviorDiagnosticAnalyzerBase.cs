using System.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers;

/// <summary>
/// Serves as a base class for diagnostic analyzers that analyze mock behavior in Moq.
/// </summary>
/// <remarks>
/// This abstract class provides common functionality for analyzing Moq's MockBehavior, such as registering
/// compilation start actions and defining the core analysis logic to be implemented by derived classes.
/// </remarks>
public abstract class MockBehaviorDiagnosticAnalyzerBase : MoqDiagnosticAnalyzerBase
{
    private protected abstract DiagnosticDescriptor DiagnosticRule { get; }

    /// <summary>
    /// Extracts the mocked type name from the operation for use in diagnostic messages.
    /// </summary>
    /// <param name="operation">The operation being analyzed.</param>
    /// <param name="target">The target method symbol.</param>
    /// <returns>The display name of the mocked type.</returns>
    internal virtual string GetMockedTypeName(IOperation operation, IMethodSymbol target)
    {
        // For object creation (new Mock<T>), get the type argument from the Mock<T> type
        if (operation is IObjectCreationOperation objectCreation
            && objectCreation.Type is INamedTypeSymbol namedType
            && namedType.TypeArguments.Length > 0)
        {
            return namedType.TypeArguments[0].ToDisplayString();
        }

        // For method invocation (Mock.Of<T>), get the type argument from the method
        if (operation is IInvocationOperation invocation && invocation.TargetMethod.TypeArguments.Length > 0)
        {
            return invocation.TargetMethod.TypeArguments[0].ToDisplayString();
        }

        // Try the containing type's type arguments (e.g. Mock<T>.ctor)
        if (target.ContainingType?.TypeArguments.Length > 0)
        {
            return target.ContainingType.TypeArguments[0].ToDisplayString();
        }

        return "T";
    }

    /// <summary>
    /// Attempts to report a diagnostic for a MockBehavior parameter issue, with the mocked type name.
    /// </summary>
    /// <param name="context">The operation analysis context.</param>
    /// <param name="parameterMatch">The MockBehavior parameter to edit.</param>
    /// <param name="rule">The diagnostic rule to report.</param>
    /// <param name="editType">The type of edit for the code fix.</param>
    /// <param name="mockedTypeNameSource">The method whose type arguments name the mocked type.</param>
    /// <returns>True if a diagnostic was reported; otherwise, false.</returns>
    internal bool TryReportMockBehaviorDiagnostic(
        OperationAnalysisContext context,
        IParameterSymbol parameterMatch,
        DiagnosticDescriptor rule,
        DiagnosticEditProperties.EditType editType,
        IMethodSymbol mockedTypeNameSource)
    {
        ImmutableDictionary<string, string?> properties = new DiagnosticEditProperties
        {
            TypeOfEdit = editType,
            EditPosition = parameterMatch.Ordinal,
        }.ToImmutableDictionary();

        string mockedTypeName = GetMockedTypeName(context.Operation, mockedTypeNameSource);
        context.ReportDiagnostic(context.Operation.CreateDiagnostic(rule, properties, mockedTypeName));
        return true;
    }

    /// <summary>
    /// Attempts to handle missing MockBehavior parameter by checking for overloads that accept it,
    /// with the mocked type name.
    /// </summary>
    /// <param name="context">The operation analysis context.</param>
    /// <param name="target">The target method to check for overloads.</param>
    /// <param name="knownSymbols">The known Moq symbols.</param>
    /// <param name="rule">The diagnostic rule to report.</param>
    /// <param name="mockedTypeNameSource">The method whose type arguments name the mocked type.</param>
    /// <returns>True if a diagnostic was reported; otherwise, false.</returns>
    internal bool TryHandleMissingMockBehaviorParameter(
        OperationAnalysisContext context,
        IMethodSymbol target,
        MoqKnownSymbols knownSymbols,
        DiagnosticDescriptor rule,
        IMethodSymbol mockedTypeNameSource)
    {
        INamedTypeSymbol? mockBehavior = knownSymbols.MockBehavior;
        if (mockBehavior is null)
        {
            Debug.Assert(false, "knownSymbols.MockBehavior must be non-null: registration is gated on it.");
            return false;
        }

        return target.TryGetOverloadWithParameterOfType(mockBehavior, out _, out IParameterSymbol? parameterMatch, cancellationToken: context.CancellationToken)
            && TryReportMockBehaviorDiagnostic(context, parameterMatch, rule, DiagnosticEditProperties.EditType.Insert, mockedTypeNameSource);
    }

    private protected abstract void AnalyzeMockBehaviorArgument(
        OperationAnalysisContext context,
        IMethodSymbol target,
        IParameterSymbol mockParameter,
        IArgumentOperation? mockArgument,
        MoqKnownSymbols knownSymbols);

    private protected bool ContainsFieldReference(IOperation operation, IFieldSymbol? field)
    {
        System.Diagnostics.Debug.Assert(field is not null, "MockBehavior field symbols are required once the MockBehavior type is resolved.");
        if (operation is IFieldReferenceOperation fieldReference && fieldReference.Member.IsInstanceOf(field!))
        {
            return true;
        }

        foreach (IOperation child in operation.ChildOperations)
        {
            if (ContainsFieldReference(child, field))
            {
                return true;
            }
        }

        return false;
    }

    private protected override void RegisterCompilationActions(CompilationStartAnalysisContext context, MoqKnownSymbols knownSymbols)
    {
        // Look for the MockBehavior type and provide it to Analyze to avoid looking it up multiple times.
        if (knownSymbols.MockBehavior is null)
        {
            return;
        }

        context.RegisterOperationAction(context => AnalyzeObjectCreation(context, knownSymbols), OperationKind.ObjectCreation);

        context.RegisterOperationAction(context => AnalyzeInvocation(context, knownSymbols), OperationKind.Invocation);
    }

    private void AnalyzeObjectCreation(OperationAnalysisContext context, MoqKnownSymbols knownSymbols)
    {
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

    private void AnalyzeCore(
        OperationAnalysisContext context,
        IMethodSymbol target,
        ImmutableArray<IArgumentOperation> arguments,
        MoqKnownSymbols knownSymbols)
    {
        IParameterSymbol? mockParameter = target.Parameters.DefaultIfNotSingle(parameter => parameter.Type.IsInstanceOf(knownSymbols.MockBehavior));

        if (mockParameter is null)
        {
            TryHandleMissingMockBehaviorParameter(context, target, knownSymbols, DiagnosticRule, target);
            return;
        }

        IArgumentOperation? mockArgument = arguments.DefaultIfNotSingle(argument => argument.Parameter.IsInstanceOf(mockParameter));

        AnalyzeMockBehaviorArgument(context, target, mockParameter, mockArgument, knownSymbols);
    }
}
