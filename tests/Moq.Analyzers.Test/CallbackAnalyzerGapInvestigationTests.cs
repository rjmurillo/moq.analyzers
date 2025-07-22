using Moq.Analyzers.Test.Helpers;

using AnalyzerVerifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.CallbackSignatureShouldMatchMockedMethodAnalyzer>;

namespace Moq.Analyzers.Test;

/// <summary>
/// Tests to understand what specific patterns are not currently supported.
/// </summary>
public class CallbackAnalyzerGapInvestigationTests
{
    [Fact]
    public async Task RegularCallback_WrongType_IsDetected()
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
                        .Callback((int wrongParam) => { });  // Should trigger diagnostic
                }
            }
            """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(source, "Net80WithOldMoq");
    }

    [Fact]
    public async Task GenericCallback_SameAsRegular_IsDetected()
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
                        .Callback<string>(wrongParam => { });  // This should NOT trigger (correct type)
                }
            }
            """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(source, "Net80WithOldMoq");
    }

    [Fact]
    public async Task GenericCallback_WrongGenericType_MightNotBeDetected()
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
                        .Callback<int>(wrongParam => { });  // Should trigger but might not
                }
            }
            """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(source, "Net80WithOldMoq");
    }
}
