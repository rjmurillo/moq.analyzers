﻿using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers;

/// <summary>
/// Mock should explicitly specify a behavior and not rely on the default.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SetExplicitMockBehaviorAnalyzer : MockBehaviorDiagnosticAnalyzerBase
{
    private static readonly LocalizableString Title = "Moq: Explicitly choose a mock behavior";
    private static readonly LocalizableString Message = "Explicitly choose a mocking behavior instead of relying on the default (Loose) behavior";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.SetExplicitMockBehavior,
        Title,
        Message,
        DiagnosticCategory.Moq,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.SetExplicitMockBehavior}.md");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    /// <inheritdoc />
    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "Should be fixed. Ignoring for now to avoid additional churn as part of larger refactor.")]
    internal override void AnalyzeCore(OperationAnalysisContext context, IMethodSymbol target, ImmutableArray<IArgumentOperation> arguments, MoqKnownSymbols knownSymbols)
    {
        // Check if the target method has a parameter of type MockBehavior
        IParameterSymbol? mockParameter = target.Parameters.DefaultIfNotSingle(parameter => parameter.Type.IsInstanceOf(knownSymbols.MockBehavior));

        // If the target method doesn't have a MockBehavior parameter, check if there's an overload that does
        if (TryHandleMissingMockBehaviorParameter(context, mockParameter, target, knownSymbols, Rule))
        {
            // Using a method that doesn't accept a MockBehavior parameter, however there's an overload that does
            return;
        }

        IArgumentOperation? mockArgument = arguments.DefaultIfNotSingle(argument => argument.Parameter.IsInstanceOf(mockParameter));

        // Is the behavior set via a default value?
        if (mockArgument?.ArgumentKind == ArgumentKind.DefaultValue && mockArgument.Value.WalkDownConversion().ConstantValue.Value == knownSymbols.MockBehaviorDefault?.ConstantValue)
        {
            TryReportMockBehaviorDiagnostic(context, target, knownSymbols, Rule, DiagnosticEditProperties.EditType.Insert);
        }

        // NOTE: This logic can't handle indirection (e.g. var x = MockBehavior.Default; new Mock(x);). We can't use the constant value either,
        // as Loose and Default share the same enum value: `1`. Being more accurate I believe requires data flow analysis.
        //
        // The operation specifies a MockBehavior; is it MockBehavior.Default?
        if (mockArgument?.DescendantsAndSelf().OfType<IFieldReferenceOperation>().Any(argument => argument.Member.IsInstanceOf(knownSymbols.MockBehaviorDefault)) == true)
        {
            TryReportMockBehaviorDiagnostic(context, target, knownSymbols, Rule, DiagnosticEditProperties.EditType.Replace);
        }
    }
}
