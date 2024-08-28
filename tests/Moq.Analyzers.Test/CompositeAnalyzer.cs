using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Moq.Analyzers.Test;

/// <summary>
/// A "meta" analyzer that aggregates all the individual analyzers into a single one.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CompositeAnalyzer : DiagnosticAnalyzer
{
    private readonly ImmutableArray<DiagnosticAnalyzer> _analyzers;
    private readonly ImmutableArray<DiagnosticDescriptor> _supportedDiagnostics;

    /// <summary>Initializes a new instance of the <see cref="CompositeAnalyzer" /> class.</summary>
    public CompositeAnalyzer()
    {
        _analyzers = [.. DiagnosticAnalyzers()];
#pragma warning disable ECS0900 // Consider using an alternative implementation to avoid boxing and unboxing
        _supportedDiagnostics = [.. _analyzers.SelectMany(diagnosticAnalyzer => diagnosticAnalyzer.SupportedDiagnostics)];
#pragma warning restore ECS0900
    }

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => _supportedDiagnostics;

    /// <inheritdoc />
    [System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1026:Enable concurrent execution", Justification = "Delegated off to children")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1025:Configure generated code analysis", Justification = "Delegated off to children")]
    public override void Initialize(AnalysisContext context)
    {
        foreach (DiagnosticAnalyzer analyzer in _analyzers)
        {
            analyzer.Initialize(context);
        }
    }

    private static IEnumerable<DiagnosticAnalyzer> DiagnosticAnalyzers()
    {
        Type diagnosticAnalyzerType = typeof(DiagnosticAnalyzer);
        IEnumerable<Type> diagnosticAnalyzerTypes = typeof(ConstructorArgumentsShouldMatchAnalyzer)
                .Assembly
                .GetTypes()
                .Where(type => diagnosticAnalyzerType.IsAssignableFrom(type) && !type.IsAbstract && type.IsClass)
            ;

        return diagnosticAnalyzerTypes
                .Select(type => (DiagnosticAnalyzer?)Activator.CreateInstance(type))
                .Where(analyzer => analyzer != null)
                .Cast<DiagnosticAnalyzer>()
            ;
    }
}
