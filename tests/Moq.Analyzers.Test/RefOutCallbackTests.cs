using Moq.Analyzers.Test.Helpers;

namespace Moq.Analyzers.Test;

public class RefOutCallbackTests
{
    [Fact]
    public async Task ShouldHandleRefParameterCallbacks()
    {
        string testCode = """
            using Moq;

            internal interface IFoo
            {
                void Submit(ref string data);
            }

            internal delegate void SubmitCallback(ref string data);

            internal class TestClass
            {
                public void TestMethod()
                {
                    var mock = new Mock<IFoo>();
                    
                    // This should work - correct ref parameter callback
                    mock.Setup(foo => foo.Submit(ref It.Ref<string>.IsAny))
                        .Callback(new SubmitCallback((ref string data) => System.Console.WriteLine("Submit called")));
                }
            }
            """;

        await AnalyzerVerifier<CallbackSignatureShouldMatchMockedMethodAnalyzer>.VerifyAnalyzerAsync(
            testCode,
            ReferenceAssemblyCatalog.Net80WithNewMoq);
    }

    [Fact]
    public async Task ShouldHandleOutParameterCallbacks()
    {
        string testCode = """
            using Moq;

            internal interface IFoo
            {
                void Process(out int result);
            }

            internal delegate void ProcessCallback(out int result);

            internal class TestClass
            {
                public void TestMethod()
                {
                    var mock = new Mock<IFoo>();
                    
                    // This should work - correct out parameter callback
                    mock.Setup(foo => foo.Process(out It.Ref<int>.IsAny))
                        .Callback(new ProcessCallback((out int result) => { result = 42; }));
                }
            }
            """;

        await AnalyzerVerifier<CallbackSignatureShouldMatchMockedMethodAnalyzer>.VerifyAnalyzerAsync(
            testCode,
            ReferenceAssemblyCatalog.Net80WithNewMoq);
    }

    [Fact]
    public async Task ShouldHandleInParameterCallbacks()
    {
        string testCode = """
            using Moq;
            using System;

            internal interface IFoo
            {
                void Handle(in DateTime timestamp);
            }

            internal delegate void HandleCallback(in DateTime timestamp);

            internal class TestClass
            {
                public void TestMethod()
                {
                    var mock = new Mock<IFoo>();
                    
                    // This should work - correct in parameter callback
                    mock.Setup(foo => foo.Handle(in It.Ref<DateTime>.IsAny))
                        .Callback(new HandleCallback((in DateTime timestamp) => System.Console.WriteLine("Handle called")));
                }
            }
            """;

        await AnalyzerVerifier<CallbackSignatureShouldMatchMockedMethodAnalyzer>.VerifyAnalyzerAsync(
            testCode,
            ReferenceAssemblyCatalog.Net80WithNewMoq);
    }
}
