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
