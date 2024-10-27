﻿using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers;

/// <summary>
/// Mock.As() should take interfaces only.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AsShouldBeUsedOnlyForInterfaceAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = "Moq: Invalid As type parameter";
    private static readonly LocalizableString Message = "Mock.As() should take interfaces only";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.AsShouldOnlyBeUsedForInterfacesRuleId,
        Title,
        Message,
        DiagnosticCategory.Moq,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.AsShouldOnlyBeUsedForInterfacesRuleId}.md");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(RegisterCompilationStartAction);
    }

    private static void RegisterCompilationStartAction(CompilationStartAnalysisContext context)
    {
        MoqKnownSymbols knownSymbols = new(context.Compilation);

        // Ensure Moq is referenced in the compilation
        if (!knownSymbols.IsMockReferenced())
        {
            return;
        }

        // Look for the Mock.As() method and provide it to Analyze to avoid looking it up multiple times.
        ImmutableArray<IMethodSymbol> asMethods = ImmutableArray.CreateRange([
            ..knownSymbols.MockAs,
            ..knownSymbols.Mock1As]);

        if (asMethods.IsEmpty)
        {
            return;
        }

        context.RegisterOperationAction(
            operationAnalysisContext => Analyze(operationAnalysisContext, asMethods),
            OperationKind.Invocation);
    }

    private static void Analyze(OperationAnalysisContext context, ImmutableArray<IMethodSymbol> wellKnownAsMethods)
    {
        if (context.Operation is not IInvocationOperation invocationOperation)
        {
            return;
        }

        IMethodSymbol targetMethod = invocationOperation.TargetMethod;
        if (!targetMethod.IsInstanceOf(wellKnownAsMethods))
        {
            return;
        }

        ImmutableArray<ITypeSymbol> typeArguments = targetMethod.TypeArguments;
        if (typeArguments.Length != 1)
        {
            return;
        }

        if (typeArguments[0] is ITypeSymbol { TypeKind: not TypeKind.Interface })
        {
            // Try to locate the type argument in the syntax tree to report the diagnostic at the correct location.
            // If that fails for any reason, report the diagnostic on the operation itself.
            NameSyntax? memberName = context.Operation.Syntax.DescendantNodes().OfType<MemberAccessExpressionSyntax>().Select(mae => mae.Name).DefaultIfNotSingle();
            Location location = memberName?.GetLocation() ?? invocationOperation.Syntax.GetLocation();

            context.ReportDiagnostic(location.CreateDiagnostic(Rule));
        }
    }
}
