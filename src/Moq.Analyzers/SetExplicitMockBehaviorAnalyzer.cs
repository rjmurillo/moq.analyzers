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
        // Ensure Moq is referenced in the compilation
        ImmutableArray<INamedTypeSymbol> mockTypes = context.Compilation.GetMoqMock();
        if (mockTypes.IsEmpty)
        {
            return;
        }

        // Look for the Mock.As() method and provide it to Analyze to avoid looking it up multiple times.
#pragma warning disable ECS0900 // Minimize boxing and unboxing
        ImmutableArray<IMethodSymbol> ofMethods = mockTypes
            .SelectMany(mockType => mockType.GetMembers(WellKnownTypeNames.Of))
            .OfType<IMethodSymbol>()
            .Where(method => method.IsGenericMethod)
            .ToImmutableArray();
#pragma warning restore ECS0900 // Minimize boxing and unboxing

        context.RegisterOperationAction(
            static context => AnalyzeNewObject(context),
            OperationKind.ObjectCreation);

        if (!ofMethods.IsEmpty)
        {
            context.RegisterOperationAction(
                context => AnalyzeInvocation(context, ofMethods),
                OperationKind.Invocation);
        }
    }

    private static void AnalyzeNewObject(OperationAnalysisContext context)
    {
        if (context.Operation is not IObjectCreationOperation creationOperation)
        {
            return;
        }

        if (creationOperation.Type is not INamedTypeSymbol namedType)
        {
            return;
        }

        if (namedType.ConstructUnboundGenericType().Name != WellKnownTypeNames.MockName)
        {
            return;
        }

        if (!creationOperation.Arguments.Any(p => p.Value is not IFieldReferenceOperation { Name: WellKnownTypeNames.MockBehavior }))
        {
#pragma warning disable ECS0900 // Minimize boxing and unboxing
            IEnumerable<IArgumentOperation> arguments = context.Operation.ChildOperations.OfType<IArgumentOperation>();
#pragma warning restore ECS0900 // Minimize boxing and unboxing
            if (!arguments.Any(arg => new string[] { "MockBehavior.Loose", "MockBehavior.Strict" }.Contains(arg.Value.Syntax.ToString())))
            {
                context.ReportDiagnostic(creationOperation.Syntax.GetLocation().CreateDiagnostic(Rule));
            }
        }
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, ImmutableArray<IMethodSymbol> wellKnownOfMethods)
    {
        if (context.Operation is not IInvocationOperation invocationOperation)
        {
            return;
        }

        IMethodSymbol targetMethod = invocationOperation.TargetMethod;
#pragma warning disable ECS0900 // Minimize boxing and unboxing
        if (!targetMethod.IsInstanceOf(wellKnownOfMethods))
        {
            return;
        }
#pragma warning restore ECS0900 // Minimize boxing and unboxing

        // if (targetMethod.Parameters.Length == 0)
        // {
        //     context.ReportDiagnostic(invocationOperation.Syntax.GetLocation().CreateDiagnostic(Rule));
        // }

        if (!targetMethod.Parameters.Any(p => p.Type is not INamedTypeSymbol { Name: WellKnownTypeNames.MockBehavior }))
        {
#pragma warning disable ECS0900 // Minimize boxing and unboxing
            IEnumerable<IArgumentOperation> arguments = context.Operation.ChildOperations.OfType<IArgumentOperation>();
#pragma warning restore ECS0900 // Minimize boxing and unboxing
            if (!arguments.Any(arg => new string[] { "MockBehavior.Loose", "MockBehavior.Strict" }.Contains(arg.Value.Syntax.ToString())))
            {
                context.ReportDiagnostic(invocationOperation.Syntax.GetLocation().CreateDiagnostic(Rule));
            }
        }
    }

    private static void Analyze(OperationAnalysisContext context, ImmutableArray<IMethodSymbol> wellKnownAsMethods)
    {
        if (context.Operation is not IInvocationOperation invocationOperation)
        {
            return;
        }

        IMethodSymbol targetMethod = invocationOperation.TargetMethod;
#pragma warning disable ECS0900 // Minimize boxing and unboxing
        if (!targetMethod.IsInstanceOf(wellKnownAsMethods))
        {
            return;
        }
#pragma warning restore ECS0900 // Minimize boxing and unboxing

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

            // context.ReportDiagnostic(location.CreateDiagnostic(Rule))
        }
    }
}
