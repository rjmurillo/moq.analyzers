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
