using Microsoft.CodeAnalysis.Text;

namespace Moq.Analyzers.Benchmarks.Helpers;

// Originally from https://github.com/dotnet/roslyn-analyzers/blob/f1115edce8633ebe03a86191bc05c6969ed9a821/src/PerformanceTests/Utilities/Common/SourceFileList.cs

internal class SourceFileList : SourceFileCollection
{
    private readonly string _defaultPrefix;
    private readonly string _defaultExtension;

    public SourceFileList(string defaultPrefix, string defaultExtension)
    {
        _defaultPrefix = defaultPrefix;
        _defaultExtension = defaultExtension;
    }

    public void Add(string content)
    {
        Add(($"{_defaultPrefix}{Count}.{_defaultExtension}", content));
    }

    public void Add(SourceText content)
    {
        Add(($"{_defaultPrefix}{Count}.{_defaultExtension}", content));
    }
}
