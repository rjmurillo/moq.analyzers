// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.CommandLine;

using Microsoft.Extensions.Logging;

namespace PerfDiff.Logging;

internal static class SimpleConsoleLoggerFactoryExtensions
{
    /// <summary>
    /// Adds a <see cref="SimpleConsoleLoggerProvider"/> to the specified <see cref="ILoggerFactory"/> with the given console and log level settings.
    /// </summary>
    /// <param name="factory">The logger factory to which the provider will be added.</param>
    /// <param name="console">The console implementation used for logging output.</param>
    /// <param name="minimalLogLevel">The minimum log level for messages to be logged.</param>
    /// <param name="minimalErrorLevel">The minimum log level for messages to be treated as errors.</param>
    /// <returns>The <see cref="ILoggerFactory"/> instance with the provider added.</returns>
    public static ILoggerFactory AddSimpleConsole(this ILoggerFactory factory, IConsole console, LogLevel minimalLogLevel, LogLevel minimalErrorLevel)
    {
        factory.AddProvider(new SimpleConsoleLoggerProvider(console, minimalLogLevel, minimalErrorLevel));
        return factory;
    }
}
