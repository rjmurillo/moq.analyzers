using Moq;

namespace AsAcceptOnlyInterface.TestBadAsForAbstractClass;

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
    private void TestBadAsForAbstractClass()
    {
        var mock = new Mock<BaseSampleClass>();
        mock.As<BaseSampleClass>();
    }
}
