// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace PerfDiff.Tests;

/// <summary>
/// Verifies the DI registration pattern used by Program.SetupLogging resolves correctly.
///
/// The fix changed:
///   serviceCollection.AddSingleton(new LoggerFactory()...)
/// to:
///   serviceCollection.AddSingleton&lt;ILoggerFactory&gt;(new LoggerFactory()...)
///
/// Without the type parameter, the instance is registered as LoggerFactory (concrete type),
/// not as ILoggerFactory (interface). AddLogging() then registers its own ILoggerFactory,
/// overwriting the custom one. GetRequiredService&lt;ILoggerFactory&gt;() returns the default
/// factory and the custom console provider is never used.
/// </summary>
public sealed class LoggingRegistrationTests
{
    /// <summary>
    /// GetRequiredService&lt;ILoggerFactory&gt;() must return the same instance that was registered.
    /// This is the core contract of the fix.
    /// </summary>
    [Fact]
    public void AddSingleton_WithInterfaceTypeParameter_ResolvesCustomFactory()
    {
        // Arrange: correct registration (the fix)
        LoggerFactory customFactory = BuildCustomFactory();

        ServiceCollection services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(customFactory);
        services.AddLogging();

        using ServiceProvider provider = services.BuildServiceProvider();

        // Act
        ILoggerFactory resolved = provider.GetRequiredService<ILoggerFactory>();

        // Assert: same instance -- the custom factory wins over the default AddLogging() one
        Assert.Same(customFactory, resolved);
    }

    /// <summary>
    /// GetRequiredService&lt;ILogger&lt;T&gt;&gt;() must resolve without throwing.
    /// </summary>
    [Fact]
    public void AddSingleton_WithInterfaceTypeParameter_ILoggerOfT_ResolvesSuccessfully()
    {
        // Arrange
        ServiceCollection services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(BuildCustomFactory());
        services.AddLogging();

        using ServiceProvider provider = services.BuildServiceProvider();

        // Act
        ILogger<LoggingRegistrationTests> logger = provider.GetRequiredService<ILogger<LoggingRegistrationTests>>();

        // Assert
        Assert.NotNull(logger);
    }

    /// <summary>
    /// The ILogger&lt;T&gt; resolved from the custom factory respects the configured minimum log level.
    /// This confirms the custom provider is active, not a no-op default.
    /// </summary>
    [Fact]
    public void ResolvedLogger_RespectsConfiguredMinimumLogLevel()
    {
        // Arrange: configure minimum level of Warning via the custom provider
        LoggerFactory customFactory = new LoggerFactory();
        customFactory.AddProvider(new MinLevelLoggerProvider(LogLevel.Warning));

        ServiceCollection services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(customFactory);
        services.AddLogging();

        using ServiceProvider provider = services.BuildServiceProvider();
        ILogger<LoggingRegistrationTests> logger = provider.GetRequiredService<ILogger<LoggingRegistrationTests>>();

        // Act + Assert: Warning and above enabled; Information and Debug not
        Assert.True(logger.IsEnabled(LogLevel.Warning));
        Assert.True(logger.IsEnabled(LogLevel.Error));
        Assert.False(logger.IsEnabled(LogLevel.Information));
        Assert.False(logger.IsEnabled(LogLevel.Debug));
    }

    /// <summary>
    /// Demonstrates the pre-fix regression: without &lt;ILoggerFactory&gt; type parameter,
    /// GetRequiredService&lt;ILoggerFactory&gt;() returns the default factory from AddLogging(),
    /// not the custom instance.
    /// </summary>
    [Fact]
    public void AddSingleton_WithoutInterfaceTypeParameter_DoesNotReturnCustomFactory()
    {
        // Arrange: BUG -- no <ILoggerFactory> type parameter
        LoggerFactory customFactory = BuildCustomFactory();

        ServiceCollection services = new ServiceCollection();
        services.AddSingleton(customFactory);   // registers as LoggerFactory, not ILoggerFactory
        services.AddLogging();                   // registers its own ILoggerFactory

        using ServiceProvider provider = services.BuildServiceProvider();

        // Act
        ILoggerFactory resolved = provider.GetRequiredService<ILoggerFactory>();

        // Assert: the default factory wins -- the custom one is invisible via the interface
        Assert.NotSame(customFactory, resolved);
    }

    private static LoggerFactory BuildCustomFactory()
    {
        LoggerFactory factory = new LoggerFactory();
        factory.AddProvider(new MinLevelLoggerProvider(LogLevel.Information));
        return factory;
    }

    /// <summary>
    /// A minimal ILoggerProvider that applies a configurable minimum log level.
    /// Used to verify the custom provider is actually wired up.
    /// </summary>
    private sealed class MinLevelLoggerProvider : ILoggerProvider
    {
        private readonly LogLevel _minLevel;

        public MinLevelLoggerProvider(LogLevel minLevel) => _minLevel = minLevel;

        public ILogger CreateLogger(string categoryName) => new MinLevelLogger(_minLevel);

        public void Dispose()
        {
        }

        private sealed class MinLevelLogger : ILogger
        {
            private readonly LogLevel _minLevel;

            public MinLevelLogger(LogLevel minLevel) => _minLevel = minLevel;

            public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLevel;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
            }

            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull
                => null;
        }
    }
}
