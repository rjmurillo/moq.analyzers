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
    /// </summary>
    /// <param name="bodyOperation">The lambda body operation to analyze.</param>
    /// <returns>The referenced member symbol, or <see langword="null" /> if not found or if the operation is <see langword="null" />.</returns>
    internal static ISymbol? GetReferencedMemberSymbolFromLambda(this IOperation? bodyOperation)
    {
        if (bodyOperation is IBlockOperation { Operations.Length: 1 } blockOperation)
        {
            // If it's a block lambda (example: => { return x.Property; })
            return blockOperation.Operations[0].GetSymbolFromOperation();
        }

        // If it's an expression lambda (example: => x.Property or => x.Method(...))
        return bodyOperation.GetSymbolFromOperation();
    }

    /// <summary>
    /// Extracts the referenced member syntax node from a lambda operation, handling both block lambdas
    /// (e.g., => { return x.Property; }) and expression lambdas (e.g., => x.Property).
    /// </summary>
    /// <param name="bodyOperation">The lambda body operation to analyze.</param>
    /// <returns>The referenced member syntax node, or <see langword="null" /> if not found or if the operation is <see langword="null" />.</returns>
    internal static SyntaxNode? GetReferencedMemberSyntaxFromLambda(this IOperation? bodyOperation)
    {
        if (bodyOperation is IBlockOperation { Operations.Length: 1 } blockOperation)
        {
            // If it's a block lambda (example: => { return x.Property; })
            return blockOperation.Operations[0].GetSyntaxFromOperation();
        }

        // If it's an expression lambda (example: => x.Property or => x.Method(...))
        return bodyOperation.GetSyntaxFromOperation();
    }

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
        IOperation? current = operation;
        while (true)
        {
            switch (current)
            {
                case IParameterReferenceOperation paramRef:
                    return lambdaParameter is not null &&
                        SymbolEqualityComparer.Default.Equals(paramRef.Parameter, lambdaParameter);

                case IMemberReferenceOperation memberRef:
                    if (memberRef.Instance == null)
                    {
                        return false; // Static member access
                    }

                    current = memberRef.Instance;
                    break;

                case IInvocationOperation invocationOp:
                    if (invocationOp.Instance == null)
                    {
                        return false; // Static method call
                    }

                    current = invocationOp.Instance;
                    break;

                case IConversionOperation conversionOp:
                    current = conversionOp.Operand;
                    break;

                default:
                    // IParenthesizedOperation is intentionally omitted. The C# compiler
                    // never emits it in IOperation trees (VB.NET only), and this analyzer
                    // targets C# exclusively via [DiagnosticAnalyzer(LanguageNames.CSharp)].
                    return false;
            }
        }
    }

    /// <summary>
    /// Traverses an <see cref="IOperation"/> tree to extract a value using the provided selector, handling return operations, assignments, and expression statements.
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
                    return operation != null ? selector(operation) : null;
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
