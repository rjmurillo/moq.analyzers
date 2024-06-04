using Microsoft.CodeAnalysis.CSharp.Syntax;
using Moq;

namespace SetupOnlyForOverridableMembers;

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
    private void TestOkForAbstractMethod()
    {
        var mock = new Mock<BaseSampleClass>();
        mock.Setup(x => x.Calculate(It.IsAny<int>(), It.IsAny<int>()));
    }

    private void TestOkForOverrideAbstractMethod()
    {
        var mock = new Mock<SampleClass>();
        mock.Setup(x => x.Calculate(It.IsAny<int>(), It.IsAny<int>()));
    }

    private void TestOkForInterfaceMethod()
    {
        var mock = new Mock<ISampleInterface>();
        mock.Setup(x => x.Calculate(It.IsAny<int>(), It.IsAny<int>()));
    }

    private void TestOkForInterfaceProperty()
    {
        var mock = new Mock<ISampleInterface>();
        mock.Setup(x => x.TestProperty);
    }

    private void TestOkForVirtualMethod()
    {
        var mock = new Mock<SampleClass>();
        mock.Setup(x => x.DoSth());
    }

    private void TestBadSetupForNonVirtualMethod()
    {
        var mock = new Mock<BaseSampleClass>();
        mock.Setup(x => x.Calculate());
    }

    private void TestBadSetupForSealedMethod()
    {
        var mock = new Mock<SampleClass>();
        mock.Setup(x => x.Calculate(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()));
    }

    private void TestBadSetupForNonVirtualProperty()
    {
        var mock = new Mock<SampleClass>();
        mock.Setup(x => x.Property);
    }
}
