using Moq;

namespace AsAcceptOnlyInterface.TestBadAsForAbstractClass;

public abstract class BaseSampleClass
{
    public int Calculate() => 0;
}

internal class MyUnitTests
{
    private void TestBadAsForAbstractClass()
    {
        var mock = new Mock<BaseSampleClass>();
        mock.As<BaseSampleClass>();
    }
}
