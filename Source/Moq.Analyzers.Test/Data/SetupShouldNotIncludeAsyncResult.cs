using System.Threading.Tasks;
using Moq;

#pragma warning disable SA1402 // File may only contain a single class
#pragma warning disable SA1649 // File name must match first type name
#pragma warning disable SA1502 // Element must not be on a single line

namespace SetupShouldNotIncludeAsyncResult;

public class AsyncClient
{
    public virtual Task VoidAsync() => Task.CompletedTask;
    public virtual Task<string> GenericAsyncWithConcreteReturn() => Task.FromResult(string.Empty);
}

internal class MyUnitTests
{
    private void TestOkForTask()
    {
        var mock = new Mock<AsyncClient>();
        mock.Setup(c => c.VoidAsync());
    }

    private void TestOkForTaskWithConcreteReturn()
    {
        var mock = new Mock<AsyncClient>();
        mock.Setup(c => c.GenericAsyncWithConcreteReturn().Result);
    }

    private void TestOkForTaskWithConcreteReturnProperSetup()
    {
        var mock = new Mock<AsyncClient>();
        mock.Setup(c => c.GenericAsyncWithConcreteReturn())
            .ReturnsAsync(string.Empty);
    }
}
