// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;
using Xunit;

namespace PerfDiff.Tests;

/// <summary>
/// Verifies DiffCommand CLI contract: verbosity mapping and option configuration.
/// </summary>
public class DiffCommandTests
{
#pragma warning disable ECS0900 // Minimize boxing and unboxing - xUnit InlineData requires object parameters
    [Theory]
    [InlineData("q", LogLevel.Error)]
    [InlineData("quiet", LogLevel.Error)]
    [InlineData("m", LogLevel.Warning)]
    [InlineData("minimal", LogLevel.Warning)]
    [InlineData("n", LogLevel.Information)]
    [InlineData("normal", LogLevel.Information)]
    [InlineData("d", LogLevel.Debug)]
    [InlineData("detailed", LogLevel.Debug)]
    [InlineData("diag", LogLevel.Trace)]
    [InlineData("diagnostic", LogLevel.Trace)]
#pragma warning restore ECS0900
    public void GetLogLevel_KnownVerbosity_ReturnsMappedLevel(string verbosity, LogLevel expected)
    {
        LogLevel actual = DiffCommand.GetLogLevel(verbosity);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown")]
    public void GetLogLevel_UnknownOrNullVerbosity_ReturnsInformation(string? verbosity)
    {
        LogLevel actual = DiffCommand.GetLogLevel(verbosity);
        Assert.Equal(LogLevel.Information, actual);
    }

    [Fact]
    public void CreateCommandLineOptions_RootCommand_HasExpectedOptions()
    {
        System.CommandLine.RootCommand cmd = DiffCommand.CreateCommandLineOptions();

        Assert.NotNull(cmd);
        Assert.NotNull(DiffCommand.BaselineOption);
        Assert.NotNull(DiffCommand.ResultsOption);
        Assert.NotNull(DiffCommand.VerbosityOption);
        Assert.NotNull(DiffCommand.FailOnRegressionOption);
    }
}
