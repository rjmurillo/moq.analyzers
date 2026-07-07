using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Build.Locator;
using Microsoft.Build.Utilities.ProjectCreation;

namespace Moq.Analyzers.EndToEnd.Test;

public sealed class MSBuildProjectCreatorEndToEndTests
{
    private static readonly string ExpectedDiagnosticId = "Moq1000";
    private static readonly string MoqVersion = "4.18.4";
    private static readonly object MSBuildRegistrationGate = new();

    [Fact]
    public async Task BuildEmitsExpectedDiagnosticForSealedClassMock()
    {
        const string source = """
            using Moq;

            namespace ConsumerProject;

            public sealed class SealedService
            {
            }

            public static class Tests
            {
                public static void CreateMock()
                {
                    _ = new Mock<SealedService>();
                }
            }
            """;

        BuildResult result = await BuildScenarioAsync(
            "sealed-class-mock",
            includeMoq: true,
            source);

        AssertBuildSucceeded(result);
        AssertAnalyzerLoaded(result);
        Assert.Contains(ExpectedDiagnosticId, result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BuildDoesNotEmitDiagnosticForInterfaceMock()
    {
        const string source = """
            using Moq;

            namespace ConsumerProject;

            public interface IService
            {
                void Execute();
            }

            public static class Tests
            {
                public static void CreateMock()
                {
                    _ = new Mock<IService>();
                }
            }
            """;

        BuildResult result = await BuildScenarioAsync(
            "interface-mock",
            includeMoq: true,
            source);

        AssertBuildSucceeded(result);
        AssertAnalyzerLoaded(result);
        Assert.DoesNotContain(ExpectedDiagnosticId, result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BuildDoesNotEmitDiagnosticWhenMoqIsNotReferenced()
    {
        const string source = """
            namespace ConsumerProject;

            public static class Tests
            {
                public static string Value => "No Moq usage";
            }
            """;

        BuildResult result = await BuildScenarioAsync(
            "no-moq-reference",
            includeMoq: false,
            source);

        AssertBuildSucceeded(result);
        AssertAnalyzerLoaded(result);
        Assert.DoesNotContain(ExpectedDiagnosticId, result.Output, StringComparison.Ordinal);
    }

    private static async Task<BuildResult> BuildScenarioAsync(string scenarioName, bool includeMoq, string source)
    {
        EnsureMSBuildRegistered();

        AnalyzerPackage analyzerPackage = FindAnalyzerPackage();
        string scenarioRoot = CreateScenarioRoot(scenarioName);
        WriteBuildIsolationFiles(scenarioRoot, analyzerPackage.Version);
        WriteNuGetConfig(scenarioRoot, analyzerPackage.Directory);

        string projectDirectory = Path.Combine(scenarioRoot, "ConsumerProject");
        Directory.CreateDirectory(projectDirectory);

        string projectPath = Path.Combine(projectDirectory, "ConsumerProject.csproj");
        CreateProjectFile(projectPath, includeMoq);

        await File.WriteAllTextAsync(Path.Combine(projectDirectory, "Consumer.cs"), source).ConfigureAwait(false);

        return await RunDotnetBuildAsync(projectPath, scenarioRoot).ConfigureAwait(false);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CreateProjectFile(string projectPath, bool includeMoq)
    {
        ProjectCreator project = ProjectCreator.Templates.SdkCsproj(path: projectPath, targetFramework: "net8.0")
            .Property("ImplicitUsings", "enable")
            .Property("Nullable", "enable")
            .ItemPackageReference("Moq.Analyzers");

        if (includeMoq)
        {
            project.ItemPackageReference("Moq");
        }

        project.Save();
    }

    private static void EnsureMSBuildRegistered()
    {
        lock (MSBuildRegistrationGate)
        {
            if (MSBuildLocator.IsRegistered)
            {
                return;
            }

            VisualStudioInstance instance = MSBuildLocator.RegisterDefaults();
            Debug.Assert(!string.IsNullOrEmpty(instance.MSBuildPath), "MSBuildLocator must resolve an MSBuild path.");
        }
    }

    private static AnalyzerPackage FindAnalyzerPackage()
    {
        DirectoryInfo outputDirectory = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory
            ?? throw new InvalidOperationException("Could not locate the end-to-end test output directory.");

        FileInfo package = outputDirectory.GetFiles("Moq.Analyzers*.nupkg")
            .Where(file => !file.Name.EndsWith(".symbols.nupkg", StringComparison.Ordinal))
            .OrderBy(file => file.Name, StringComparer.Ordinal)
            .LastOrDefault()
            ?? throw new InvalidOperationException("No Moq.Analyzers package was found in the end-to-end test output directory.");

        string version = GetPackageVersion(package.Name);
        Debug.Assert(package.DirectoryName != null, "Package files returned from a directory must expose DirectoryName.");

        return new AnalyzerPackage(package.DirectoryName, version);
    }

    private static string GetPackageVersion(string packageName)
    {
        const string prefix = "Moq.Analyzers.";
        const string suffix = ".nupkg";

        if (!packageName.StartsWith(prefix, StringComparison.Ordinal) ||
            !packageName.EndsWith(suffix, StringComparison.Ordinal) ||
            packageName.Length == prefix.Length + suffix.Length)
        {
            throw new InvalidOperationException($"Unexpected analyzer package file name: {packageName}");
        }

        return packageName[prefix.Length..^suffix.Length];
    }

    private static string CreateScenarioRoot(string scenarioName)
    {
        string root = Path.Combine(
            AppContext.BaseDirectory,
            "GeneratedProjects",
            $"{scenarioName}-{Guid.NewGuid():N}");

        Directory.CreateDirectory(root);
        return root;
    }

    private static void WriteBuildIsolationFiles(string scenarioRoot, string analyzerVersion)
    {
        const string emptyProject = """
            <Project>
            </Project>
            """;

        File.WriteAllText(Path.Combine(scenarioRoot, "Directory.Build.props"), emptyProject);
        File.WriteAllText(Path.Combine(scenarioRoot, "Directory.Build.targets"), emptyProject);

        string packagesProps = $$"""
            <Project>
              <PropertyGroup>
                <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
              </PropertyGroup>
              <ItemGroup>
                <PackageVersion Include="Moq" Version="{{MoqVersion}}" />
                <PackageVersion Include="Moq.Analyzers" Version="{{analyzerVersion}}" />
              </ItemGroup>
            </Project>
            """;

        File.WriteAllText(Path.Combine(scenarioRoot, "Directory.Packages.props"), packagesProps);
    }

    private static void WriteNuGetConfig(string scenarioRoot, string packageDirectory)
    {
        string nugetConfig = $$"""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <packageSources>
                <clear />
                <add key="local" value="{{packageDirectory}}" />
                <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
              </packageSources>
              <packageSourceMapping>
                <clear />
                <packageSource key="local">
                  <package pattern="Moq.Analyzers" />
                </packageSource>
                <packageSource key="nuget.org">
                  <package pattern="*" />
                </packageSource>
              </packageSourceMapping>
            </configuration>
            """;

        File.WriteAllText(Path.Combine(scenarioRoot, "nuget.config"), nugetConfig);
    }

    private static async Task<BuildResult> RunDotnetBuildAsync(string projectPath, string scenarioRoot)
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromMinutes(2));
        using Process process = CreateDotnetBuildProcess(projectPath, scenarioRoot);

        bool started = process.Start();
        Debug.Assert(started, "Process.Start returned false for a redirected dotnet build process.");

        Task<string> standardOutput = process.StandardOutput.ReadToEndAsync(timeout.Token);
        Task<string> standardError = process.StandardError.ReadToEndAsync(timeout.Token);

        try
        {
            await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }

            throw new TimeoutException("dotnet build did not complete within two minutes.");
        }

        string output = await standardOutput.ConfigureAwait(false);
        string error = await standardError.ConfigureAwait(false);

        return new BuildResult(process.ExitCode, output + error);
    }

    private static Process CreateDotnetBuildProcess(string projectPath, string scenarioRoot)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = "dotnet",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(projectPath)!,
        };

        startInfo.ArgumentList.Add("build");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add("--nologo");
        startInfo.ArgumentList.Add("--configuration");
        startInfo.ArgumentList.Add("Release");
        startInfo.ArgumentList.Add("--verbosity");
        startInfo.ArgumentList.Add("minimal");

        startInfo.Environment["DOTNET_CLI_HOME"] = Path.Combine(scenarioRoot, ".dotnet");
        startInfo.Environment["DOTNET_NOLOGO"] = "1";
        startInfo.Environment["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1";
        startInfo.Environment["NUGET_PACKAGES"] = Path.Combine(scenarioRoot, ".nuget", "packages");

        return new Process { StartInfo = startInfo };
    }

    private static void AssertBuildSucceeded(BuildResult result)
    {
        Assert.True(result.ExitCode == 0, result.Output);
    }

    private static void AssertAnalyzerLoaded(BuildResult result)
    {
        Assert.DoesNotContain("CS8032", result.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("AD0001", result.Output, StringComparison.Ordinal);
        Assert.DoesNotContain("could not load file or assembly", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record AnalyzerPackage(string Directory, string Version);

    private sealed record BuildResult(int ExitCode, string Output);
}
