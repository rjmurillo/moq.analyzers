using Moq;

namespace SetupOnlyForOverridableMembers.TestBadSetupForNonVirtualProperty;

public class SampleClass
{

    public int Property { get; set; }
}

internal class MyUnitTests
{
    private void TestBadSetupForNonVirtualProperty()
    {
        var mock = new Mock<SampleClass>();
        mock.Setup(x => x.Property);
    }
}
