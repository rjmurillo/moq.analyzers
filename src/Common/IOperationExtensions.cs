using Microsoft.CodeAnalysis.Operations;

namespace Moq.Analyzers.Common;

internal static class IOperationExtensions
{
    /// <summary>
    /// Walks down consecutive conversion operations until an operand is reached that isn't a conversion operation.
    /// </summary>
    /// <param name="operation">The starting operation.</param>
    /// <returns>The inner non conversion operation or the starting operation if it wasn't a conversion operation.</returns>
    public static IOperation WalkDownConversion(this IOperation operation)
    {
        while (operation is IConversionOperation conversionOperation)
        {
            operation = conversionOperation.Operand;
        }

        return operation;
    }

    /// <summary>
    /// Walks down consecutive implicit conversion operations until an operand is reached that isn't an implicit conversion operation.
    /// Unlike WalkDownConversion, this method only traverses through implicit conversions, which is particularly useful for
    /// handling Moq expression lambdas that are typically converted to Expression&lt;Func&lt;...&gt;&gt; or Func&lt;...&gt;.
    /// </summary>
    /// <param name="operation">The starting operation.</param>
    /// <returns>The inner non-conversion operation or the starting operation if it wasn't an implicit conversion operation.</returns>
    public static IOperation WalkDownImplicitConversion(this IOperation operation)
    {
        // Keep peeling off any IConversionOperation layers as long as the conversion is implicit or trivial.
        // Typically, Moq expression lambdas are converted to Expression<Func<...>> or Func<...>.
        while (operation is IConversionOperation { Conversion.IsImplicit: true, Operand: not null } conversion)
        {
            operation = conversion.Operand;
        }

        return operation;
    }

    /// <summary>
    /// Extracts the referenced member symbol from a lambda operation, handling both block lambdas
    /// (e.g., => { return x.Property; }) and expression lambdas (e.g., => x.Property).
    /// Also handles <see cref="System.Action{T}"/> lambdas where the block may contain an expression statement
    /// plus an implicit void return (e.g., <c>SetupSet(x =&gt; x.Property = value)</c>).
    /// </summary>
    /// <param name="bodyOperation">The lambda body operation to analyze.</param>
    /// <returns>The referenced member symbol, or <see langword="null" /> if not found or if the operation is <see langword="null" />.</returns>
    internal static ISymbol? GetReferencedMemberSymbolFromLambda(this IOperation? bodyOperation)
        => TraverseLambdaBody(bodyOperation, static op => op.GetSymbolFromOperation());

    /// <summary>
    /// Extracts the referenced member syntax node from a lambda operation, handling both block lambdas
    /// (e.g., => { return x.Property; }) and expression lambdas (e.g., => x.Property).
    /// Also handles <see cref="System.Action{T}"/> lambdas where the block may contain an expression statement
    /// plus an implicit void return (e.g., <c>SetupSet(x =&gt; x.Property = value)</c>).
    /// </summary>
    /// <param name="bodyOperation">The lambda body operation to analyze.</param>
    /// <returns>The referenced member syntax node, or <see langword="null" /> if not found or if the operation is <see langword="null" />.</returns>
    internal static SyntaxNode? GetReferencedMemberSyntaxFromLambda(this IOperation? bodyOperation)
        => TraverseLambdaBody(bodyOperation, static op => op.GetSyntaxFromOperation());

    /// <summary>
    /// Determines whether an operation's receiver chain terminates in a parameter of the
    /// given lambda. Walks instance receivers (property, method, field, event) and transparent
    /// wrappers (conversion, parenthesized) until it reaches a
    /// <see cref="IParameterReferenceOperation"/> or a terminal node.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method exists because <c>IAnonymousFunctionOperation.GetCaptures()</c> is an
    /// internal Roslyn API and cannot be used by analyzers. Even if it were public, it
    /// solves a different problem: it reports closed-over variables, not whether a member
    /// access chain originates from the lambda parameter.
    /// </para>
    /// <para>
    /// Use this method before flagging member accesses inside lambda expression analysis
    /// to distinguish mock setup members (rooted in the lambda parameter) from value
    /// expressions (static members, external locals, constants).
    /// </para>
    /// </remarks>
    /// <param name="operation">The operation whose receiver chain to walk.</param>
    /// <param name="lambdaOperation">The lambda whose parameter to match against.</param>
    /// <returns>
    /// <see langword="true"/> if the receiver chain terminates in the lambda parameter;
    /// <see langword="false"/> otherwise.
    /// </returns>
    internal static bool IsRootedInLambdaParameter(
        this IOperation operation,
        IAnonymousFunctionOperation lambdaOperation)
    {
        IParameterSymbol? lambdaParameter = lambdaOperation.Symbol.Parameters.FirstOrDefault();
        if (lambdaParameter == null)
        {
            return false;
        }

        IOperation current = operation;
        while (current != null)
        {
            switch (current)
            {
                case IParameterReferenceOperation paramRef:
                    return SymbolEqualityComparer.Default.Equals(paramRef.Parameter, lambdaParameter);

                case IPropertyReferenceOperation propRef:
                    if (propRef.Instance == null)
                    {
                        return false; // Static property access
                    }

                    current = propRef.Instance;
                    break;

                case IInvocationOperation invocationOp:
                    if (invocationOp.Instance == null)
                    {
                        return false; // Static method call
                    }

                    current = invocationOp.Instance;
                    break;

                case IFieldReferenceOperation fieldRef:
                    if (fieldRef.Instance == null)
                    {
                        return false; // Static field access
                    }

                    current = fieldRef.Instance;
                    break;

                case IEventReferenceOperation eventRef:
                    if (eventRef.Instance == null)
                    {
                        return false; // Static event access
                    }

                    current = eventRef.Instance;
                    break;

                case IConversionOperation conversionOp:
                    current = conversionOp.Operand;
                    break;

                case IParenthesizedOperation parenOp:
                    current = parenOp.Operand;
                    break;

                default:
                    return false;
            }
        }

        return false;
    }

    /// <summary>
    /// Traverses a lambda body operation to extract a value. For block lambdas, iterates all
    /// operations and returns the first non-null result (handling <see cref="System.Action{T}"/> lambdas with multiple
    /// operations, e.g., ExpressionStatement + implicit void Return). For expression lambdas,
    /// applies the extractor directly.
    /// </summary>
    /// <typeparam name="T">The type of value to extract (e.g., <see cref="SyntaxNode"/>, <see cref="ISymbol"/>).</typeparam>
    /// <param name="bodyOperation">The lambda body operation to analyze.</param>
    /// <param name="extractor">A function that attempts to extract a value from a single operation.</param>
    /// <returns>The extracted value, or <see langword="null" /> if not found or if the operation is <see langword="null" />.</returns>
    private static T? TraverseLambdaBody<T>(IOperation? bodyOperation, Func<IOperation, T?> extractor)
        where T : class
    {
        if (bodyOperation is IBlockOperation blockOperation)
        {
            // Iterate all operations and return on the first match. This handles Action<T> block
            // lambdas (e.g., SetupSet) that emit ExpressionStatement + implicit void Return.
            // Moq setup expressions contain at most one meaningful operation, so first-match is correct.
            foreach (IOperation op in blockOperation.Operations)
            {
                T? result = extractor(op);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        return bodyOperation != null ? extractor(bodyOperation) : null;
    }

    /// <summary>
    /// Traverses an <see cref="IOperation"/> tree to extract a value using the provided selector, handling conversions, return operations, assignments, and expression statements.
    /// </summary>
    /// <typeparam name="T">The type of value to extract (e.g., <see cref="SyntaxNode"/>, <see cref="ISymbol"/>).</typeparam>
    /// <param name="operation">The <see cref="IOperation"/> to analyze.</param>
    /// <param name="selector">A function that extracts the desired value from a leaf operation.</param>
    /// <returns>The extracted value, or <see langword="null" /> if not found or if the <paramref name="operation"/> is <see langword="null" />.</returns>
    private static T? TraverseOperation<T>(this IOperation? operation, Func<IOperation, T?> selector)
        where T : class
    {
        while (true)
        {
            switch (operation)
            {
                case null:
                    return null;
                case IConversionOperation conversionOp:
                    operation = conversionOp.Operand;
                    continue;
                case IReturnOperation returnOp:
                    operation = returnOp.ReturnedValue;
                    continue;
                case IAssignmentOperation assignmentOp:
                    operation = assignmentOp.Target;
                    continue;
                case IExpressionStatementOperation exprStmtOp:
                    operation = exprStmtOp.Operation;
                    continue;
                default:
                    return selector(operation);
            }
        }
    }

    /// <summary>
    /// Extracts a syntax node from an <see cref="IOperation"/>, handling return operations, property references,
    /// method invocations, events, fields, assignment operations, and expression statements.
    /// </summary>
    /// <param name="operation">The <see cref="IOperation"/> to analyze.</param>
    /// <returns>The extracted syntax node, or <see langword="null" /> if not found or if the <paramref name="operation"/> operation is <see langword="null" />.</returns>
    private static SyntaxNode? GetSyntaxFromOperation(this IOperation? operation)
        => IOperationExtensions.TraverseOperation<SyntaxNode>(operation, static op => op switch
        {
            IPropertyReferenceOperation propertyRef => propertyRef.Syntax,
            IInvocationOperation methodOp => methodOp.Syntax,
            IEventReferenceOperation eventRef => eventRef.Syntax,
            IFieldReferenceOperation fieldRef => fieldRef.Syntax,
            _ => null,
        });

    /// <summary>
    /// Extracts a <see cref="ISymbol"/> from an <see cref="IOperation"/>, handling return operations, property references,
    /// method invocations, events, fields, assignment operations, and expression statements.
    /// </summary>
    /// <param name="operation">The <see cref="IOperation"/> to analyze.</param>
    /// <returns>The extracted symbol, or <see langword="null" /> if not found or if the <paramref name="operation"/> operation is <see langword="null" />.</returns>
    private static ISymbol? GetSymbolFromOperation(this IOperation? operation)
        => IOperationExtensions.TraverseOperation<ISymbol>(operation, static op => op switch
        {
            IPropertyReferenceOperation propertyRef => propertyRef.Property,
            IInvocationOperation methodOp => methodOp.TargetMethod,
            IEventReferenceOperation eventRef => eventRef.Event,
            IFieldReferenceOperation fieldRef => fieldRef.Field,
            _ => null,
        });
}
