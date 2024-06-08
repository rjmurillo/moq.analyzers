using Verifier = Moq.Analyzers.Test.Helpers.CodeFixVerifier<Moq.Analyzers.CallbackSignatureShouldMatchMockedMethodAnalyzer, Moq.Analyzers.CallbackSignatureShouldMatchMockedMethodCodeFix>;

namespace Moq.Analyzers.Test;

public class CallbackSignatureShouldMatchMockedMethodCodeFixTests
{
    [Fact]
    public async Task ShouldPassWhenCorrectSetupAndReturns()
    {
        await Verifier.VerifyCodeFixAsync(
            """
            namespace CallbackSignatureShouldMatchMockedMethod.MyGoodSetupAndReturns;

            internal interface IFoo
            {
                int Do(string s);

                int Do(int i, string s, DateTime dt);

                int Do(List<string> l);
            }

            internal class MyUnitTests
            {
                private void MyGoodSetupAndReturns()
                {
                    var mock = new Mock<IFoo>();
                    mock.Setup(x => x.Do(It.IsAny<string>())).Returns((string s) => { return 0; });
                    mock.Setup(x => x.Do(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>())).Returns((int i, string s, DateTime dt) => { return 0; });
                    mock.Setup(x => x.Do(It.IsAny<List<string>>())).Returns((List<string> l) => { return 0; });
                }
            }
            """,
            """
            namespace CallbackSignatureShouldMatchMockedMethod.MyGoodSetupAndReturns;

            internal interface IFoo
            {
                int Do(string s);

                int Do(int i, string s, DateTime dt);

                int Do(List<string> l);
            }

            internal class MyUnitTests
            {
                private void MyGoodSetupAndReturns()
                {
                    var mock = new Mock<IFoo>();
                    mock.Setup(x => x.Do(It.IsAny<string>())).Returns((string s) => { return 0; });
                    mock.Setup(x => x.Do(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>())).Returns((int i, string s, DateTime dt) => { return 0; });
                    mock.Setup(x => x.Do(It.IsAny<List<string>>())).Returns((List<string> l) => { return 0; });
                }
            }
            """);
    }

    [Fact]
    public async Task ShouldSuggestQuickFixWhenIncorrectCallbacks()
    {
        await Verifier.VerifyCodeFixAsync(
            """
            namespace CallbackSignatureShouldMatchMockedMethod.TestBadCallbacks;

            internal interface IFoo
            {
                int Do(string s);

                int Do(int i, string s, DateTime dt);

                int Do(List<string> l);
            }

            internal class MyUnitTests
            {
                private void TestBadCallbacks()
                {
                    var mock = new Mock<IFoo>();
                    mock.Setup(x => x.Do(It.IsAny<string>())).Callback({|Moq1100:(int i)|} => { });
                    mock.Setup(x => x.Do(It.IsAny<string>())).Callback({|Moq1100:(string s1, string s2)|} => { });
                    mock.Setup(x => x.Do(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>())).Callback({|Moq1100:(string s1, int i1)|} => { });
                    mock.Setup(x => x.Do(It.IsAny<List<string>>())).Callback({|Moq1100:(int i)|} => { });
                }
            }
            """,
            """
            namespace CallbackSignatureShouldMatchMockedMethod.TestBadCallbacks;

            internal interface IFoo
            {
                int Do(string s);

                int Do(int i, string s, DateTime dt);

                int Do(List<string> l);
            }

            internal class MyUnitTests
            {
                private void TestBadCallbacks()
                {
                    var mock = new Mock<IFoo>();
                    mock.Setup(x => x.Do(It.IsAny<string>())).Callback((string s) => { });
                    mock.Setup(x => x.Do(It.IsAny<string>())).Callback((string s) => { });
                    mock.Setup(x => x.Do(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>())).Callback((int i, string s, DateTime dt) => { });
                    mock.Setup(x => x.Do(It.IsAny<List<string>>())).Callback((List<string> l) => { });
                }
            }
            """);
    }

    [Fact]
    public async Task ShouldPassWhenCorrectSetupAndCallbacks()
    {
        await Verifier.VerifyCodeFixAsync(
            """
            namespace CallbackSignatureShouldMatchMockedMethod.TestGoodSetupAndCallback;

            internal interface IFoo
            {
                int Do(string s);

                int Do(int i, string s, DateTime dt);

                int Do(List<string> l);
            }

            internal class MyUnitTests
            {
                private void TestGoodSetupAndCallback()
                {
                    var mock = new Mock<IFoo>();
                    mock.Setup(x => x.Do(It.IsAny<string>())).Callback((string s) => { });
                    mock.Setup(x => x.Do(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>())).Callback((int i, string s, DateTime dt) => { });
                    mock.Setup(x => x.Do(It.IsAny<List<string>>())).Callback((List<string> l) => { });
                }
            }
            """,
            """
            namespace CallbackSignatureShouldMatchMockedMethod.TestGoodSetupAndCallback;

            internal interface IFoo
            {
                int Do(string s);

                int Do(int i, string s, DateTime dt);

                int Do(List<string> l);
            }

            internal class MyUnitTests
            {
                private void TestGoodSetupAndCallback()
                {
                    var mock = new Mock<IFoo>();
                    mock.Setup(x => x.Do(It.IsAny<string>())).Callback((string s) => { });
                    mock.Setup(x => x.Do(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>())).Callback((int i, string s, DateTime dt) => { });
                    mock.Setup(x => x.Do(It.IsAny<List<string>>())).Callback((List<string> l) => { });
                }
            }
            """);
    }

    [Fact]
    public async Task ShouldPassWhenCorrectSetupAndParameterlessCallbacks()
    {
        await Verifier.VerifyCodeFixAsync(
            """
            namespace CallbackSignatureShouldMatchMockedMethod.TestGoodSetupAndParameterlessCallback;

            internal interface IFoo
            {
                int Do(string s);

                int Do(int i, string s, DateTime dt);

                int Do(List<string> l);
            }

            internal class MyUnitTests
            {
                private void TestGoodSetupAndParameterlessCallback()
                {
                    var mock = new Mock<IFoo>();
                    mock.Setup(x => x.Do(It.IsAny<string>())).Callback(() => { });
                    mock.Setup(x => x.Do(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>())).Callback(() => { });
                    mock.Setup(x => x.Do(It.IsAny<List<string>>())).Callback(() => { });
                }
            }
            """,
            """
            namespace CallbackSignatureShouldMatchMockedMethod.TestGoodSetupAndParameterlessCallback;

            internal interface IFoo
            {
                int Do(string s);

                int Do(int i, string s, DateTime dt);

                int Do(List<string> l);
            }

            internal class MyUnitTests
            {
                private void TestGoodSetupAndParameterlessCallback()
                {
                    var mock = new Mock<IFoo>();
                    mock.Setup(x => x.Do(It.IsAny<string>())).Callback(() => { });
                    mock.Setup(x => x.Do(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>())).Callback(() => { });
                    mock.Setup(x => x.Do(It.IsAny<List<string>>())).Callback(() => { });
                }
            }
            """);
    }

    [Fact]
    public async Task ShouldPassWhenCorrectSetupAndReturnsAndCallbacks()
    {
        await Verifier.VerifyCodeFixAsync(
            """
            namespace CallbackSignatureShouldMatchMockedMethod.TestGoodSetupAndReturnsAndCallback;

            internal interface IFoo
            {
                int Do(string s);

                int Do(int i, string s, DateTime dt);

                int Do(List<string> l);
            }

            internal class MyUnitTests
            {
                private void TestGoodSetupAndReturnsAndCallback()
                {
                    var mock = new Mock<IFoo>();
                    mock.Setup(x => x.Do(It.IsAny<string>())).Returns(0).Callback((string s) => { });
                    mock.Setup(x => x.Do(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>())).Returns(0).Callback((int i, string s, DateTime dt) => { });
                    mock.Setup(x => x.Do(It.IsAny<List<string>>())).Returns(0).Callback((List<string> l) => { });
                }
            }
            """,
            """
            namespace CallbackSignatureShouldMatchMockedMethod.TestGoodSetupAndReturnsAndCallback;

            internal interface IFoo
            {
                int Do(string s);

                int Do(int i, string s, DateTime dt);

                int Do(List<string> l);
            }

            internal class MyUnitTests
            {
                private void TestGoodSetupAndReturnsAndCallback()
                {
                    var mock = new Mock<IFoo>();
                    mock.Setup(x => x.Do(It.IsAny<string>())).Returns(0).Callback((string s) => { });
                    mock.Setup(x => x.Do(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>())).Returns(0).Callback((int i, string s, DateTime dt) => { });
                    mock.Setup(x => x.Do(It.IsAny<List<string>>())).Returns(0).Callback((List<string> l) => { });
                }
            }
            """);
    }
}
