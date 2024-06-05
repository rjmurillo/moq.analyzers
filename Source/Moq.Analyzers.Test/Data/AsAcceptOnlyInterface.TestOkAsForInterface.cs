using Moq;

namespace AsAcceptOnlyInterface.TestOkAsForInterface;

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
    private void TestOkAsForInterface()
    {
        var mock = new Mock<SampleClass>();
        mock.As<ISampleInterface>();
    }
}
