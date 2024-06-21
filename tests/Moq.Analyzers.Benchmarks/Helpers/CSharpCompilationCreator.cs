using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Moq.Analyzers.Benchmarks.Helpers;

namespace Moq.Analyzers.Benchmarks;

// Originally from https://github.com/dotnet/roslyn-analyzers/blob/f1115edce8633ebe03a86191bc05c6969ed9a821/src/PerformanceTests/Utilities/CSharp/CSharpCompilationHelper.cs
// See https://github.com/dotnet/roslyn-sdk/issues/1165 for discussion on providing these or similar helpers in the testing packages.
internal static class CSharpCompilationCreator
{
    public static async Task<Compilation?> CreateAsync((string, string)[] sourceFiles)
    {
        (Project project, _) = await CreateProjectAsync(sourceFiles, globalOptions: null).ConfigureAwait(false);
        return await project.GetCompilationAsync().ConfigureAwait(false);
    }

    public static async Task<(Compilation? Compilation, AnalyzerOptions Options)> CreateWithOptionsAsync((string, string)[] sourceFiles, (string, string)[] globalOptions)
    {
        (Project project, AnalyzerOptions options) = await CreateProjectAsync(sourceFiles, globalOptions).ConfigureAwait(false);
        return (await project.GetCompilationAsync().ConfigureAwait(false), options);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "AV1553:Do not use optional parameters with default value null for strings, collections or tasks", Justification = "Minimizing divergence from upstream code")]
    private static Task<(Project Project, AnalyzerOptions Options)> CreateProjectAsync((string, string)[] sourceFiles, (string, string)[]? globalOptions = null)
        => CompilationCreator.CreateProjectAsync(
            sourceFiles,
            globalOptions,
            "TestProject",
            LanguageNames.CSharp,
            "/0/Test",
            "cs",
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
            new CSharpParseOptions(LanguageVersion.Default));
}
