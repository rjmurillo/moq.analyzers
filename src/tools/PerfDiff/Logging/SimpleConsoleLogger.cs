// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Frozen;
using Microsoft.Extensions.Logging;

namespace PerfDiff.Logging;

/// <summary>
/// Provides a simple console logger for structured logging output.
/// </summary>
internal sealed class SimpleConsoleLogger : ILogger
{
    private readonly Lock _gate = new();

    private readonly LogLevel _minimalLogLevel;
    private readonly LogLevel _minimalErrorLevel;

    private static readonly FrozenDictionary<LogLevel, ConsoleColor> LogLevelColorMap = new Dictionary<LogLevel, ConsoleColor>
    {
        [LogLevel.Critical] = ConsoleColor.Red,
        [LogLevel.Error] = ConsoleColor.Red,
        [LogLevel.Warning] = ConsoleColor.Yellow,
        [LogLevel.Information] = ConsoleColor.White,
        [LogLevel.Debug] = ConsoleColor.Gray,
        [LogLevel.Trace] = ConsoleColor.Gray,
        [LogLevel.None] = ConsoleColor.White,
    }.ToFrozenDictionary();

    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleConsoleLogger"/> class.
    /// </summary>
    /// <param name="minimalLogLevel">The minimal log level for output.</param>
    /// <param name="minimalErrorLevel">The minimal log level for error output.</param>
    public SimpleConsoleLogger(LogLevel minimalLogLevel, LogLevel minimalErrorLevel)
    {
        _minimalLogLevel = minimalLogLevel;
        _minimalErrorLevel = minimalErrorLevel;
    }

    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        lock (_gate)
        {
            string message = formatter(state, exception);
            bool logToErrorStream = logLevel >= _minimalErrorLevel;
            ConsoleColor messageColor = LogLevelColorMap[logLevel];

            Console.ForegroundColor = messageColor;
            try
            {
                LogToConsole(message, logToErrorStream);
            }
            finally
            {
                Console.ResetColor();
            }
        }
    }

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel)
    {
        return (int)logLevel >= (int)_minimalLogLevel;
    }

    /// <inheritdoc/>
    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull
    {
        return NullScope.Instance;
    }

    private static void LogToConsole(string message, bool logToErrorStream)
    {
        if (logToErrorStream)
        {
            Console.Error.Write($"{message}{Environment.NewLine}");
        }
        else
        {
            Console.Out.Write($"  {message}{Environment.NewLine}");
        }
    }
}
