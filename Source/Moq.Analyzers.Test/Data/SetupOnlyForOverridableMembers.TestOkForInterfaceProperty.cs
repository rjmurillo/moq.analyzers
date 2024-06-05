using Moq;

namespace SetupOnlyForOverridableMembers.TestOkForInterfaceProperty;

public interface ISampleInterface
{
    int Calculate(int a, int b);

    int TestProperty { get; set; }
}

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

    public virtual int DoSth() => 0;

    public int Property { get; set; }
}

internal class MyUnitTests
{
    private void TestOkForInterfaceProperty()
    {
        var mock = new Mock<ISampleInterface>();
        mock.Setup(x => x.TestProperty);
    }
}
