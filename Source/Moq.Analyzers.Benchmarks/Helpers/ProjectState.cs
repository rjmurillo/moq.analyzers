using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;

namespace Moq.Analyzers.Benchmarks.Helpers;

// Originally from https://github.com/dotnet/roslyn-analyzers/blob/f1115edce8633ebe03a86191bc05c6969ed9a821/src/PerformanceTests/Utilities/Common/ProjectState.cs
internal record ProjectState
{
    public static ProjectState Create(string name, string language, string defaultPrefix, string defaultExtension)
    {
        return new ProjectState
        {
            Name = name,
            Language = language,
            DefaultPrefix = defaultPrefix,
            DefaultExtension = defaultExtension,
            Sources = new SourceFileList(defaultPrefix, defaultExtension),
        };
    }

    public string Name { get; init; }

    public string Language { get; init; }

    public SourceFileList Sources { get; init; }

    private protected string DefaultPrefix { get; init; }

    private protected string DefaultExtension { get; init; }

    public string AssemblyName => Name;

    public ReferenceAssemblies? ReferenceAssemblies { get; set; }

    public OutputKind? OutputKind { get; set; }

    public DocumentationMode? DocumentationMode { get; set; }

    public SourceFileCollection GeneratedSources { get; } = new SourceFileCollection();

    public SourceFileCollection AdditionalFiles { get; } = new SourceFileCollection();

    public SourceFileCollection AnalyzerConfigFiles { get; } = new SourceFileCollection();

    public List<Func<IEnumerable<(string filename, SourceText content)>>> AdditionalFilesFactories { get; } = new List<Func<IEnumerable<(string filename, SourceText content)>>>();

    public List<string> AdditionalProjectReferences { get; } = [];
}
