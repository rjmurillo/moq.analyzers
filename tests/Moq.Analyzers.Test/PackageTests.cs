using System.Reflection;

namespace Moq.Analyzers.Test;

public class PackageTests
{
    private static readonly FileInfo Package = new FileInfo(Assembly.GetExecutingAssembly().Location)
        .Directory!
        .GetFiles("Moq.Analyzers*.nupkg")
        .OrderByDescending(fileInfo => fileInfo.LastWriteTimeUtc)
        .First();

    [Fact]
    public Task Baseline()
    {
        return VerifyFile(Package).ScrubNuspec();
    }
}
