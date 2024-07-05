namespace Moq.Analyzers.Common;

internal static class DiagnosticExtensions
{
    public static Diagnostic CreateDiagnostic(
        this SyntaxNode node,
        DiagnosticDescriptor rule,
        params object?[]? messageArgs)
        => node.CreateDiagnostic(rule, properties: null, messageArgs);

    public static Diagnostic CreateDiagnostic(
        this SyntaxNode node,
        DiagnosticDescriptor rule,
        ImmutableDictionary<string, string?>? properties,
        params object?[]? messageArgs)
        => node.CreateDiagnostic(rule, additionalLocations: ImmutableArray<Location>.Empty, properties, messageArgs);

    public static Diagnostic CreateDiagnostic(
        this SyntaxNode node,
        DiagnosticDescriptor rule,
        ImmutableArray<Location> additionalLocations,
        ImmutableDictionary<string, string?>? properties,
        params object?[]? messageArgs)
        => node
            .GetLocation()
            .CreateDiagnostic(
                rule: rule,
                additionalLocations: additionalLocations,
                properties: properties,
                messageArgs: messageArgs);

    public static Diagnostic CreateDiagnostic(
            this IOperation operation,
            DiagnosticDescriptor rule,
            params object?[]? messageArgs)
            => operation.CreateDiagnostic(rule, properties: null, messageArgs);

    public static Diagnostic CreateDiagnostic(
        this IOperation operation,
        DiagnosticDescriptor rule,
        ImmutableDictionary<string, string?>? properties,
        params object?[]? messageArgs)
    {
        return operation.Syntax.CreateDiagnostic(rule, properties, messageArgs);
    }

    public static Diagnostic CreateDiagnostic(
        this IOperation operation,
        DiagnosticDescriptor rule,
        ImmutableArray<Location> additionalLocations,
        ImmutableDictionary<string, string?>? properties,
        params object?[]? messageArgs)
    {
        return operation.Syntax.CreateDiagnostic(rule, additionalLocations, properties, messageArgs);
    }

    public static Diagnostic CreateDiagnostic(
        this SyntaxToken token,
        DiagnosticDescriptor rule,
        params object?[]? messageArgs)
    {
        return token.GetLocation().CreateDiagnostic(rule, messageArgs);
    }

    public static Diagnostic CreateDiagnostic(
        this ISymbol symbol,
        DiagnosticDescriptor rule,
        params object?[]? messageArgs)
    {
        return symbol.Locations.CreateDiagnostic(rule, messageArgs);
    }

    public static Diagnostic CreateDiagnostic(
        this ISymbol symbol,
        DiagnosticDescriptor rule,
        ImmutableDictionary<string, string?>? properties,
        params object?[]? messageArgs)
    {
        return symbol.Locations.CreateDiagnostic(rule, properties, messageArgs);
    }

    public static Diagnostic CreateDiagnostic(
        this Location location,
        DiagnosticDescriptor rule,
        params object?[]? messageArgs)
        => location
            .CreateDiagnostic(
                rule: rule,
                properties: ImmutableDictionary<string, string?>.Empty,
                messageArgs: messageArgs);

    public static Diagnostic CreateDiagnostic(
        this Location location,
        DiagnosticDescriptor rule,
        ImmutableDictionary<string, string?>? properties,
        params object?[]? messageArgs)
        => location.CreateDiagnostic(rule, ImmutableArray<Location>.Empty, properties, messageArgs);

    public static Diagnostic CreateDiagnostic(
        this Location location,
        DiagnosticDescriptor rule,
        ImmutableArray<Location> additionalLocations,
        ImmutableDictionary<string, string?>? properties,
        params object?[]? messageArgs)
    {
        if (!location.IsInSource)
        {
            location = Location.None;
        }

        return Diagnostic.Create(
            descriptor: rule,
            location: location,
            additionalLocations: additionalLocations,
            properties: properties,
            messageArgs: messageArgs);
    }

    public static Diagnostic CreateDiagnostic(
        this IEnumerable<Location> locations,
        DiagnosticDescriptor rule,
        params object?[]? messageArgs)
    {
        return locations.CreateDiagnostic(rule, properties: null, messageArgs);
    }

    public static Diagnostic CreateDiagnostic(
        this IEnumerable<Location> locations,
        DiagnosticDescriptor rule,
        ImmutableDictionary<string, string?>? properties,
        params object?[]? messageArgs)
    {
        IEnumerable<Location> inSource = locations.Where(location => location.IsInSource);
        if (!inSource.Any())
        {
            return Diagnostic.Create(rule, location: null, messageArgs);
        }

        return Diagnostic.Create(
                 rule,
                 location: inSource.First(),
                 additionalLocations: inSource.Skip(1),
                 properties: properties,
                 messageArgs: messageArgs);
    }
}
