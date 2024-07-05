namespace Moq.Analyzers.Common;

internal static class MethodSymbolExtensions
{
    public static bool HasOptionalParameters(this IMethodSymbol methodSymbol)
    {
        return methodSymbol.Parameters.Any(parameterSymbol => parameterSymbol.IsOptional);
    }
}
