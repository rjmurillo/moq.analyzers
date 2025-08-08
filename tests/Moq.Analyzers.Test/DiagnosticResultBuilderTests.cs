using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Moq.Analyzers.Test.Helpers;
using Xunit;

namespace Moq.Analyzers.Test;

/// <summary>
/// Tests for the <see cref="DiagnosticResultBuilder"/> class to ensure it creates diagnostic results correctly.
/// </summary>
public class DiagnosticResultBuilderTests
{
    [Fact]
    public void Create_WithId_ReturnsErrorDiagnostic()
    {
        // Act
        var result = DiagnosticResultBuilder.Create("TEST001");

        // Assert
        Assert.Equal("TEST001", result.Id);
        Assert.Equal(DiagnosticSeverity.Error, result.Severity);
        Assert.True(string.IsNullOrEmpty(result.Message)); // Message can be null or empty
    }

    [Fact]
    public void Create_WithIdAndSeverity_ReturnsDiagnosticWithCorrectSeverity()
    {
        // Act
        var result = DiagnosticResultBuilder.Create("TEST001", DiagnosticSeverity.Warning);

        // Assert
        Assert.Equal("TEST001", result.Id);
        Assert.Equal(DiagnosticSeverity.Warning, result.Severity);
    }

    [Fact]
    public void Create_WithIdAndMessage_ReturnsDiagnosticWithMessage()
    {
        // Act
        var result = DiagnosticResultBuilder.Create("TEST001", "Test message");

        // Assert
        Assert.Equal("TEST001", result.Id);
        Assert.Equal(DiagnosticSeverity.Error, result.Severity);
        Assert.Equal("Test message", result.Message);
    }

    [Fact]
    public void Create_WithAllParameters_ReturnsDiagnosticWithAllDetails()
    {
        // Act
        var result = DiagnosticResultBuilder.Create("TEST001", "Test message", DiagnosticSeverity.Warning);

        // Assert
        Assert.Equal("TEST001", result.Id);
        Assert.Equal(DiagnosticSeverity.Warning, result.Severity);
        Assert.Equal("Test message", result.Message);
    }

    [Fact]
    public void CreateAt_WithLineAndColumn_ReturnsDiagnosticWithLocation()
    {
        // Act
        var result = DiagnosticResultBuilder.CreateAt("TEST001", "Test message", 10, 5);

        // Assert
        Assert.Equal("TEST001", result.Id);
        Assert.Equal("Test message", result.Message);
        Assert.True(result.Spans.Length == 1);

        var span = result.Spans[0];
        Assert.Equal(10, span.Span.StartLinePosition.Line + 1); // Converting from 0-based to 1-based
        Assert.Equal(5, span.Span.StartLinePosition.Character + 1); // Converting from 0-based to 1-based
    }

    [Fact]
    public void CreateAt_WithSpan_ReturnsDiagnosticWithSpanLocation()
    {
        // Act
        var result = DiagnosticResultBuilder.CreateAt("TEST001", "Test message", 10, 5, 10, 20);

        // Assert
        Assert.Equal("TEST001", result.Id);
        Assert.Equal("Test message", result.Message);
        Assert.True(result.Spans.Length == 1);

        var span = result.Spans[0];
        Assert.Equal(10, span.Span.StartLinePosition.Line + 1); // Converting from 0-based to 1-based
        Assert.Equal(5, span.Span.StartLinePosition.Character + 1); // Converting from 0-based to 1-based
        Assert.Equal(10, span.Span.EndLinePosition.Line + 1); // Converting from 0-based to 1-based
        Assert.Equal(20, span.Span.EndLinePosition.Character + 1); // Converting from 0-based to 1-based
    }

    [Fact]
    public void CreateAt_WithSeverityAndLocation_ReturnsDiagnosticWithAllDetails()
    {
        // Act
        var result = DiagnosticResultBuilder.CreateAt("TEST001", "Test message", DiagnosticSeverity.Info, 5, 10);

        // Assert
        Assert.Equal("TEST001", result.Id);
        Assert.Equal("Test message", result.Message);
        Assert.Equal(DiagnosticSeverity.Info, result.Severity);
        Assert.True(result.Spans.Length == 1);
    }
}
