using System.Reflection;

namespace Moq.Analyzers.Test;

public class PackageTests
{
    public static TheoryData<string> GetPackages()
    {
        DirectoryInfo directory = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory!;
        FileInfo[] packages = directory.GetFiles("Moq.Analyzers*.nupkg")
            .OrderBy(fileInfo => fileInfo.Name, StringComparer.Ordinal)
            .ToArray();

        if (packages.Length == 0)
        {
            throw new InvalidOperationException("No Moq.Analyzers*.nupkg files were found. Ensure the pack step runs before executing this test.");
        }

        TheoryData<string> theoryData = new();
        foreach (FileInfo package in packages)
        {
            theoryData.Add(package.FullName);
        }

        return theoryData;
    }

    [Theory]
    [MemberData(nameof(GetPackages))]
    public Task Baseline(string packagePath)
    {
        // xUnit requires theory data to be serializable. FileInfo is not serializable,
        // so we pass the path as a string and reconstruct the FileInfo here.
        FileInfo package = new(packagePath);

        // Use a stable discriminator based on package type, not version/hash
        string discriminator = package.Name.Contains(".symbols.nupkg", StringComparison.Ordinal)
            ? "symbols"
            : "main";

        return VerifyFile(package)
            .ScrubNuspec()
            .UseTextForParameters(discriminator);
    }
}
