// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace PerfDiff.Tests;

/// <summary>
/// Verifies TryGetETLPaths behavior: FirstOrDefault selection and warning on multiple files.
/// TryGetETLPaths is private; accessed via reflection.
/// </summary>
public class EtlPathTests
{
    private static readonly MethodInfo TryGetETLPaths;

#pragma warning disable ECS0600 // Cannot use nameof on a private method in another assembly
#pragma warning disable ECS1200 // Assignment in static constructor is required here; field initializer cannot throw meaningfully
#pragma warning disable S3963  // Static constructor is required to throw on missing method
    static EtlPathTests()
    {
        TryGetETLPaths = typeof(PerfDiff).GetMethod(
            "TryGetETLPaths",
            BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("TryGetETLPaths not found.");
    }
#pragma warning restore S3963
#pragma warning restore ECS1200
#pragma warning restore ECS0600

    /// <summary>
    /// With a single ETL file, returns that file and no warning is logged.
    /// </summary>
    [Fact]
    public void TryGetETLPaths_SingleFile_ReturnsTrueWithPath()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        string etlFile = Path.Combine(tempDir, "trace.etl.zip");
        File.WriteAllText(etlFile, string.Empty);

        try
        {
            CapturingLogger logger = new();
            object?[] parameters = [tempDir, logger, null];
#pragma warning disable ECS0900 // Reflection Invoke returns object; unboxing is unavoidable
            bool result = (bool)TryGetETLPaths.Invoke(null, parameters)!;
#pragma warning restore ECS0900
            string? etlPath = (string?)parameters[2];

            Assert.True(result);
            Assert.Equal(etlFile, etlPath);
            Assert.Empty(logger.Warnings);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    /// <summary>
    /// With multiple ETL files, returns the first one and logs exactly one warning.
    /// </summary>
    [Fact]
    public void TryGetETLPaths_MultipleFiles_ReturnsFirstAndLogsWarning()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        // Create two ETL files; GetFiles ordering is filesystem-dependent.
        string etlFile1 = Path.Combine(tempDir, "trace_a.etl.zip");
        string etlFile2 = Path.Combine(tempDir, "trace_b.etl.zip");
        File.WriteAllText(etlFile1, string.Empty);
        File.WriteAllText(etlFile2, string.Empty);

        try
        {
            CapturingLogger logger = new();
            object?[] parameters = [tempDir, logger, null];
#pragma warning disable ECS0900 // Reflection Invoke returns object; unboxing is unavoidable
            bool result = (bool)TryGetETLPaths.Invoke(null, parameters)!;
#pragma warning restore ECS0900
            string? etlPath = (string?)parameters[2];

            Assert.True(result);
            Assert.NotNull(etlPath);
            Assert.EndsWith("etl.zip", etlPath, StringComparison.OrdinalIgnoreCase);
            Assert.Single(logger.Warnings);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    /// <summary>
    /// With no ETL files, returns false and out param is null.
    /// </summary>
    [Fact]
    public void TryGetETLPaths_NoFiles_ReturnsFalse()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        try
        {
            CapturingLogger logger = new();
            object?[] parameters = [tempDir, logger, null];
#pragma warning disable ECS0900 // Reflection Invoke returns object; unboxing is unavoidable
            bool result = (bool)TryGetETLPaths.Invoke(null, parameters)!;
#pragma warning restore ECS0900

            Assert.False(result);
            Assert.Null(parameters[2]);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    /// <summary>
    /// Minimal ILogger implementation that captures warning messages for assertion.
    /// </summary>
    private sealed class CapturingLogger : ILogger
    {
        public List<string> Warnings { get; } = [];

        /// <inheritdoc/>
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => null;

        /// <inheritdoc/>
        public bool IsEnabled(LogLevel logLevel) => true;

        /// <inheritdoc/>
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                Warnings.Add(formatter(state, exception));
            }
        }
    }
}
