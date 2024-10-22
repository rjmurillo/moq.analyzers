namespace Moq.Analyzers.Common;

internal static class DiagnosticExtensions
{
    public static Diagnostic CreateDiagnostic(
        this SyntaxNode node,
        DiagnosticDescriptor rule,
        params object?[]? messageArgs) => node.CreateDiagnostic(rule, properties: null, messageArgs);

    public static Diagnostic CreateDiagnostic(
        this SyntaxNode node,
        DiagnosticDescriptor rule,
        ImmutableDictionary<string, string?>? properties,
        params object?[]? messageArgs) => node.CreateDiagnostic(rule, additionalLocations: null, properties, messageArgs);

    public static Diagnostic CreateDiagnostic(
        this SyntaxNode node,
        DiagnosticDescriptor rule,
        IEnumerable<Location>? additionalLocations,
        ImmutableDictionary<string, string?>? properties,
        params object?[]? messageArgs) => node
            .GetLocation()
            .CreateDiagnostic(
                rule: rule,
                additionalLocations: additionalLocations,
                properties: properties,
                messageArgs: messageArgs);

    public static Diagnostic CreateDiagnostic(
        this Location location,
        DiagnosticDescriptor rule,
        params object?[]? messageArgs) => location.CreateDiagnostic(rule, properties: null, messageArgs);

    public static Diagnostic CreateDiagnostic(
        this Location location,
        DiagnosticDescriptor rule,
        ImmutableDictionary<string, string?>? properties,
        params object?[]? messageArgs) => location.CreateDiagnostic(rule, additionalLocations: null, properties, messageArgs);

    public static Diagnostic CreateDiagnostic(
        this Location location,
        DiagnosticDescriptor rule,
        IEnumerable<Location>? additionalLocations,
        ImmutableDictionary<string, string?>? properties,
        params object?[]? messageArgs)
    {
        if (!location.IsInSource)
        {
            location = Location.None;
        }

        return Diagnostic.Create(rule, location, additionalLocations, properties, messageArgs);
    }

    public static Diagnostic CreateDiagnostic(
        this IOperation operation,
        DiagnosticDescriptor rule,
        params object?[]? messageArgs) => operation.CreateDiagnostic(rule, properties: null, messageArgs);

    public static Diagnostic CreateDiagnostic(
    this IOperation operation,
    DiagnosticDescriptor rule,
    ImmutableDictionary<string, string?>? properties,
    params object?[]? messageArgs) => operation.CreateDiagnostic(rule, additionalLocations: null, properties, messageArgs);

    public static Diagnostic CreateDiagnostic(
        this IOperation operation,
        DiagnosticDescriptor rule,
        IEnumerable<Location>? additionalLocations,
        ImmutableDictionary<string, string?>? properties,
        params object?[]? messageArgs) => operation.Syntax.CreateDiagnostic(rule, additionalLocations, properties, messageArgs);
}
