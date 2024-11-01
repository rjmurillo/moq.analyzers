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

    private static void AnalyzeNewObject(OperationAnalysisContext context, MoqKnownSymbols knownSymbols)
    {
        if (context.Operation is not IObjectCreationOperation creationOperation)
        {
            return;
        }

        if (creationOperation.Type is not INamedTypeSymbol namedType)
        {
            return;
        }

        ImmutableArray<INamedTypeSymbol> mockTypes = new[] { knownSymbols.Mock1, knownSymbols.MockRepository }.WhereNotNull().ToImmutableArray();
        ImmutableArray<IFieldSymbol> explicitBehaviors = new[] { knownSymbols.MockBehaviorLoose, knownSymbols.MockBehaviorStrict }.WhereNotNull().ToImmutableArray();

        if (!namedType.IsInstanceOf(mockTypes))
        {
            return;
        }

        if (creationOperation.Arguments.Any(argument => argument.DescendantsAndSelf().OfType<IMemberReferenceOperation>().Any(argument => argument.Member.IsInstanceOf(explicitBehaviors))))
        {
            return;
        }

        context.ReportDiagnostic(creationOperation.CreateDiagnostic(Rule));
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, MoqKnownSymbols knownSymbols)
    {
        if (context.Operation is not IInvocationOperation invocationOperation)
        {
            return;
        }

        ImmutableArray<IMethodSymbol> wellKnownOfMethods = knownSymbols.MockOf;
        ImmutableArray<IFieldSymbol> explicitBehaviors = new[] { knownSymbols.MockBehaviorLoose, knownSymbols.MockBehaviorStrict }.WhereNotNull().ToImmutableArray();

        IMethodSymbol targetMethod = invocationOperation.TargetMethod;
        if (!targetMethod.IsInstanceOf(wellKnownOfMethods))
        {
            return;
        }

        if (invocationOperation.Arguments.Any(argument => argument.DescendantsAndSelf().OfType<IMemberReferenceOperation>().Any(argument => argument.Member.IsInstanceOf(explicitBehaviors))))
        {
            return;
        }

        context.ReportDiagnostic(invocationOperation.CreateDiagnostic(Rule));
    }
}
