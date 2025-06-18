using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Moq.Analyzers.Common;
using Moq.Analyzers.Common.WellKnown;

namespace Moq.Analyzers;

/// <summary>
/// Method setups that return a value should specify a return value using Returns() or Throws().
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MethodSetupShouldSpecifyReturnValueAnalyzer : DiagnosticAnalyzer
{
    internal static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.MethodSetupShouldSpecifyReturnValue,
        "Method setup should specify a return value",
        "Method setup for '{0}' should use Returns() or Throws() to specify a return value",
        DiagnosticCategory.Moq,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Method setups that have a return type should specify what value to return using Returns() or Throws().");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        if (context.Operation is not IInvocationOperation invocationOperation)
        {
            return;
        }

        SemanticModel? semanticModel = invocationOperation.SemanticModel;
        if (semanticModel == null)
        {
            return;
        }

        MoqKnownSymbols knownSymbols = new(semanticModel.Compilation);
        IMethodSymbol targetMethod = invocationOperation.TargetMethod;

        // Check if this is a Moq Setup method call
        if (!targetMethod.IsMoqSetupMethod(knownSymbols))
        {
            return;
        }

        // Get the mocked method symbol from the setup expression
        ISymbol? mockedMethodSymbol = TryGetMockedMethodSymbol(invocationOperation);
        if (mockedMethodSymbol is not IMethodSymbol mockedMethod)
        {
            return;
        }

        // Skip if method has void return type
        if (mockedMethod.ReturnsVoid)
        {
            return;
        }

        // Skip if the method setup already has a Returns/Throws chain
        if (HasReturnValueSpecification(invocationOperation))
        {
            return;
        }

        // Report diagnostic for methods with return types that don't specify a return value
        Diagnostic diagnostic = invocationOperation.Syntax.CreateDiagnostic(Rule, mockedMethod.Name);
        context.ReportDiagnostic(diagnostic);
    }

    /// <summary>
    /// Attempts to resolve the symbol representing the method being referenced in the Setup(...) call.
    /// </summary>
    private static ISymbol? TryGetMockedMethodSymbol(IInvocationOperation moqSetupInvocation)
    {
        if (moqSetupInvocation.Arguments.Length == 0)
        {
            return null;
        }

        IOperation argumentOperation = moqSetupInvocation.Arguments[0].Value;

        // Unwrap conversions (Roslyn often wraps lambdas in IConversionOperation)
        argumentOperation = argumentOperation.WalkDownImplicitConversion();

        if (argumentOperation is IAnonymousFunctionOperation lambdaOperation)
        {
            // Use the existing extension method to extract the member symbol
            return lambdaOperation.Body.GetReferencedMemberSymbolFromLambda();
        }

        return null;
    }

    /// <summary>
    /// Checks if the setup invocation is followed by a Returns() or Throws() call.
    /// This is a simplified check that looks at the immediate parent syntax.
    /// </summary>
    private static bool HasReturnValueSpecification(IInvocationOperation setupInvocation)
    {
        // Get the syntax node of the Setup call
        SyntaxNode setupSyntax = setupInvocation.Syntax;

        // Check if the Setup call is part of a method chain
        // by looking at the parent node. If it's a member access, it means
        // the Setup is being chained with something like .Returns() or .Throws()
        if (setupSyntax.Parent is Microsoft.CodeAnalysis.CSharp.Syntax.MemberAccessExpressionSyntax memberAccess)
        {
            // Check if the member being accessed is Returns, Throws, ReturnsAsync, ThrowsAsync
            string memberName = memberAccess.Name.Identifier.ValueText;
            return memberName.StartsWith("Returns", StringComparison.Ordinal) || memberName.StartsWith("Throws", StringComparison.Ordinal);
        }

        return false;
    }
}
