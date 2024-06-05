using Moq;

namespace SetupOnlyForOverridableMembers.TestBadSetupForNonVirtualMethod;

public abstract class BaseSampleClass
{
    public int Calculate() => 0;

    public abstract int Calculate(int a, int b);

    public abstract int Calculate(int a, int b, int c);
}

internal class MyUnitTests
{
    private void TestBadSetupForNonVirtualMethod()
    {
        var mock = new Mock<BaseSampleClass>();
        mock.Setup(x => x.Calculate());
    }
}
