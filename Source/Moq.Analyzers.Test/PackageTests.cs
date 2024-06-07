using System.Reflection;

namespace Moq.Analyzers.Test;

public class PackageTests
{
    private static readonly FileInfo Package;

    static PackageTests()
    {
        Package = new FileInfo(Assembly.GetExecutingAssembly().Location)
            .Directory!
            .GetFiles("Moq.Analyzers*.nupkg")
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .First();
    }

    [Fact]
    public Task Baseline()
    {
        return VerifyFile(Package).ScrubNuspec();
    }
}
