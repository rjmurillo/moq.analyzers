using Moq;

namespace AsAcceptOnlyInterface.TestOkAsForInterfaceWithConfiguration;

public interface ISampleInterface
{
    int Calculate(int a, int b);

    int TestProperty { get; set; }
}

public abstract class BaseSampleClass
{
    public int Calculate()
    {
        return 0;
    }

    public abstract int Calculate(int a, int b, int c);
}

public class SampleClass
{

    public virtual int Calculate(int a, int b) => 0;
}

public class OtherClass
{

    public virtual int Calculate() => 0;
}

internal class MyUnitTests
{
    private void TestOkAsForInterfaceWithConfiguration()
    {
        var mock = new Mock<BaseSampleClass>();
        mock.As<ISampleInterface>()
            .Setup(x => x.Calculate(It.IsAny<int>(), It.IsAny<int>()))
            .Returns(10);
    }
}
