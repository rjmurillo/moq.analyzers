﻿using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers;

/// <summary>
/// Mock.As() should take interfaces only.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AsShouldBeUsedOnlyForInterfaceAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = "Moq: Invalid As type parameter";
    private static readonly LocalizableString Message = "Mock.As() should take interfaces only, but '{0}' is not an interface";
    private static readonly LocalizableString Description = "Mock.As() should take interfaces only.";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.AsShouldOnlyBeUsedForInterfacesRuleId,
        Title,
        Message,
        DiagnosticCategory.Usage,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: Description,
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

        if (typeArguments[0] is ITypeSymbol { TypeKind: not TypeKind.Interface } typeSymbol)
        {
            // Find the first As<T> generic type argument and report the diagnostic on it
            GenericNameSyntax? asGeneric = invocationOperation.Syntax
                .DescendantNodes()
                .OfType<GenericNameSyntax>()
                .FirstOrDefault(x => string.Equals(x.Identifier.ValueText, "As", StringComparison.Ordinal));

            TypeSyntax? typeArg = asGeneric?.TypeArgumentList.Arguments.FirstOrDefault();
            Location location = typeArg?.GetLocation() ?? invocationOperation.Syntax.GetLocation();
            context.ReportDiagnostic(location.CreateDiagnostic(Rule, typeSymbol.ToDisplayString()));
        }
    }
}
