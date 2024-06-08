using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.SetupShouldNotIncludeAsyncResultAnalyzer>;

namespace Moq.Analyzers.Test;

public class SetupShouldNotIncludeAsyncResultAnalyzerTests
{
    [Fact]
    public async Task ShouldPassWhenSetupWithoutReturn()
    {
        await Verifier.VerifyAnalyzerAsync(
                """
                namespace SetupShouldNotIncludeAsyncResult.TestOkForTask;

                public class AsyncClient
                {
                    public virtual Task TaskAsync() => Task.CompletedTask;

                    public virtual Task<string> GenericTaskAsync() => Task.FromResult(string.Empty);
                }

                internal class MyUnitTests
                {
                    private void TestOkForTask()
                    {
                        var mock = new Mock<AsyncClient>();
                        mock.Setup(c => c.TaskAsync());
                    }
                }
                """);
    }

    [Fact]
    public async Task ShouldPassWhenSetupWithReturnsAsync()
    {
        await Verifier.VerifyAnalyzerAsync(
                """
                namespace SetupShouldNotIncludeAsyncResult.TestOkForGenericTaskProperSetup;

                public class AsyncClient
                {
                    public virtual Task TaskAsync() => Task.CompletedTask;

                    public virtual Task<string> GenericTaskAsync() => Task.FromResult(string.Empty);
                }

                internal class MyUnitTests
                {
                    private void TestOkForGenericTaskProperSetup()
                    {
                        var mock = new Mock<AsyncClient>();
                        mock.Setup(c => c.GenericTaskAsync())
                            .ReturnsAsync(string.Empty);
                    }
                }
                """);
    }

    [Fact]
    public async Task ShouldFailWhenSetupWithTaskResult()
    {
        await Verifier.VerifyAnalyzerAsync(
                """
                namespace SetupShouldNotIncludeAsyncResult.TestBadForGenericTask;

                public class AsyncClient
                {
                    public virtual Task TaskAsync() => Task.CompletedTask;

                    public virtual Task<string> GenericTaskAsync() => Task.FromResult(string.Empty);
                }

                internal class MyUnitTests
                {
                    private void TestBadForGenericTask()
                    {
                        var mock = new Mock<AsyncClient>();
                        mock.Setup(c => {|Moq1201:c.GenericTaskAsync().Result|});
                    }
                }
                """);
    }
}
