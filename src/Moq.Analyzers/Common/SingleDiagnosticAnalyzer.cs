namespace Moq.Analyzers.Common;

/// <summary>
/// Base class for an <see cref="DiagnosticAnalyzer"/> which may report only a single <see cref="DiagnosticDescriptor"/>.
/// </summary>
public abstract class SingleDiagnosticAnalyzer : DiagnosticAnalyzerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SingleDiagnosticAnalyzer"/> class.
    /// </summary>
    /// <param name="id">The <see cref="DiagnosticId"/>.</param>
    /// <param name="title">The diagnostic title.</param>
    /// <param name="messageFormat">The diagnostic message format.</param>
    /// <param name="description">The diagnostic description.</param>
    /// <param name="category">The diagnostic category.</param>
    /// <param name="severity">The diagnostic severity. Default value is <see cref="DiagnosticSeverity.Error"/>.</param>
    /// <param name="isEnabled">if set to <c>true</c> the diagnostic is enabled.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "AV1564:Parameter in public or internal member is of type bool or bool?", Justification = "Is flag for DiagnosticRule")]
    protected SingleDiagnosticAnalyzer(
        DiagnosticId id,
        string title,
        string messageFormat,
        string? description,
        string category,
        DiagnosticSeverity severity = DiagnosticSeverity.Error,
        bool isEnabled = true)
    {
        Id = id;
        string helpLink = id.ToHelpLinkUrl();
        Rule = new(Id.ToId(), title, messageFormat, category, severity, isEnabled, description, helpLink);
        SupportedDiagnostics = ImmutableArray.Create(Rule);
    }

    /// <summary>
    /// Gets the diagnostic identifier.
    /// </summary>
    /// <value>
    /// A <see cref="DiagnosticId"/> value.
    /// </value>
    public DiagnosticId Id { get; }

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

    /// <summary>
    /// Gets the <see cref="DiagnosticDescriptor"/> for this analyzer.
    /// </summary>
    /// <value>
    /// An instance of <see cref="DiagnosticDescriptor"/>.
    /// </value>
    protected DiagnosticDescriptor Rule { get; }
}
