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
    /// Walks down consecutive implicit conversion operations until an operand is reached that isn't a conversion operation.
    /// </summary>
    /// <param name="operation">The starting operation.</param>
    /// <returns>The inner non conversion operation or the starting operation if it wasn't a conversion operation.</returns>
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

    public static ISymbol? GetSymbolFromOperation(this IOperation? operation)
    {
        if (operation is IReturnOperation returnOp)
        {
            operation = returnOp.ReturnedValue;
        }

        return operation switch
        {
            IPropertyReferenceOperation propertyRef => propertyRef.Property,
            IInvocationOperation methodOp => methodOp.TargetMethod,
            _ => null,
        };
    }

    public static ISymbol? GetReferencedMemberSymbolFromLambda(this IOperation? bodyOperation)
    {
        if (bodyOperation is IBlockOperation { Operations.Length: 1 } blockOperation)
        {
            // If it's a block lambda (example: => { return x.Property; })
            return blockOperation.Operations[0].GetSymbolFromOperation();
        }

        // If it's an expression lambda (example: => x.Property or => x.Method(...))
        return bodyOperation.GetSymbolFromOperation();
    }
}
