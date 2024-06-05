using Moq;

namespace SetupOnlyForOverridableMembers.TestOkForAbstractMethod;

public abstract class BaseSampleClass
{
    public int Calculate() => 0;

    public abstract int Calculate(int a, int b);

    public abstract int Calculate(int a, int b, int c);
}

internal class MyUnitTests
{
    private void TestOkForAbstractMethod()
    {
        var mock = new Mock<BaseSampleClass>();
        mock.Setup(x => x.Calculate(It.IsAny<int>(), It.IsAny<int>()));
    }
}
