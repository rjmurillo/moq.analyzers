namespace Moq.Analyzers;

internal static class DiagnosticExtensions
{
    /// <summary>
    /// Create a <see cref="Diagnostic"/> with the given <paramref name="descriptor"/> at the <paramref name="node"/>'s location.
    /// </summary>
    /// <param name="node">The <see cref="SyntaxNode"/> to use for a <see cref="Location"/> for a diagnostic.</param>
    /// <param name="descriptor">The <see cref="DiagnosticDescriptor"/> to use when creating the diagnostic.</param>
    /// <returns>A <see cref="Diagnostic"/> with the given <paramref name="descriptor"/> at the <paramref name="node"/>'s location.</returns>
    public static Diagnostic CreateDiagnostic(this SyntaxNode? node, DiagnosticDescriptor descriptor) =>
        Diagnostic.Create(descriptor, node?.GetLocation());

    /// <summary>
    /// Create a <see cref="Diagnostic"/> with the given <paramref name="descriptor"/> at the <paramref name="location"/>.
    /// </summary>
    /// <param name="location">The <see cref="Location"/> to use for a diagnostic.</param>
    /// <param name="descriptor">The <see cref="DiagnosticDescriptor"/> to use when creating the diagnostic.</param>
    /// <returns>A <see cref="Diagnostic"/> with the given <paramref name="descriptor"/> at the <paramref name="location"/>.</returns>
    public static Diagnostic CreateDiagnostic(this Location? location, DiagnosticDescriptor descriptor) =>
        Diagnostic.Create(descriptor, location);
}
