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
}
