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
    /// Attempts to report a diagnostic for a MockBehavior parameter issue. The message names the mocked type,
    /// or the repository type for <c>new MockRepository(...)</c>.
    /// </summary>
    /// <param name="context">The operation analysis context.</param>
    /// <param name="parameterMatch">The MockBehavior parameter to edit.</param>
    /// <param name="rule">The diagnostic rule to report.</param>
    /// <param name="editType">The type of edit for the code fix.</param>
    /// <returns>True if a diagnostic was reported; otherwise, false.</returns>
    internal bool TryReportMockBehaviorDiagnostic(
        OperationAnalysisContext context,
        IParameterSymbol parameterMatch,
        DiagnosticDescriptor rule,
        DiagnosticEditProperties.EditType editType)
    {
        ImmutableDictionary<string, string?> properties = new DiagnosticEditProperties
        {
            TypeOfEdit = editType,
            EditPosition = parameterMatch.Ordinal,
        }.ToImmutableDictionary();

        string subjectName = GetDiagnosticSubjectName(context.Operation);
        context.ReportDiagnostic(context.Operation.CreateDiagnostic(rule, properties, subjectName));
        return true;
    }

    /// <summary>
    /// Attempts to handle missing MockBehavior parameter by checking for overloads that accept it.
    /// </summary>
    /// <param name="context">The operation analysis context.</param>
    /// <param name="target">The target method to check for overloads.</param>
    /// <param name="knownSymbols">The known Moq symbols.</param>
    /// <param name="rule">The diagnostic rule to report.</param>
    /// <returns>True if a diagnostic was reported; otherwise, false.</returns>
    internal bool TryHandleMissingMockBehaviorParameter(
        OperationAnalysisContext context,
        IMethodSymbol target,
        MoqKnownSymbols knownSymbols,
        DiagnosticDescriptor rule)
    {
        INamedTypeSymbol? mockBehavior = knownSymbols.MockBehavior;
        if (mockBehavior is null)
        {
            Debug.Assert(false, "knownSymbols.MockBehavior must be non-null: registration is gated on it.");
            return false;
        }

        return target.TryGetOverloadWithParameterOfType(mockBehavior, out _, out IParameterSymbol? parameterMatch, cancellationToken: context.CancellationToken)
            && TryReportMockBehaviorDiagnostic(context, parameterMatch, rule, DiagnosticEditProperties.EditType.Insert);
    }

    private protected abstract void AnalyzeMockBehaviorArgument(
        OperationAnalysisContext context,
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

    /// <summary>
    /// Gets the name the diagnostic message reports: the mocked type, or the repository type when
    /// the operation creates a <c>MockRepository</c>.
    /// </summary>
    /// <param name="operation">The object creation or invocation the diagnostic is reported on.</param>
    /// <returns>The display name for the message argument.</returns>
    private static string GetDiagnosticSubjectName(IOperation operation)
    {
        switch (operation)
        {
            // new Mock<T>(...): name the mocked type T.
            case IObjectCreationOperation { Type: INamedTypeSymbol { TypeArguments.Length: > 0 } mockType }:
                return mockType.TypeArguments[0].ToDisplayString();

            // A created type with no type argument, such as new MockRepository(...): there is no mocked
            // type, so name the created type. The short name is intentional: the rule docs quote
            // "MockRepository", not "Moq.MockRepository".
            case IObjectCreationOperation { Type: { } createdType }:
                return createdType.Name;

            // Mock.Of<T>(...): the invocation holds the constructed method. Its type argument is the mocked
            // type. The method definition would yield the type parameter name "T".
            case IInvocationOperation { TargetMethod.TypeArguments.Length: > 0 } invocation:
                return invocation.TargetMethod.TypeArguments[0].ToDisplayString();

            default:
                Debug.Assert(false, "Only Mock<T> and MockRepository creations and Mock.Of<T> invocations are analyzed.");
                return "mock";
        }
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
            TryHandleMissingMockBehaviorParameter(context, target, knownSymbols, DiagnosticRule);
            return;
        }

        IArgumentOperation? mockArgument = arguments.DefaultIfNotSingle(argument => argument.Parameter.IsInstanceOf(mockParameter));

        AnalyzeMockBehaviorArgument(context, mockParameter, mockArgument, knownSymbols);
    }
}
