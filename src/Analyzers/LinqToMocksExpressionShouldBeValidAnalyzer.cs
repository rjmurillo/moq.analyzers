using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers;

/// <summary>
/// LINQ to Mocks expressions should be valid and supported.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class LinqToMocksExpressionShouldBeValidAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Maximum recursion depth for lambda body analysis. Hand-written LINQ-to-Mocks predicates
    /// nest well under 10 levels; 64 comfortably covers generated-but-plausible expressions while
    /// bounding the walk to a trivial number of stack frames (two frames per level via the
    /// AnalyzeLambdaBody/AnalyzeMemberOperations mutual recursion, so at most ~128 frames).
    /// Mirrors the pathological-tree guard in InvocationExpressionSyntaxExtensions.FindSetupInvocation
    /// (src/Common/InvocationExpressionSyntaxExtensions.cs, cap of 10), sized larger because
    /// operation trees for boolean predicates legitimately nest deeper than fluent chains.
    /// Operations beyond the cap are skipped: no crash, no diagnostic (accepted false negative).
    /// </summary>
    private static readonly int MaxAnalysisDepth = 64;

    private static readonly LocalizableString Title = "Moq: Invalid LINQ to Mocks expression";
    private static readonly LocalizableString Message = "Invalid member '{0}' in LINQ to Mocks expression";
    private static readonly LocalizableString Description = "LINQ to Mocks expression contains non-virtual member that cannot be mocked.";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.LinqToMocksExpressionShouldBeValid,
        Title,
        Message,
        DiagnosticCategory.Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description,
        helpLinkUri: $"https://github.com/rjmurillo/moq.analyzers/blob/{ThisAssembly.GitCommitId}/docs/rules/{DiagnosticIds.LinqToMocksExpressionShouldBeValid}.md");

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

        if (!knownSymbols.IsMockReferenced())
        {
            return;
        }

        context.RegisterOperationAction(
            operationContext => AnalyzeInvocation(operationContext, knownSymbols),
            OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, MoqKnownSymbols knownSymbols)
    {
        if (context.Operation is not IInvocationOperation invocationOperation)
        {
            return;
        }

        // Check if this is a Mock.Of invocation
        if (!IsValidMockOfInvocation(invocationOperation, knownSymbols))
        {
            return;
        }

        // Analyze lambda expressions in the arguments
        AnalyzeMockOfArguments(context, invocationOperation, knownSymbols);
    }

    /// <summary>
    /// Determines if the operation is a valid Mock.Of() invocation.
    /// </summary>
    private static bool IsValidMockOfInvocation(IInvocationOperation invocation, MoqKnownSymbols knownSymbols)
    {
        IMethodSymbol targetMethod = invocation.TargetMethod;

        return targetMethod.IsStatic && targetMethod.IsInstanceOf(knownSymbols.MockOf);
    }

    private static void AnalyzeMockOfArguments(OperationAnalysisContext context, IInvocationOperation invocationOperation, MoqKnownSymbols knownSymbols)
    {
        // Look for lambda expressions in the arguments (LINQ to Mocks expressions)
        foreach (IArgumentOperation argument in invocationOperation.Arguments)
        {
            IOperation argumentValue = argument.Value.WalkDownImplicitConversion();

            if (argumentValue is IAnonymousFunctionOperation lambdaOperation)
            {
                AnalyzeLambdaExpression(context, lambdaOperation, knownSymbols);
            }
        }
    }

    private static void AnalyzeLambdaExpression(OperationAnalysisContext context, IAnonymousFunctionOperation lambdaOperation, MoqKnownSymbols knownSymbols)
    {
        // Only report diagnostics if the lambda is semantically valid. Binding failures inside
        // the lambda materialize as IInvalidOperation nodes; scanning the already-built operation
        // tree replaces the previous SemanticModel.GetDiagnostics(span) call, which forced a full
        // re-bind of the span for every member-reference candidate.
        if (ContainsInvalidOperation(lambdaOperation))
        {
            return;
        }

        // For LINQ to Mocks, we need to handle more complex expressions like: x => x.Property == "value"
        // The lambda body is often a binary expression where the left operand is the member we want to check
        AnalyzeLambdaBody(context, lambdaOperation, lambdaOperation.Body, knownSymbols, depth: 0);
    }

    private static bool ContainsInvalidOperation(IOperation root)
    {
        foreach (IOperation descendant in root.DescendantsAndSelf())
        {
            if (descendant.Kind == OperationKind.Invalid)
            {
                return true;
            }
        }

        return false;
    }

    private static void AnalyzeLambdaBody(OperationAnalysisContext context, IAnonymousFunctionOperation lambdaOperation, IOperation? body, MoqKnownSymbols knownSymbols, int depth)
    {
        if (body == null)
        {
            return;
        }

        // Bound the AnalyzeLambdaBody <-> AnalyzeMemberOperations mutual recursion so
        // machine-generated expressions with thousands of clauses cannot overflow the stack.
        // A StackOverflowException here is uncatchable and would kill the host compiler/IDE
        // process, so operations beyond the cap are skipped: no crash, no diagnostic.
        if (depth > MaxAnalysisDepth)
        {
            return;
        }

        DispatchLambdaBodyOperation(context, lambdaOperation, body, knownSymbols, depth);
    }

    private static void DispatchLambdaBodyOperation(OperationAnalysisContext context, IAnonymousFunctionOperation lambdaOperation, IOperation body, MoqKnownSymbols knownSymbols, int depth)
    {
        switch (body)
        {
            case IBlockOperation blockOp when blockOp.Operations.Length == 1:
                // Handle block lambdas with return statements
                AnalyzeLambdaBody(context, lambdaOperation, blockOp.Operations[0], knownSymbols, depth + 1);
                break;

            case IReturnOperation returnOp:
                // Handle return statements
                AnalyzeLambdaBody(context, lambdaOperation, returnOp.ReturnedValue, knownSymbols, depth + 1);
                break;

            case IBinaryOperation binaryOp:
                // Analyze each operand independently. The IsRootedInLambdaParameter guard
                // in AnalyzeMemberOperations filters out operands not rooted in the lambda
                // parameter (e.g., static constants, enum values).
                AnalyzeMemberOperations(context, lambdaOperation, binaryOp.LeftOperand, knownSymbols, depth + 1);
                AnalyzeMemberOperations(context, lambdaOperation, binaryOp.RightOperand, knownSymbols, depth + 1);
                break;

            case IPropertyReferenceOperation propertyRef:
                // Direct property reference
                AnalyzeMemberSymbol(context, propertyRef.Property, lambdaOperation);
                break;

            case IInvocationOperation methodOp:
                // Direct method invocation
                AnalyzeMemberSymbol(context, methodOp.TargetMethod, lambdaOperation);
                break;

            case IFieldReferenceOperation fieldRef:
                // Direct field reference
                AnalyzeMemberSymbol(context, fieldRef.Field, lambdaOperation);
                break;

            default:
                // Route children through AnalyzeMemberOperations so they pass through the
                // IsRootedInLambdaParameter guard. Calling AnalyzeLambdaBody directly would
                // bypass the guard for operation kinds not enumerated above (e.g.,
                // IConditionalOperation, ICoalesceOperation).
                AnalyzeChildOperations(context, lambdaOperation, body, knownSymbols, depth + 1);
                break;
        }
    }

    private static void AnalyzeChildOperations(OperationAnalysisContext context, IAnonymousFunctionOperation lambdaOperation, IOperation body, MoqKnownSymbols knownSymbols, int depth)
    {
        foreach (IOperation childOperation in body.ChildOperations)
        {
            AnalyzeMemberOperations(context, lambdaOperation, childOperation, knownSymbols, depth);
        }
    }

    /// <summary>
    /// Guards member analysis by filtering out operations not rooted in the lambda parameter,
    /// then delegates to <see cref="AnalyzeLambdaBody"/> for recursive analysis.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method is the single entry point for all recursive member analysis. Every code path
    /// in <see cref="AnalyzeLambdaBody"/> that descends into child operations must route through
    /// this method. The <see cref="IOperationExtensions.IsRootedInLambdaParameter"/> guard is
    /// applied only to leaf member operations (<see cref="IMemberReferenceOperation"/> and
    /// <see cref="IInvocationOperation"/>). Composite operations (e.g., <c>IBinaryOperation</c>
    /// for chained <c>&amp;&amp;</c>/<c>||</c>/<c>==</c>) pass through to
    /// <see cref="AnalyzeLambdaBody"/> for decomposition. Blocking composite operations would
    /// cause false negatives for chained comparisons (see GitHub issue #1010).
    /// </para>
    /// <para>
    /// Nested <c>Mock.Of</c> calls are excluded to prevent false positives from inner mock
    /// expressions that have their own lambda parameters.
    /// </para>
    /// </remarks>
    private static void AnalyzeMemberOperations(OperationAnalysisContext context, IAnonymousFunctionOperation lambdaOperation, IOperation operation, MoqKnownSymbols knownSymbols, int depth)
    {
        // Don't recursively analyze nested Mock.Of calls to avoid false positives
        if (operation is IInvocationOperation invocation && IsValidMockOfInvocation(invocation, knownSymbols))
        {
            return;
        }

        // Only apply the lambda-parameter guard to leaf member operations (property,
        // field, event, method). Composite operations (IBinaryOperation for &&/||/==,
        // IConditionalOperation, etc.) must pass through to AnalyzeLambdaBody for
        // decomposition; blocking them here causes false negatives for chained
        // comparisons like `c.Prop == "a" && c.Other == "b"`.
        if (operation is (IMemberReferenceOperation or IInvocationOperation)
            && !operation.IsRootedInLambdaParameter(lambdaOperation))
        {
            return;
        }

        // Recursively analyze the operation to find member references. The caller already
        // incremented depth; AnalyzeLambdaBody is the single choke point that enforces the cap.
        AnalyzeLambdaBody(context, lambdaOperation, operation, knownSymbols, depth);
    }

    private static void AnalyzeMemberSymbol(OperationAnalysisContext context, ISymbol memberSymbol, IAnonymousFunctionOperation lambdaOperation)
    {
        // Only report diagnostics if the member is not from an interface
        if (memberSymbol.ContainingType?.TypeKind == TypeKind.Interface)
        {
            return;
        }

        // Don't flag members whose containing type failed to resolve (mid-edit code).
        // Binding failures inside the lambda are already handled once, up front, by the
        // ContainsInvalidOperation scan in AnalyzeLambdaExpression; this guard covers the
        // narrower case of an unresolved containing type on the member symbol itself.
        if (memberSymbol.ContainingType?.TypeKind == TypeKind.Error)
        {
            return;
        }

        Location? memberLocation = GetMemberReferenceLocation(lambdaOperation, memberSymbol, context.Operation.SemanticModel, context.CancellationToken);
        if (memberLocation == null)
        {
            return;
        }

        // Only report diagnostics for non-virtual, non-abstract, non-override members
        switch (memberSymbol)
        {
            case IPropertySymbol propertySymbol:
                if (!ShouldReportForProperty(propertySymbol))
                {
                    return;
                }

                break;
            case IMethodSymbol methodSymbol:
                if (!ShouldReportForMethod(methodSymbol))
                {
                    return;
                }

                break;
            case IFieldSymbol:
                // Always report for fields (fields are never virtual/abstract/override)
                break;
            default:
                // For any other symbol types, do not report
                return;
        }

        // Get the expression text from the syntax location
        string expressionText = memberLocation.SourceTree?.GetText(context.CancellationToken).ToString(memberLocation.SourceSpan) ?? memberSymbol.ToDisplayString();

        context.ReportDiagnostic(memberLocation.CreateDiagnostic(Rule, expressionText));
    }

    private static bool ShouldReportForProperty(IPropertySymbol property)
    {
        // Report diagnostic if property is not virtual, abstract, or override
        return property is { IsVirtual: false, IsAbstract: false, IsOverride: false };
    }

    private static bool ShouldReportForMethod(IMethodSymbol method)
    {
        // Report diagnostic if method is not virtual, abstract, or override
        return method is { IsVirtual: false, IsAbstract: false, IsOverride: false };
    }

    /// <summary>
    /// Attempts to find the specific syntax location of the member reference within the lambda using symbol-based matching.
    /// </summary>
    private static Location? GetMemberReferenceLocation(
        IAnonymousFunctionOperation lambdaOperation,
        ISymbol memberSymbol,
        SemanticModel? semanticModel,
        CancellationToken cancellationToken)
    {
        SyntaxNode syntax = lambdaOperation.Syntax;

        // 1. Try InvocationExpressionSyntax (for method calls)
        Location? location = syntax.FindLocation<InvocationExpressionSyntax>(memberSymbol, semanticModel, cancellationToken);
        if (location != null)
        {
            return location;
        }

        // 2. Try MemberAccessExpressionSyntax (for property/field access)
        location = syntax.FindLocation<MemberAccessExpressionSyntax>(memberSymbol, semanticModel, cancellationToken);
        if (location != null)
        {
            return location;
        }

        // Note for future maintainers:
        // The fallback to IdentifierNameSyntax is intentionally omitted. In the context of Moq LINQ-to-Mocks expressions,
        // member references are always accessed via the lambda parameter (e.g., x => x.Property or x => x.Method()),
        // which are represented as MemberAccessExpressionSyntax or InvocationExpressionSyntax. A bare identifier (IdentifierNameSyntax)
        // would only occur for static members or locals, which are not relevant for this analyzer and cannot be mocked.
        // No valid or invalid Moq usage can reach this code path, so it is not implemented and this is not an invalid omission.
        return null;
    }
}
