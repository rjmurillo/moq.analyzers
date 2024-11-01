using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers;

/// <summary>
/// Mock should explicitly specify a behavior and not rely on the default.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SetExplicitMockBehaviorAnalyzer : DiagnosticAnalyzer
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

        // Look for the MockBehavior type and provide it to Analyze to avoid looking it up multiple times.
        if (knownSymbols.MockBehavior is null)
        {
            return;
        }

        context.RegisterOperationAction(context => AnalyzeNewObject(context, knownSymbols), OperationKind.ObjectCreation);

        context.RegisterOperationAction(context => AnalyzeInvocation(context, knownSymbols), OperationKind.Invocation);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "ECS0900:Minimize boxing and unboxing", Justification = "<Pending>")]
    private static void AnalyzeNewObject(OperationAnalysisContext context, MoqKnownSymbols knownSymbols)
    {
        if (context.Operation is not IObjectCreationOperation creationOperation)
        {
            return;
        }

        if (creationOperation.Type is null || !(creationOperation.Type.IsInstanceOf(knownSymbols.Mock1) || creationOperation.Type.IsInstanceOf(knownSymbols.MockRepository)))
        {
            // We could expand this check to include any method that accepts a MockBehavior parameter.
            // Leaving it narrowly scoped for now to avoid false positives and potential performance problems.
            return;
        }

        IParameterSymbol? mockParameter = creationOperation.Constructor?.Parameters.DefaultIfNotSingle(parameter => parameter.Type.IsInstanceOf(knownSymbols.MockBehavior));

        if (mockParameter is null && creationOperation.Constructor!.TryGetOverloadWithParameterOfType(knownSymbols.MockBehavior!, out _, cancellationToken: context.CancellationToken))
        {
            // Using a constructor that doesn't accept a MockBehavior parameter
            context.ReportDiagnostic(creationOperation.CreateDiagnostic(Rule));
            return;
        }

        IArgumentOperation? mockArgument = creationOperation.Arguments.DefaultIfNotSingle(argument => argument.Parameter.IsInstanceOf(mockParameter));

        if (mockArgument?.DescendantsAndSelf().OfType<IMemberReferenceOperation>().Any(argument => argument.Member.IsInstanceOf(knownSymbols.MockBehaviorDefault)) == true)
        {
            context.ReportDiagnostic(creationOperation.CreateDiagnostic(Rule));
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "ECS0900:Minimize boxing and unboxing", Justification = "<Pending>")]
    private static void AnalyzeInvocation(OperationAnalysisContext context, MoqKnownSymbols knownSymbols)
    {
        if (context.Operation is not IInvocationOperation invocationOperation)
        {
            return;
        }

        if (!invocationOperation.TargetMethod.IsInstanceOf(knownSymbols.MockOf, out IMethodSymbol? match))
        {
            // We could expand this check to include any method that accepts a MockBehavior parameter.
            // Leaving it narrowly scoped for now to avoid false positives and potential performance problems.
            return;
        }

        IParameterSymbol? mockParameter = match.Parameters.DefaultIfNotSingle(parameter => parameter.Type.IsInstanceOf(knownSymbols.MockBehavior));

        if (mockParameter is null && match.TryGetOverloadWithParameterOfType(knownSymbols.MockBehavior!, out _, cancellationToken: context.CancellationToken))
        {
            // Using a method that doesn't accept a MockBehavior parameter
            context.ReportDiagnostic(invocationOperation.CreateDiagnostic(Rule));
            return;
        }

        IArgumentOperation? mockArgument = invocationOperation.Arguments.DefaultIfNotSingle(argument => argument.Parameter.IsInstanceOf(mockParameter));

        if (mockArgument?.DescendantsAndSelf().OfType<IMemberReferenceOperation>().Any(argument => argument.Member.IsInstanceOf(knownSymbols.MockBehaviorDefault)) == true)
        {
            context.ReportDiagnostic(invocationOperation.CreateDiagnostic(Rule));
        }
    }
}
