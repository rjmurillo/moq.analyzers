// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.CommandLine;
using Microsoft.Extensions.Logging;

namespace PerfDiff.Logging;

/// <summary>
/// Provides a logger provider for the simple console logger.
/// </summary>
internal sealed class SimpleConsoleLoggerProvider : ILoggerProvider
{
    private readonly IConsole _console;
    private readonly LogLevel _minimalLogLevel;
    private readonly LogLevel _minimalErrorLevel;

    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleConsoleLoggerProvider"/> class.
    /// </summary>
    /// <param name="console">The console to write output to.</param>
    /// <param name="minimalLogLevel">The minimal log level for output.</param>
    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleConsoleLoggerProvider"/> class with the specified console and minimal log levels.
    /// </summary>
    /// <param name="console">The console instance used for output.</param>
    /// <param name="minimalLogLevel">The minimal log level for general output.</param>
    /// <param name="minimalErrorLevel">The minimal log level for error output.</param>
    public SimpleConsoleLoggerProvider(IConsole console, LogLevel minimalLogLevel, LogLevel minimalErrorLevel)
    {
        _console = console;
        _minimalLogLevel = minimalLogLevel;
        _minimalErrorLevel = minimalErrorLevel;
    }

    /// <summary>
    /// Creates a new <see cref="SimpleConsoleLogger"/> instance for the specified category.
    /// </summary>
    /// <param name="categoryName">The category name for messages produced by the logger.</param>
    /// <returns>A <see cref="SimpleConsoleLogger"/> configured with the provider's console and log level settings.</returns>
    public ILogger CreateLogger(string categoryName)
    {
        return new SimpleConsoleLogger(_console, _minimalLogLevel, _minimalErrorLevel);
    }

    /// <summary>
    /// Releases resources used by the logger provider. No action is required for this implementation.
    /// </summary>
    public void Dispose()
    {
    }
}
