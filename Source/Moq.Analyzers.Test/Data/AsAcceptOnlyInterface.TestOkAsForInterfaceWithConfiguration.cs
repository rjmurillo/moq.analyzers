using Moq;

namespace AsAcceptOnlyInterface.TestOkAsForInterfaceWithConfiguration;

public interface ISampleInterface
{
    int Calculate(int a, int b);
}

public class SampleClass
{
    public int Calculate() => 0;
}

internal class MyUnitTests
{
    private void TestOkAsForInterfaceWithConfiguration()
    {
        var mock = new Mock<SampleClass>();
        mock.As<ISampleInterface>()
            .Setup(x => x.Calculate(It.IsAny<int>(), It.IsAny<int>()))
            .Returns(10);
    }
}
