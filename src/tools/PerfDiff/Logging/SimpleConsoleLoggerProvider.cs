// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;

namespace PerfDiff.Logging;

/// <summary>
/// Provides a logger provider for the simple console logger.
/// </summary>
internal sealed class SimpleConsoleLoggerProvider : ILoggerProvider
{
    private readonly SimpleConsoleLogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleConsoleLoggerProvider"/> class.
    /// </summary>
    /// <param name="minimalLogLevel">The minimal log level for output.</param>
    /// <param name="minimalErrorLevel">The minimal log level for error output.</param>
    public SimpleConsoleLoggerProvider(LogLevel minimalLogLevel, LogLevel minimalErrorLevel)
    {
        _logger = new SimpleConsoleLogger(minimalLogLevel, minimalErrorLevel);
    }

    /// <inheritdoc/>
    public ILogger CreateLogger(string categoryName)
    {
        return _logger;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
    }
}
