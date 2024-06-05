using Moq;

namespace AsAcceptOnlyInterface.TestBadAsForNonAbstractClass;

public interface ISampleInterface
{
    int Calculate(int a, int b);
}

public abstract class BaseSampleClass
{
    public int Calculate() => 0;
}

public class OtherClass
{

    public int Calculate() => 0;
}

internal class MyUnitTests
{
    private void TestBadAsForNonAbstractClass()
    {
        var mock = new Mock<BaseSampleClass>();
        mock.As<OtherClass>();
    }
}
