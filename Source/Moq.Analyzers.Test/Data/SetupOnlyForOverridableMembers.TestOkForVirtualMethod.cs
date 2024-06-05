using Moq;

namespace SetupOnlyForOverridableMembers.TestOkForVirtualMethod;

public class SampleClass
{
    public virtual int DoSth() => 0;
}

internal class MyUnitTests
{
    private void TestOkForVirtualMethod()
    {
        var mock = new Mock<SampleClass>();
        mock.Setup(x => x.DoSth());
    }
}
