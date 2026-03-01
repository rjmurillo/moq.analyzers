using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.NoMockOfLoggerAnalyzer>;

namespace Moq.Analyzers.Test;

public class NoMockOfLoggerAnalyzerTests
{
    [Fact]
    public async Task ShouldDetectMockOfILogger()
    {
        await Verifier.VerifyAnalyzerAsync(
                """
                using Moq;
                using Microsoft.Extensions.Logging;

                internal class UnitTest
                {
                    private void Test()
                    {
                        var mock = new Mock<{|Moq1004:ILogger|}>();
                    }
                }
                """,
                referenceAssemblyGroup: ReferenceAssemblyCatalog.Net80WithNewMoqAndLogging);
    }

    [Fact]
    public async Task ShouldDetectMockOfILoggerOfT()
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
                        var mock = new Mock<{|Moq1004:ILogger<MyService>|}>();
                    }
                }
                """,
                referenceAssemblyGroup: ReferenceAssemblyCatalog.Net80WithNewMoqAndLogging);
    }

    [Fact]
    public async Task ShouldDetectMockOfILoggerWithBehavior()
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
                        var mock1 = new Mock<{|Moq1004:ILogger|}>(MockBehavior.Strict);
                        var mock2 = new Mock<{|Moq1004:ILogger<MyService>|}>(MockBehavior.Loose);
                    }
                }
                """,
                referenceAssemblyGroup: ReferenceAssemblyCatalog.Net80WithNewMoqAndLogging);
    }

    [Fact]
    public async Task ShouldDetectMockOfViaMockOf()
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
                        var logger1 = Mock.Of<{|Moq1004:ILogger|}>();
                        var logger2 = Mock.Of<{|Moq1004:ILogger<MyService>|}>();
                    }
                }
                """,
                referenceAssemblyGroup: ReferenceAssemblyCatalog.Net80WithNewMoqAndLogging);
    }

    [Fact]
    public async Task ShouldDetectMockRepositoryCreate()
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
                        var repository = new MockRepository(MockBehavior.Strict);
                        var mock1 = repository.Create<{|Moq1004:ILogger|}>();
                        var mock2 = repository.Create<{|Moq1004:ILogger<MyService>|}>();
                    }
                }
                """,
                referenceAssemblyGroup: ReferenceAssemblyCatalog.Net80WithNewMoqAndLogging);
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
