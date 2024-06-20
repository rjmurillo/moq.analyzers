using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Moq.Analyzers.Benchmarks.Helpers;

// Originally from https://github.com/dotnet/roslyn-analyzers/blob/f1115edce8633ebe03a86191bc05c6969ed9a821/src/PerformanceTests/Utilities/Common/SourceFileCollection.cs
internal class SourceFileCollection : List<(string filename, SourceText content)>
{
    public void Add((string filename, string content) file)
    {
        Add((file.filename, SourceText.From(file.content)));
    }

    public void Add((Type sourceGeneratorType, string filename, string content) file)
    {
        var contentWithEncoding = SourceText.From(file.content, Encoding.UTF8);
        Add((file.sourceGeneratorType, file.filename, contentWithEncoding));
    }

    public void Add((Type sourceGeneratorType, string filename, SourceText content) file)
    {
        var generatedPath = Path.Combine(file.sourceGeneratorType.GetTypeInfo().Assembly.GetName().Name ?? string.Empty, file.sourceGeneratorType.FullName!, file.filename);
        Add((generatedPath, file.content));
    }
}
