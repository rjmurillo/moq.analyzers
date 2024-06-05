using System.Threading.Tasks;
using Moq;

namespace SetupShouldNotIncludeAsyncResult;

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

    private void TestOkForGenericTask()
    {
        var mock = new Mock<AsyncClient>();
        mock.Setup(c => c.GenericTaskAsync().Result);
    }

    private void TestOkForGenericTaskProperSetup()
    {
        var mock = new Mock<AsyncClient>();
        mock.Setup(c => c.GenericTaskAsync())
            .ReturnsAsync(string.Empty);
    }
}
