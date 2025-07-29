using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers;

/// <summary>
/// Mock should explicitly specify Strict behavior.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SetStrictMockBehaviorAnalyzer : MockBehaviorDiagnosticAnalyzerBase
{
    private static readonly LocalizableString Title = "Moq: Set MockBehavior to Strict";
    private static readonly LocalizableString Message = "Explicitly set the Strict mocking behavior for '{0}'";
    private static readonly LocalizableString Description = "Explicitly set the Strict mocking behavior.";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.SetStrictMockBehavior,
        Title,
        Message,
        DiagnosticCategory.Usage,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: Description,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.SetStrictMockBehavior}.md");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    /// <inheritdoc />
    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "Should be fixed. Ignoring for now to avoid additional churn as part of larger refactor.")]
    internal override void AnalyzeCore(OperationAnalysisContext context, IMethodSymbol target, ImmutableArray<IArgumentOperation> arguments, MoqKnownSymbols knownSymbols)
    {
        // Extract the mocked type name for the diagnostic message
        string mockedTypeName = GetMockedTypeName(context.Operation, target);

        // Check if the target method has a parameter of type MockBehavior
        IParameterSymbol? mockParameter = target.Parameters.DefaultIfNotSingle(parameter => parameter.Type.IsInstanceOf(knownSymbols.MockBehavior));

        // If the target method doesn't have a MockBehavior parameter, check if there's an overload that does
        if (TryHandleMissingMockBehaviorParameter(context, mockParameter, target, knownSymbols, mockedTypeName))
        {
            // Using a method that doesn't accept a MockBehavior parameter, however there's an overload that does
            return;
        }

        IArgumentOperation? mockArgument = arguments.DefaultIfNotSingle(argument => argument.Parameter.IsInstanceOf(mockParameter));

        // Is the behavior set via a default value?
        if (mockArgument?.ArgumentKind == ArgumentKind.DefaultValue && mockArgument.Value.WalkDownConversion().ConstantValue.Value == knownSymbols.MockBehaviorDefault?.ConstantValue
            && TryReportStrictMockBehaviorDiagnostic(context, target, knownSymbols, mockedTypeName, DiagnosticEditProperties.EditType.Insert))
        {
            return;
        }

        // NOTE: This logic can't handle indirection (e.g. var x = MockBehavior.Default; new Mock(x);)
        //
        // The operation specifies a MockBehavior; is it MockBehavior.Strict?
        if (mockArgument?.Value.WalkDownConversion().ConstantValue.Value != knownSymbols.MockBehaviorStrict?.ConstantValue
            && mockArgument?.DescendantsAndSelf().OfType<IFieldReferenceOperation>().Any(argument => argument.Member.IsInstanceOf(knownSymbols.MockBehaviorStrict)) != true)
        {
            TryReportStrictMockBehaviorDiagnostic(context, target, knownSymbols, mockedTypeName, DiagnosticEditProperties.EditType.Replace);
        }
    }

    /// <summary>
    /// Extracts the mocked type name from the operation.
    /// </summary>
    /// <param name="operation">The operation being analyzed.</param>
    /// <param name="target">The target method symbol.</param>
    /// <returns>The name of the mocked type, or "Unknown" if it cannot be determined.</returns>
    private static string GetMockedTypeName(IOperation operation, IMethodSymbol target)
    {
        // For object creation like new Mock<ISample>()
        if (operation is IObjectCreationOperation objectCreation
            && objectCreation.Type is INamedTypeSymbol namedType
            && namedType.TypeArguments.Length > 0)
        {
            return namedType.TypeArguments[0].ToDisplayString();
        }

        // For method invocation like Mock.Of<ISample>()
        if (operation is IInvocationOperation && target.TypeArguments.Length > 0)
        {
            return target.TypeArguments[0].Name;
        }

        return "Unknown";
    }

    /// <summary>
    /// Attempts to report a strict mock behavior diagnostic with the mocked type name.
    /// </summary>
    /// <param name="context">The operation analysis context.</param>
    /// <param name="method">The method to check for MockBehavior parameter.</param>
    /// <param name="knownSymbols">The known Moq symbols.</param>
    /// <param name="mockedTypeName">The name of the mocked type.</param>
    /// <param name="editType">The type of edit for the code fix.</param>
    /// <returns>True if a diagnostic was reported; otherwise, false.</returns>
    private bool TryReportStrictMockBehaviorDiagnostic(
        OperationAnalysisContext context,
        IMethodSymbol method,
        MoqKnownSymbols knownSymbols,
        string mockedTypeName,
        DiagnosticEditProperties.EditType editType)
    {
        if (!method.TryGetParameterOfType(knownSymbols.MockBehavior!, out IParameterSymbol? parameterMatch, cancellationToken: context.CancellationToken))
        {
            return false;
        }

        ImmutableDictionary<string, string?> properties = new DiagnosticEditProperties
        {
            TypeOfEdit = editType,
            EditPosition = parameterMatch.Ordinal,
        }.ToImmutableDictionary();

        context.ReportDiagnostic(context.Operation.CreateDiagnostic(Rule, properties, mockedTypeName));
        return true;
    }

    /// <summary>
    /// Attempts to handle missing MockBehavior parameter by checking for overloads that accept it.
    /// </summary>
    /// <param name="context">The operation analysis context.</param>
    /// <param name="mockParameter">The MockBehavior parameter (should be null to trigger overload check).</param>
    /// <param name="target">The target method to check for overloads.</param>
    /// <param name="knownSymbols">The known Moq symbols.</param>
    /// <param name="mockedTypeName">The name of the mocked type.</param>
    /// <returns>True if a diagnostic was reported; otherwise, false.</returns>
    private bool TryHandleMissingMockBehaviorParameter(
        OperationAnalysisContext context,
        IParameterSymbol? mockParameter,
        IMethodSymbol target,
        MoqKnownSymbols knownSymbols,
        string mockedTypeName)
    {
        // If the target method doesn't have a MockBehavior parameter, check if there's an overload that does
        return mockParameter is null
            && target.TryGetOverloadWithParameterOfType(knownSymbols.MockBehavior!, out IMethodSymbol? methodMatch, out _, cancellationToken: context.CancellationToken)
            && TryReportStrictMockBehaviorDiagnostic(context, methodMatch, knownSymbols, mockedTypeName, DiagnosticEditProperties.EditType.Insert);
    }
}
