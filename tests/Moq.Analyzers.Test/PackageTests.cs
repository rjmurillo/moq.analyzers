using System.Reflection;

namespace Moq.Analyzers.Test;

public class PackageTests
{
    public static TheoryData<FileInfo> GetPackages()
    {
        DirectoryInfo directory = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory!;
        FileInfo[] packages = directory.GetFiles("Moq.Analyzers*.nupkg")
            .OrderBy(fileInfo => fileInfo.Name, StringComparer.Ordinal)
            .ToArray();

        TheoryData<FileInfo> theoryData = new();
        foreach (FileInfo package in packages)
        {
            theoryData.Add(package);
        }

        return theoryData;
    }

    [Theory]
    [MemberData(nameof(GetPackages))]
    public Task Baseline(FileInfo package)
    {
        // Use a stable discriminator based on package type, not version/hash
        string discriminator = package.Name.Contains(".symbols.nupkg", StringComparison.Ordinal)
            ? "symbols"
            : "main";

        return VerifyFile(package)
            .ScrubNuspec()
            .UseTextForParameters(discriminator);
    }
}
