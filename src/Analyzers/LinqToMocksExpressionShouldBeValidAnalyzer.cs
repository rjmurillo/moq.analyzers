using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers;

/// <summary>
/// LINQ to Mocks expressions should be valid and supported.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class LinqToMocksExpressionShouldBeValidAnalyzer : DiagnosticAnalyzer
{
    private static readonly LocalizableString Title = "Moq: Invalid LINQ to Mocks expression";
    private static readonly LocalizableString Message = "LINQ to Mocks expression contains non-virtual member '{0}' that cannot be mocked";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.LinqToMocksExpressionShouldBeValid,
        Title,
        Message,
        DiagnosticCategory.Moq,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.LinqToMocksExpressionShouldBeValid}.md");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
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

        // Check if this is a Mock.Of invocation
        if (!IsValidMockOfInvocation(invocationOperation, knownSymbols))
        {
            return;
        }

        // Analyze lambda expressions in the arguments
        AnalyzeMockOfArguments(context, invocationOperation);
    }

    /// <summary>
    /// Determines if the operation is a valid Mock.Of() invocation.
    /// </summary>
    private static bool IsValidMockOfInvocation(IInvocationOperation invocation, MoqKnownSymbols knownSymbols)
    {
        IMethodSymbol targetMethod = invocation.TargetMethod;

        // Check if this is a static method call to Mock.Of()
        if (!targetMethod.IsStatic || !string.Equals(targetMethod.Name, "Of", StringComparison.Ordinal))
        {
            return false;
        }

        return targetMethod.ContainingType is not null &&
               targetMethod.ContainingType.Equals(knownSymbols.Mock, SymbolEqualityComparer.Default);
    }

    private static void AnalyzeMockOfArguments(OperationAnalysisContext context, IInvocationOperation invocationOperation)
    {
        // Look for lambda expressions in the arguments (LINQ to Mocks expressions)
        foreach (IArgumentOperation argument in invocationOperation.Arguments)
        {
            IOperation argumentValue = argument.Value.WalkDownImplicitConversion();

            if (argumentValue is IAnonymousFunctionOperation lambdaOperation)
            {
                AnalyzeLambdaExpression(context, lambdaOperation);
            }
        }
    }

    private static void AnalyzeLambdaExpression(OperationAnalysisContext context, IAnonymousFunctionOperation lambdaOperation)
    {
        // DEBUG: Report that we found a lambda
        Diagnostic debugDiagnostic = lambdaOperation.Syntax.GetLocation().CreateDiagnostic(Rule, "LAMBDA_FOUND");
        context.ReportDiagnostic(debugDiagnostic);

        // Use the existing utility to find referenced member symbols
        ISymbol? referencedMember = lambdaOperation.Body.GetReferencedMemberSymbolFromLambda();

        if (referencedMember != null)
        {
            // DEBUG: Report that we found a member
            Diagnostic memberDebugDiagnostic = lambdaOperation.Syntax.GetLocation().CreateDiagnostic(Rule, $"MEMBER_{referencedMember.Name}");
            context.ReportDiagnostic(memberDebugDiagnostic);

            AnalyzeMemberSymbol(context, referencedMember, lambdaOperation);
        }
        else
        {
            // DEBUG: Report that we didn't find a member
            Diagnostic noMemberDebugDiagnostic = lambdaOperation.Syntax.GetLocation().CreateDiagnostic(Rule, "NO_MEMBER");
            context.ReportDiagnostic(noMemberDebugDiagnostic);
        }
    }

    private static void AnalyzeMemberSymbol(OperationAnalysisContext context, ISymbol memberSymbol, IAnonymousFunctionOperation lambdaOperation)
    {
        // Skip if the symbol is part of an interface, those are always mockable
        if (memberSymbol.ContainingType?.TypeKind == TypeKind.Interface)
        {
            return;
        }

        bool shouldReportDiagnostic = memberSymbol switch
        {
            IPropertySymbol property => ShouldReportForProperty(property),
            IMethodSymbol method => ShouldReportForMethod(method),
            _ => false,
        };

        if (shouldReportDiagnostic)
        {
            // Try to find the specific syntax location for the member reference
            Location diagnosticLocation = GetMemberReferenceLocation(lambdaOperation, memberSymbol.Name)
                                        ?? lambdaOperation.Syntax.GetLocation();

            Diagnostic diagnostic = diagnosticLocation.CreateDiagnostic(Rule, memberSymbol.Name);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool ShouldReportForProperty(IPropertySymbol property)
    {
        // Report diagnostic if property is not virtual, abstract, or override
        return !property.IsVirtual && !property.IsAbstract && !property.IsOverride;
    }

    private static bool ShouldReportForMethod(IMethodSymbol method)
    {
        // Report diagnostic if method is not virtual, abstract, or override
        return !method.IsVirtual && !method.IsAbstract && !method.IsOverride;
    }

    /// <summary>
    /// Attempts to find the specific syntax location of the member reference within the lambda.
    /// </summary>
    private static Location? GetMemberReferenceLocation(IAnonymousFunctionOperation lambdaOperation, string memberName)
    {
        // Walk through the lambda body to find the specific member reference syntax
        SyntaxNode memberReferenceSyntax = lambdaOperation.Syntax
            .DescendantNodes()
            .FirstOrDefault(node => node.ToString().Contains(memberName));

        return memberReferenceSyntax?.GetLocation();
    }
}
