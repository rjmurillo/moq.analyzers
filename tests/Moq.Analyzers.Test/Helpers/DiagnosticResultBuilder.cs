using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

namespace Moq.Analyzers.Test.Helpers;

/// <summary>
/// A builder class for creating <see cref="DiagnosticResult"/> instances with improved readability and type safety.
/// This class provides a fluent API for constructing diagnostic results for use in analyzer tests.
/// </summary>
public static class DiagnosticResultBuilder
{
    /// <summary>
    /// Creates a new diagnostic result with the specified diagnostic ID and error severity.
    /// </summary>
    /// <param name="diagnosticId">The diagnostic ID (e.g., "Moq1200").</param>
    /// <returns>A <see cref="DiagnosticResult"/> with error severity.</returns>
    public static DiagnosticResult Create(string diagnosticId)
    {
        return DiagnosticResult.CompilerError(diagnosticId);
    }

    /// <summary>
    /// Creates a new diagnostic result with the specified diagnostic ID and severity.
    /// </summary>
    /// <param name="diagnosticId">The diagnostic ID (e.g., "Moq1200").</param>
    /// <param name="severity">The diagnostic severity.</param>
    /// <returns>A <see cref="DiagnosticResult"/> with the specified severity.</returns>
    public static DiagnosticResult Create(string diagnosticId, DiagnosticSeverity severity)
    {
        return severity switch
        {
            DiagnosticSeverity.Error => DiagnosticResult.CompilerError(diagnosticId),
            DiagnosticSeverity.Warning => DiagnosticResult.CompilerWarning(diagnosticId),
            _ => new DiagnosticResult(diagnosticId, severity),
        };
    }

    /// <summary>
    /// Creates a new diagnostic result with the specified diagnostic ID, message, and error severity.
    /// </summary>
    /// <param name="diagnosticId">The diagnostic ID (e.g., "Moq1200").</param>
    /// <param name="message">The diagnostic message.</param>
    /// <returns>A <see cref="DiagnosticResult"/> with error severity and the specified message.</returns>
    public static DiagnosticResult Create(string diagnosticId, string message)
    {
        return DiagnosticResult.CompilerError(diagnosticId).WithMessage(message);
    }

    /// <summary>
    /// Creates a new diagnostic result with the specified diagnostic ID, message, and severity.
    /// </summary>
    /// <param name="diagnosticId">The diagnostic ID (e.g., "Moq1200").</param>
    /// <param name="message">The diagnostic message.</param>
    /// <param name="severity">The diagnostic severity.</param>
    /// <returns>A <see cref="DiagnosticResult"/> with the specified severity and message.</returns>
    public static DiagnosticResult Create(string diagnosticId, string message, DiagnosticSeverity severity)
    {
        var result = Create(diagnosticId, severity);
        return result.WithMessage(message);
    }

    /// <summary>
    /// Creates a new diagnostic result with full details including location.
    /// </summary>
    /// <param name="diagnosticId">The diagnostic ID (e.g., "Moq1200").</param>
    /// <param name="message">The diagnostic message.</param>
    /// <param name="line">The line number (1-based).</param>
    /// <param name="column">The column number (1-based).</param>
    /// <returns>A <see cref="DiagnosticResult"/> with the specified details and location.</returns>
    public static DiagnosticResult CreateAt(string diagnosticId, string message, int line, int column)
    {
        return Create(diagnosticId, message).WithSpan(line, column, line, column);
    }

    /// <summary>
    /// Creates a new diagnostic result with full details including span location.
    /// </summary>
    /// <param name="diagnosticId">The diagnostic ID (e.g., "Moq1200").</param>
    /// <param name="message">The diagnostic message.</param>
    /// <param name="startLine">The start line number (1-based).</param>
    /// <param name="startColumn">The start column number (1-based).</param>
    /// <param name="endLine">The end line number (1-based).</param>
    /// <param name="endColumn">The end column number (1-based).</param>
    /// <returns>A <see cref="DiagnosticResult"/> with the specified details and span location.</returns>
    public static DiagnosticResult CreateAt(string diagnosticId, string message, int startLine, int startColumn, int endLine, int endColumn)
    {
        return Create(diagnosticId, message).WithSpan(startLine, startColumn, endLine, endColumn);
    }

    /// <summary>
    /// Creates a new diagnostic result with full details including severity and location.
    /// </summary>
    /// <param name="diagnosticId">The diagnostic ID (e.g., "Moq1200").</param>
    /// <param name="message">The diagnostic message.</param>
    /// <param name="severity">The diagnostic severity.</param>
    /// <param name="line">The line number (1-based).</param>
    /// <param name="column">The column number (1-based).</param>
    /// <returns>A <see cref="DiagnosticResult"/> with the specified details and location.</returns>
    public static DiagnosticResult CreateAt(string diagnosticId, string message, DiagnosticSeverity severity, int line, int column)
    {
        return Create(diagnosticId, message, severity).WithSpan(line, column, line, column);
    }

    /// <summary>
    /// Creates a new diagnostic result with full details including severity and span location.
    /// </summary>
    /// <param name="diagnosticId">The diagnostic ID (e.g., "Moq1200").</param>
    /// <param name="message">The diagnostic message.</param>
    /// <param name="severity">The diagnostic severity.</param>
    /// <param name="startLine">The start line number (1-based).</param>
    /// <param name="startColumn">The start column number (1-based).</param>
    /// <param name="endLine">The end line number (1-based).</param>
    /// <param name="endColumn">The end column number (1-based).</param>
    /// <returns>A <see cref="DiagnosticResult"/> with the specified details and span location.</returns>
    public static DiagnosticResult CreateAt(string diagnosticId, string message, DiagnosticSeverity severity, int startLine, int startColumn, int endLine, int endColumn)
    {
        return Create(diagnosticId, message, severity).WithSpan(startLine, startColumn, endLine, endColumn);
    }
}
