using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Model;
using Microsoft.VisualStudio.Composition;

namespace Moq.Analyzers.Benchmarks.Helpers;

// Originally from https://github.com/dotnet/roslyn-analyzers/blob/f1115edce8633ebe03a86191bc05c6969ed9a821/src/PerformanceTests/Utilities/Common/CompilationHelper.cs
// See https://github.com/dotnet/roslyn-sdk/issues/1165 for discussion on providing these or similar helpers in the testing packages.
internal static class CompilationCreator
{
    private static readonly ReferenceAssemblies ReferenceAssemblies = ReferenceAssemblies.Net.Net80.AddPackages([new PackageIdentity("Moq", "4.18.4")]);

    [SuppressMessage("Maintainability", "AV1500:Member or local function contains too many statements", Justification = "Minimizing divergence from upstream code.")]
    public static async Task<(Project Project, AnalyzerOptions Options)> CreateProjectAsync(
        (string, string)[] sourceFiles,
        (string, string)[]? globalOptions,
        string name,
        string language,
        string defaultPrefix,
        string defaultExtension,
        CompilationOptions compilationOptions,
        ParseOptions parseOptions)
    {
        ProjectState projectState = new ProjectState(name, language, defaultPrefix, defaultExtension);
        foreach ((string filename, string content) in sourceFiles)
        {
            projectState.Sources.Add((defaultPrefix + filename + "." + defaultExtension, content));
        }

        EvaluatedProjectState evaluatedProj = new EvaluatedProjectState(projectState, ReferenceAssemblies);

        Project project = await CreateProjectAsync(evaluatedProj, compilationOptions, parseOptions).ConfigureAwait(false);

        if (globalOptions is not null)
        {
            OptionsProvider optionsProvider = new(globalOptions);
            AnalyzerOptions options = new(ImmutableArray<AdditionalText>.Empty, optionsProvider);

            return (project, options);
        }

        return (project, project.AnalyzerOptions);
    }

    [SuppressMessage("Maintainability", "AV1500:Member or local function contains too many statements", Justification = "Minimizing divergence with upstream code")]
    [SuppressMessage("Maintainability", "AV1551:Method overload should call another overload", Justification = "Minimizing divergence with upstream code")]
    [SuppressMessage("Maintainability", "AV1555:Avoid using non-(nullable-)boolean named arguments", Justification = "Minimizing divergence with upstream code")]
    private static async Task<Project> CreateProjectAsync(
        EvaluatedProjectState primaryProject,
        CompilationOptions compilationOptions,
        ParseOptions parseOptions)
    {
        ProjectId projectId = ProjectId.CreateNewId(debugName: primaryProject.Name);
        Solution solution = await CreateSolutionAsync(projectId, primaryProject, compilationOptions, parseOptions).ConfigureAwait(false);

        foreach ((string newFileName, Microsoft.CodeAnalysis.Text.SourceText source) in primaryProject.Sources)
        {
            DocumentId documentId = DocumentId.CreateNewId(projectId, debugName: newFileName);
            solution = solution.AddDocument(documentId, newFileName, source, filePath: newFileName);
        }

        foreach ((string newFileName, Microsoft.CodeAnalysis.Text.SourceText source) in primaryProject.AdditionalFiles)
        {
            DocumentId documentId = DocumentId.CreateNewId(projectId, debugName: newFileName);
            solution = solution.AddAdditionalDocument(documentId, newFileName, source, filePath: newFileName);
        }

        foreach ((string newFileName, Microsoft.CodeAnalysis.Text.SourceText source) in primaryProject.AnalyzerConfigFiles)
        {
            DocumentId documentId = DocumentId.CreateNewId(projectId, debugName: newFileName);
            solution = solution.AddAnalyzerConfigDocument(documentId, newFileName, source, filePath: newFileName);
        }

        return solution.GetProject(projectId)!;
    }

    [SuppressMessage("Maintainability", "AV1500:Member or local function contains too many statements", Justification = "Minimizing divergence from upstream")]
    [SuppressMessage("Maintainability", "AV1561:Signature contains too many parameters", Justification = "Minimizing divergence from upstream")]
    private static async Task<Solution> CreateSolutionAsync(
        ProjectId projectId,
        EvaluatedProjectState projectState,
        CompilationOptions compilationOptions,
        ParseOptions parseOptions)
    {
        ReferenceAssemblies referenceAssemblies = projectState.ReferenceAssemblies ?? ReferenceAssemblies.Default;

        compilationOptions = compilationOptions
            .WithOutputKind(projectState.OutputKind)
            .WithAssemblyIdentityComparer(referenceAssemblies.AssemblyIdentityComparer);

        parseOptions = parseOptions
            .WithDocumentationMode(projectState.DocumentationMode);

        AsyncLazy<IExportProviderFactory> exportProviderFactory = new(
            async () =>
            {
                AttributedPartDiscovery discovery = new(Resolver.DefaultInstance, isNonPublicSupported: true);
                DiscoveredParts parts = await discovery.CreatePartsAsync(MefHostServices.DefaultAssemblies).ConfigureAwait(false);
                ComposableCatalog catalog = ComposableCatalog.Create(Resolver.DefaultInstance).AddParts(parts);

                CompositionConfiguration configuration = CompositionConfiguration.Create(catalog);
                RuntimeComposition runtimeComposition = RuntimeComposition.CreateRuntimeComposition(configuration);
                return runtimeComposition.CreateExportProviderFactory();
            },
            CancellationToken.None);
        ExportProvider exportProvider = (await exportProviderFactory).CreateExportProvider();
        MefHostServices host = MefHostServices.Create(exportProvider.AsCompositionContext());
        AdhocWorkspace workspace = new AdhocWorkspace(host);

        Solution solution = workspace
            .CurrentSolution
            .AddProject(projectId, projectState.Name, projectState.Name, projectState.Language)
            .WithProjectCompilationOptions(projectId, compilationOptions)
            .WithProjectParseOptions(projectId, parseOptions);

        ImmutableArray<MetadataReference> metadataReferences = await referenceAssemblies.ResolveAsync(projectState.Language, CancellationToken.None).ConfigureAwait(false);
        solution = solution.AddMetadataReferences(projectId, metadataReferences);

        return solution;
    }

    /// <summary>
    /// This class just passes argument through to the projects options provider and it used to provider custom global options.
    /// </summary>
    private sealed class OptionsProvider : AnalyzerConfigOptionsProvider
    {
        public OptionsProvider((string, string)[] globalOptions)
        {
            GlobalOptions = new ConfigOptions(globalOptions);
        }

        public override AnalyzerConfigOptions GlobalOptions { get; }

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
            => GlobalOptions;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
            => GlobalOptions;
    }

    /// <summary>
    /// Allows adding additional global options.
    /// </summary>
    private sealed class ConfigOptions : AnalyzerConfigOptions
    {
        private readonly Dictionary<string, string> _globalOptions;

        public ConfigOptions((string, string)[] globalOptions)
            => _globalOptions = globalOptions.ToDictionary(kvp => kvp.Item1, kvp => kvp.Item2, StringComparer.OrdinalIgnoreCase);

        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
            => _globalOptions.TryGetValue(key, out value);
    }
}
