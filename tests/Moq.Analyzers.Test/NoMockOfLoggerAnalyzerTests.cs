using Microsoft.CodeAnalysis.Testing;
using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.NoMockOfLoggerAnalyzer>;

namespace Moq.Analyzers.Test;

public class NoMockOfLoggerAnalyzerTests
{
    [Fact]
    public async Task ShouldSuggestNullLoggerInstanceForILogger()
    {
        // Verify the diagnostic message suggests "NullLogger.Instance" for non-generic ILogger.
        await Verifier.VerifyAnalyzerAsync(
                """
                using Moq;
                using Microsoft.Extensions.Logging;

                internal class UnitTest
                {
                    private void Test()
                    {
                        var mock = new Mock<ILogger>();
                    }
                }
                """,
                ReferenceAssemblyCatalog.Net80WithNewMoqAndLogging,
                new DiagnosticResult("Moq1004", DiagnosticSeverity.Warning)
                    .WithSpan("/0/Test1.cs", 8, 29, 8, 36)
                    .WithArguments("NullLogger.Instance"));
    }

    [Fact]
    public async Task ShouldSuggestNullLoggerOfTInstanceForILoggerOfT()
    {
        // Verify the diagnostic message suggests "NullLogger<T>.Instance" for generic ILogger<T>.
        await Verifier.VerifyAnalyzerAsync(
                """
                using Moq;
                using Microsoft.Extensions.Logging;

                internal class MyService { }

                internal class UnitTest
                {
                    private void Test()
                    {
                        var mock = new Mock<ILogger<MyService>>();
                    }
                }
                """,
                ReferenceAssemblyCatalog.Net80WithNewMoqAndLogging,
                new DiagnosticResult("Moq1004", DiagnosticSeverity.Warning)
                    .WithSpan("/0/Test1.cs", 10, 29, 10, 47)
                    .WithArguments("NullLogger<T>.Instance"));
    }

    [Fact]
    public async Task ShouldSuggestCorrectNullLoggerWithBehaviorArgument()
    {
        await Verifier.VerifyAnalyzerAsync(
                """
                using Moq;
                using Microsoft.Extensions.Logging;

                internal class MyService { }

                internal class UnitTest
                {
                    private void Test()
                    {
                        var mock1 = new Mock<ILogger>(MockBehavior.Strict);
                        var mock2 = new Mock<ILogger<MyService>>(MockBehavior.Loose);
                    }
                }
                """,
                ReferenceAssemblyCatalog.Net80WithNewMoqAndLogging,
                new DiagnosticResult("Moq1004", DiagnosticSeverity.Warning)
                    .WithSpan("/0/Test1.cs", 10, 30, 10, 37)
                    .WithArguments("NullLogger.Instance"),
                new DiagnosticResult("Moq1004", DiagnosticSeverity.Warning)
                    .WithSpan("/0/Test1.cs", 11, 30, 11, 48)
                    .WithArguments("NullLogger<T>.Instance"));
    }

    [Fact]
    public async Task ShouldSuggestNullLoggerInstanceForMockOfILogger()
    {
        // Verify Mock.Of<ILogger>() suggests "NullLogger.Instance".
        await Verifier.VerifyAnalyzerAsync(
                """
                using Moq;
                using Microsoft.Extensions.Logging;

                internal class UnitTest
                {
                    private void Test()
                    {
                        var logger = Mock.Of<ILogger>();
                    }
                }
                """,
                ReferenceAssemblyCatalog.Net80WithNewMoqAndLogging,
                new DiagnosticResult("Moq1004", DiagnosticSeverity.Warning)
                    .WithSpan("/0/Test1.cs", 8, 30, 8, 37)
                    .WithArguments("NullLogger.Instance"));
    }

    [Fact]
    public async Task ShouldSuggestNullLoggerOfTInstanceForMockOfILoggerOfT()
    {
        // Verify Mock.Of<ILogger<T>>() suggests "NullLogger<T>.Instance".
        await Verifier.VerifyAnalyzerAsync(
                """
                using Moq;
                using Microsoft.Extensions.Logging;

                internal class MyService { }

                internal class UnitTest
                {
                    private void Test()
                    {
                        var logger = Mock.Of<ILogger<MyService>>();
                    }
                }
                """,
                ReferenceAssemblyCatalog.Net80WithNewMoqAndLogging,
                new DiagnosticResult("Moq1004", DiagnosticSeverity.Warning)
                    .WithSpan("/0/Test1.cs", 10, 30, 10, 48)
                    .WithArguments("NullLogger<T>.Instance"));
    }

    [Fact]
    public async Task ShouldSuggestCorrectNullLoggerForMockRepositoryCreate()
    {
        // Verify MockRepository.Create<ILogger>() suggests "NullLogger.Instance"
        // and MockRepository.Create<ILogger<T>>() suggests "NullLogger<T>.Instance".
        await Verifier.VerifyAnalyzerAsync(
                """
                using Moq;
                using Microsoft.Extensions.Logging;

                internal class MyService { }

                internal class UnitTest
                {
                    private void Test()
                    {
                        var repository = new MockRepository(MockBehavior.Strict);
                        var mock1 = repository.Create<ILogger>();
                        var mock2 = repository.Create<ILogger<MyService>>();
                    }
                }
                """,
                ReferenceAssemblyCatalog.Net80WithNewMoqAndLogging,
                new DiagnosticResult("Moq1004", DiagnosticSeverity.Warning)
                    .WithSpan("/0/Test1.cs", 11, 39, 11, 46)
                    .WithArguments("NullLogger.Instance"),
                new DiagnosticResult("Moq1004", DiagnosticSeverity.Warning)
                    .WithSpan("/0/Test1.cs", 12, 39, 12, 57)
                    .WithArguments("NullLogger<T>.Instance"));
    }

    [Fact]
    public async Task ShouldNotFlagNonLoggerMocks()
    {
        await Verifier.VerifyAnalyzerAsync(
                """
                using Moq;
                using Microsoft.Extensions.Logging;

                internal interface IMyService { }

                internal class UnitTest
                {
                    private void Test()
                    {
                        var mock = new Mock<IMyService>();
                        var of = Mock.Of<IMyService>();
                    }
                }
                """,
                referenceAssemblyGroup: ReferenceAssemblyCatalog.Net80WithNewMoqAndLogging);
    }

    [Fact]
    public async Task ShouldNotFlagNonLoggerMockRepositoryCreate()
    {
        await Verifier.VerifyAnalyzerAsync(
                """
                using Moq;
                using Microsoft.Extensions.Logging;

                internal interface IMyService { }

                internal class UnitTest
                {
                    private void Test()
                    {
                        var repository = new MockRepository(MockBehavior.Strict);
                        var mock = repository.Create<IMyService>();
                    }
                }
                """,
                referenceAssemblyGroup: ReferenceAssemblyCatalog.Net80WithNewMoqAndLogging);
    }

    [Fact]
    public async Task ShouldNotFlagWhenLoggingNotReferenced()
    {
        await Verifier.VerifyAnalyzerAsync(
                """
                using Moq;

                internal interface IMyService { }

                internal class UnitTest
                {
                    private void Test()
                    {
                        var mock = new Mock<IMyService>();
                    }
                }
                """,
                referenceAssemblyGroup: ReferenceAssemblyCatalog.Net80WithNewMoq);
    }

    [Fact]
    public async Task ShouldNotFlagNonMockObjectCreation()
    {
        await Verifier.VerifyAnalyzerAsync(
                """
                using Moq;
                using Microsoft.Extensions.Logging;
                using System.Collections.Generic;

                internal class UnitTest
                {
                    private void Test()
                    {
                        var list = new List<int>();
                        var str = "test";
                    }
                }
                """,
                referenceAssemblyGroup: ReferenceAssemblyCatalog.Net80WithNewMoqAndLogging);
    }
}
