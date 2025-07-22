using Moq.Analyzers.Test.Helpers;

using AnalyzerVerifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.CallbackSignatureShouldMatchMockedMethodAnalyzer>;

namespace Moq.Analyzers.Test;

/// <summary>
/// Simplified test to understand exactly what patterns need enhancement.
/// </summary>
public class CallbackAnalyzerEnhancementTargetTests
{
    [Fact]
    public async Task MultipleCallbacks_FirstCallbackWrong_ShouldDetect()
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
                        .Callback({|Moq1100:(int wrongParam)|} => { })  // Should trigger diagnostic  
                        .Returns(42)
                        .Callback(() => { });                           // This one is correct
                }
            }
            """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(source, "Net80WithOldMoq");
    }

    [Fact]
    public async Task MultipleCallbacks_SecondCallbackWrong_ShouldDetect()
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
                        .Callback(() => { })                            // This one is correct
                        .Returns(42)
                        .Callback({|Moq1100:(int wrongParam)|} => { }); // Should trigger diagnostic
                }
            }
            """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(source, "Net80WithOldMoq");
    }

    [Fact]
    public async Task GenericCallback_WrongType_ShouldDetect()
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
                        .Callback<int>({|Moq1100:i|} => { });  // Wrong type - should trigger diagnostic
                }
            }
            """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(source, "Net80WithOldMoq");
    }

    [Fact]
    public async Task GenericCallback_CorrectType_ShouldNotDetect()
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
                        .Callback<string>(s => { });  // Correct type - should not trigger
                }
            }
            """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(source, "Net80WithOldMoq");
    }
}
