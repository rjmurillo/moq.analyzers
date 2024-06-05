using Moq;

namespace SetupOnlyForOverridableMembers.TestBadSetupForSealedMethod;

public abstract class BaseSampleClass
{
    public int Calculate() => 0;

    public abstract int Calculate(int a, int b);

    public abstract int Calculate(int a, int b, int c);
}

public class SampleClass : BaseSampleClass
{

    public override int Calculate(int a, int b) => 0;

    public sealed override int Calculate(int a, int b, int c) => 0;
}

internal class MyUnitTests
{
    private void TestBadSetupForSealedMethod()
    {
        var mock = new Mock<SampleClass>();
        mock.Setup(x => x.Calculate(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()));
    }
}
