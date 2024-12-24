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
    private static readonly LocalizableString Message = "Explicitly set the Strict mocking behavior";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.SetStrictMockBehavior,
        Title,
        Message,
        DiagnosticCategory.Moq,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.SetStrictMockBehavior}.md");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    /// <inheritdoc />
    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "Should be fixed. Ignoring for now to avoid additional churn as part of larger refactor.")]
    internal override void AnalyzeCore(OperationAnalysisContext context, IMethodSymbol target, ImmutableArray<IArgumentOperation> arguments, MoqKnownSymbols knownSymbols)
    {
        // Check if the target method has a parameter of type MockBehavior
        IParameterSymbol? mockParameter = target.Parameters.DefaultIfNotSingle(parameter => parameter.Type.IsInstanceOf(knownSymbols.MockBehavior));

        // If the target method doesn't have a MockBehavior parameter, check if there's an overload that does
        if (mockParameter is null && target.TryGetOverloadWithParameterOfType(knownSymbols.MockBehavior!, out IMethodSymbol? methodMatch, out _, cancellationToken: context.CancellationToken))
        {
            if (!methodMatch.TryGetParameterOfType(knownSymbols.MockBehavior!, out IParameterSymbol? parameterMatch, cancellationToken: context.CancellationToken))
            {
                return;
            }

            ImmutableDictionary<string, string?> properties = new DiagnosticEditProperties
            {
                TypeOfEdit = DiagnosticEditProperties.EditType.Insert,
                EditPosition = parameterMatch.Ordinal,
            }.ToImmutableDictionary();

            // Using a method that doesn't accept a MockBehavior parameter, however there's an overload that does
            context.ReportDiagnostic(context.Operation.CreateDiagnostic(Rule, properties));
            return;
        }

        IArgumentOperation? mockArgument = arguments.DefaultIfNotSingle(argument => argument.Parameter.IsInstanceOf(mockParameter));

        // Is the behavior set via a default value?
        if (mockArgument?.ArgumentKind == ArgumentKind.DefaultValue && mockArgument.Value.WalkDownConversion().ConstantValue.Value == knownSymbols.MockBehaviorDefault?.ConstantValue)
        {
            if (!target.TryGetParameterOfType(knownSymbols.MockBehavior!, out IParameterSymbol? parameterMatch, cancellationToken: context.CancellationToken))
            {
                return;
            }

            ImmutableDictionary<string, string?> properties = new DiagnosticEditProperties
            {
                TypeOfEdit = DiagnosticEditProperties.EditType.Insert,
                EditPosition = parameterMatch.Ordinal,
            }.ToImmutableDictionary();

            context.ReportDiagnostic(context.Operation.CreateDiagnostic(Rule, properties));
            return;
        }

        // NOTE: This logic can't handle indirection (e.g. var x = MockBehavior.Default; new Mock(x);)
        //
        // The operation specifies a MockBehavior; is it MockBehavior.Strict?
        if (mockArgument?.Value.WalkDownConversion().ConstantValue.Value != knownSymbols.MockBehaviorStrict?.ConstantValue
            && mockArgument?.DescendantsAndSelf().OfType<IFieldReferenceOperation>().Any(argument => argument.Member.IsInstanceOf(knownSymbols.MockBehaviorStrict)) != true)
        {
            if (!target.TryGetParameterOfType(knownSymbols.MockBehavior!, out IParameterSymbol? parameterMatch, cancellationToken: context.CancellationToken))
            {
                return;
            }

            ImmutableDictionary<string, string?> properties = new DiagnosticEditProperties
            {
                TypeOfEdit = DiagnosticEditProperties.EditType.Replace,
                EditPosition = parameterMatch.Ordinal,
            }.ToImmutableDictionary();

            context.ReportDiagnostic(context.Operation.CreateDiagnostic(Rule, properties));
        }
    }
}
