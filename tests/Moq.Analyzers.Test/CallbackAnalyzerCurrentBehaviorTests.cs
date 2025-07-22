using Moq.Analyzers.Test.Helpers;

using AnalyzerVerifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.CallbackSignatureShouldMatchMockedMethodAnalyzer>;

namespace Moq.Analyzers.Test;

/// <summary>
/// Simple tests to understand current analyzer behavior before enhancements.
/// </summary>
public class CallbackAnalyzerCurrentBehaviorTests
{
    [Fact]
    public async Task CurrentAnalyzer_BasicCallback_ShouldDetectWrongSignature()
    {
        const string source = """
            using Moq;

            public interface IFoo
            {
                int DoWork(string input);
            }

            public class TestClass
            {
                public void TestMethod()
                {
                    var mock = new Mock<IFoo>();
                    mock.Setup(x => x.DoWork("test"))
                        .Callback(({|Moq1100:int wrongParam|}) => { });
                }
            }
            """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(source, "Net80WithOldMoq");
    }

    [Fact]
    public async Task CurrentAnalyzer_MultipleCallbacks_WhatHappens()
    {
        const string source = """
            using Moq;

            public interface IFoo
            {
                int DoWork(string input);
            }

            public class TestClass
            {
                public void TestMethod()
                {
                    var mock = new Mock<IFoo>();
                    mock.Setup(x => x.DoWork("test"))
                        .Callback(() => { })  // This should be fine
                        .Returns(42)
                        .Callback(() => { }); // This might be missed
                }
            }
            """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(source, "Net80WithOldMoq");
    }

    [Fact]
    public async Task CurrentAnalyzer_GenericCallback_WhatHappens()
    {
        const string source = """
            using Moq;

            public interface IFoo
            {
                T ProcessGeneric<T>(T input);
            }

            public class TestClass
            {
                public void TestMethod()
                {
                    var mock = new Mock<IFoo>();
                    mock.Setup(x => x.ProcessGeneric<string>("test"))
                        .Callback<string>(s => { });  // This might not be validated
                }
            }
            """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(source, "Net80WithOldMoq");
    }
}
