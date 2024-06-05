using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace Moq.Analyzers.Test;

public class SetupShouldNotIncludeAsyncResultAnalyzerTests : DiagnosticVerifier
{
    // [Fact]
    public Task ShouldPassWhenSetupWithoutReturn()
    {
        return Verify(VerifyCSharpDiagnostic(
            [
                """
                using System.Threading.Tasks;
                using Moq;

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
                """
            ]));
    }

    // [Fact]
    public Task ShouldPassWhenSetupWithReturnsAsync()
    {
        return Verify(VerifyCSharpDiagnostic(
            [
                """
                using System.Threading.Tasks;
                using Moq;

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
                """
            ]));
    }

    // [Fact]
    public Task ShouldFailWhenSetupWithTaskResult()
    {
        return Verify(VerifyCSharpDiagnostic(
            [
                """
                using System.Threading.Tasks;
                using Moq;

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
                        mock.Setup(c => c.GenericTaskAsync().Result);
                    }
                }
                """
            ]));
    }

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        return new SetupShouldNotIncludeAsyncResultAnalyzer();
    }
}
