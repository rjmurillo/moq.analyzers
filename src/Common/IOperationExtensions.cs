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
    /// handling Moq expression lambdas that are typically converted to Expression<Func<...>> or Func<...>.
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
    /// Extracts a <see cref="ISymbol"/> from an <see cref="IOperation"/>, handling return operations, property references,
    /// method invocations, events, and fields.
    /// </summary>
    /// <param name="operation">The <see cref="IOperation"/> to analyze.</param>
    /// <returns>The extracted symbol, or <see langword="null" /> if not found or if the <paramref name="operation"/> operation is <see langword="null" />.</returns>
    private static ISymbol? GetSymbolFromOperation(this IOperation? operation)
    {
        switch (operation)
        {
            case null:
                return null;
            case IReturnOperation returnOp:
                operation = returnOp.ReturnedValue;
                break;
        }

        return operation switch
        {
            IPropertyReferenceOperation propertyRef => propertyRef.Property,
            IInvocationOperation methodOp => methodOp.TargetMethod,
            IEventReferenceOperation eventRef => eventRef.Event,
            IFieldReferenceOperation fieldRef => fieldRef.Field,
            _ => null,
        };
    }
}
