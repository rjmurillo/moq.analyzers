// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.CommandLine;
using System.CommandLine.Rendering;
using Microsoft.Extensions.Logging;

namespace PerfDiff.Logging;

/// <summary>
/// Provides a simple console logger for structured logging output.
/// </summary>
internal sealed class SimpleConsoleLogger : ILogger
{
    private readonly Lock _gate = new();

    private readonly IConsole _console;
    private readonly ITerminal _terminal;
    private readonly LogLevel _minimalLogLevel;
    private readonly LogLevel _minimalErrorLevel;

    private static ImmutableDictionary<LogLevel, ConsoleColor> LogLevelColorMap => new Dictionary<LogLevel, ConsoleColor>
    {
        [LogLevel.Critical] = ConsoleColor.Red,
        [LogLevel.Error] = ConsoleColor.Red,
        [LogLevel.Warning] = ConsoleColor.Yellow,
        [LogLevel.Information] = ConsoleColor.White,
        [LogLevel.Debug] = ConsoleColor.Gray,
        [LogLevel.Trace] = ConsoleColor.Gray,
        [LogLevel.None] = ConsoleColor.White,
    }.ToImmutableDictionary();

    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleConsoleLogger"/> class.
    /// </summary>
    /// <param name="console">The console to write output to.</param>
    /// <param name="minimalLogLevel">The minimal log level for output.</param>
    /// <param name="minimalErrorLevel">The minimal log level for error output.</param>
    public SimpleConsoleLogger(IConsole console, LogLevel minimalLogLevel, LogLevel minimalErrorLevel)
    {
        _terminal = console.GetTerminal();
        _console = console;
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
            if (_terminal is null)
            {
                LogToConsole(_console, message, logToErrorStream);
            }
            else
            {
                LogToTerminal(message, logLevel, logToErrorStream);
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

    private void LogToTerminal(string message, LogLevel logLevel, bool logToErrorStream)
    {
        ConsoleColor messageColor = LogLevelColorMap[logLevel];
        _terminal.ForegroundColor = messageColor;

        LogToConsole(_terminal, message, logToErrorStream);

        _terminal.ResetColor();
    }

    private static void LogToConsole(IConsole console, string message, bool logToErrorStream)
    {
        if (logToErrorStream)
        {
            console.Error.Write($"{message}{Environment.NewLine}");
        }
        else
        {
            console.Out.Write($"  {message}{Environment.NewLine}");
        }
    }
}
