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
    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleConsoleLogger"/> class with the specified console and log level thresholds.
    /// </summary>
    /// <param name="console">The console interface used for output.</param>
    /// <param name="minimalLogLevel">The minimum log level required for messages to be logged.</param>
    /// <param name="minimalErrorLevel">The minimum log level at which messages are logged to the error stream.</param>
    public SimpleConsoleLogger(IConsole console, LogLevel minimalLogLevel, LogLevel minimalErrorLevel)
    {
        _terminal = console.GetTerminal();
        _console = console;
        _minimalLogLevel = minimalLogLevel;
        _minimalErrorLevel = minimalErrorLevel;
    }

    /// <summary>
    /// Logs a formatted message to the console or terminal if the specified log level is enabled.
    /// </summary>
    /// <typeparam name="TState">The type of the state object to be logged.</typeparam>
    /// <param name="logLevel">The severity level of the log message.</param>
    /// <param name="eventId">The identifier for the log event.</param>
    /// <param name="state">The state object containing log data.</param>
    /// <param name="exception">An optional exception associated with the log entry.</param>
    /// <param name="formatter">A function to create a log message string from the state and exception.</param>
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

    /// <summary>
    /// Determines whether logging is enabled for the specified log level.
    /// </summary>
    /// <param name="logLevel">The log level to check.</param>
    /// <returns><c>true</c> if the log level is enabled; otherwise, <c>false</c>.</returns>
    public bool IsEnabled(LogLevel logLevel)
    {
        return (int)logLevel >= (int)_minimalLogLevel;
    }

    /// <summary>
    /// Returns a disposable object representing a no-op logging scope.
    /// </summary>
    /// <typeparam name="TState">The type of the state to associate with the scope.</typeparam>
    /// <returns>A disposable that does nothing when disposed.</returns>
    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull
    {
        return NullScope.Instance;
    }

    /// <summary>
    /// Writes a log message to the terminal with color formatting based on the log level.
    /// </summary>
    /// <param name="message">The log message to output.</param>
    /// <param name="logLevel">The severity level of the log message.</param>
    /// <param name="logToErrorStream">Indicates whether to write the message to the error stream.</param>
    private void LogToTerminal(string message, LogLevel logLevel, bool logToErrorStream)
    {
        ConsoleColor messageColor = LogLevelColorMap[logLevel];
        _terminal.ForegroundColor = messageColor;

        LogToConsole(_terminal, message, logToErrorStream);

        _terminal.ResetColor();
    }

    /// <summary>
    /// Writes a log message to the console's standard output or error stream.
    /// </summary>
    /// <param name="console">The console interface used for output.</param>
    /// <param name="message">The log message to write.</param>
    /// <param name="logToErrorStream">If true, writes to the error stream; otherwise, writes to standard output.</param>
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
