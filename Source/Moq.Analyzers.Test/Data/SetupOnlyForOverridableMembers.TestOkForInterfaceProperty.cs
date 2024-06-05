using Moq;

namespace SetupOnlyForOverridableMembers.TestOkForInterfaceProperty;

public interface ISampleInterface
{
    int TestProperty { get; set; }
}

internal class MyUnitTests
{
    private void TestOkForInterfaceProperty()
    {
        var mock = new Mock<ISampleInterface>();
        mock.Setup(x => x.TestProperty);
    }
}
