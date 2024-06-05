using Moq;

namespace SetupOnlyForOverridableMembers.TestOkForInterfaceMethod;

public interface ISampleInterface
{
    int Calculate(int a, int b);
}


internal class MyUnitTests
{
    private void TestOkForInterfaceMethod()
    {
        var mock = new Mock<ISampleInterface>();
        mock.Setup(x => x.Calculate(It.IsAny<int>(), It.IsAny<int>()));
    }
}
