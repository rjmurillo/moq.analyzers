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
    /// <param name="minimalErrorLevel">The minimal log level for error output.</param>
    public SimpleConsoleLoggerProvider(IConsole console, LogLevel minimalLogLevel, LogLevel minimalErrorLevel)
    {
        _console = console;
        _minimalLogLevel = minimalLogLevel;
        _minimalErrorLevel = minimalErrorLevel;
    }

    /// <inheritdoc/>
    public ILogger CreateLogger(string categoryName)
    {
        return new SimpleConsoleLogger(_console, _minimalLogLevel, _minimalErrorLevel);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
    }
}
