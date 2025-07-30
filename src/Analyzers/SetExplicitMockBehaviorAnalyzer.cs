using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers;

/// <summary>
/// Mock should explicitly specify a behavior and not rely on the default.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SetExplicitMockBehaviorAnalyzer : MockBehaviorDiagnosticAnalyzerBase
{
    private static readonly LocalizableString Title = "Moq: Explicitly choose a mock behavior";
    private static readonly LocalizableString Message = "Explicitly choose a mocking behavior for {0} instead of relying on the default (Loose) behavior";
    private static readonly LocalizableString Description = "Explicitly choose a mocking behavior instead of relying on the default (Loose) behavior.";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.SetExplicitMockBehavior,
        Title,
        Message,
        DiagnosticCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.SetExplicitMockBehavior}.md");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    /// <inheritdoc />
    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "Should be fixed. Ignoring for now to avoid additional churn as part of larger refactor.")]
    internal override void AnalyzeCore(OperationAnalysisContext context, IMethodSymbol target, ImmutableArray<IArgumentOperation> arguments, MoqKnownSymbols knownSymbols)
    {
        // Extract the type name for the diagnostic message
        string typeName = GetMockedTypeName(context, target);

        // Check if the target method has a parameter of type MockBehavior
        IParameterSymbol? mockParameter = target.Parameters.DefaultIfNotSingle(parameter => parameter.Type.IsInstanceOf(knownSymbols.MockBehavior));

        // If the target method doesn't have a MockBehavior parameter, check if there's an overload that does
        if (mockParameter is null && target.TryGetOverloadWithParameterOfType(knownSymbols.MockBehavior!, out IMethodSymbol? methodMatch, out _, cancellationToken: context.CancellationToken))
        {
            // Using a method that doesn't accept a MockBehavior parameter, however there's an overload that does
            ReportDiagnosticWithTypeName(context, methodMatch, typeName, knownSymbols, DiagnosticEditProperties.EditType.Insert);
            return;
        }

        IArgumentOperation? mockArgument = arguments.DefaultIfNotSingle(argument => argument.Parameter.IsInstanceOf(mockParameter));

        // Is the behavior set via a default value?
        if (mockArgument?.ArgumentKind == ArgumentKind.DefaultValue && mockArgument.Value.WalkDownConversion().ConstantValue.Value == knownSymbols.MockBehaviorDefault?.ConstantValue)
        {
            ReportDiagnosticWithTypeName(context, target, typeName, knownSymbols, DiagnosticEditProperties.EditType.Insert);
        }

        // NOTE: This logic can't handle indirection (e.g. var x = MockBehavior.Default; new Mock(x);). We can't use the constant value either,
        // as Loose and Default share the same enum value: `1`. Being more accurate I believe requires data flow analysis.
        //
        // The operation specifies a MockBehavior; is it MockBehavior.Default?
        if (mockArgument?.DescendantsAndSelf().OfType<IFieldReferenceOperation>().Any(argument => argument.Member.IsInstanceOf(knownSymbols.MockBehaviorDefault)) == true)
        {
            ReportDiagnosticWithTypeName(context, target, typeName, knownSymbols, DiagnosticEditProperties.EditType.Replace);
        }
    }

    private static string GetMockedTypeName(OperationAnalysisContext context, IMethodSymbol target)
    {
        // For object creation (new Mock<T>), get the type argument from the Mock<T> type
        if (context.Operation is IObjectCreationOperation objectCreation && objectCreation.Type is INamedTypeSymbol namedType && namedType.TypeArguments.Length > 0)
        {
            return namedType.TypeArguments[0].ToDisplayString();
        }

        // For method invocation (MockRepository.Of<T>), get the type argument from the method
        if (context.Operation is IInvocationOperation invocation && invocation.TargetMethod.TypeArguments.Length > 0)
        {
            return invocation.TargetMethod.TypeArguments[0].ToDisplayString();
        }

        // If we can't determine the type, try to get it from the target method if it's generic
        if (target.ContainingType?.TypeArguments.Length > 0)
        {
            return target.ContainingType.TypeArguments[0].ToDisplayString();
        }

        // Fallback to a generic name
        return "T";
    }

    private void ReportDiagnosticWithTypeName(
        OperationAnalysisContext context,
        IMethodSymbol method,
        string typeName,
        MoqKnownSymbols knownSymbols,
        DiagnosticEditProperties.EditType editType)
    {
        if (!method.TryGetParameterOfType(knownSymbols.MockBehavior!, out IParameterSymbol? parameterMatch, cancellationToken: context.CancellationToken))
        {
            return;
        }

        ImmutableDictionary<string, string?> properties = new DiagnosticEditProperties
        {
            TypeOfEdit = editType,
            EditPosition = parameterMatch.Ordinal,
        }.ToImmutableDictionary();

        context.ReportDiagnostic(context.Operation.CreateDiagnostic(Rule, properties, typeName));
    }
}
