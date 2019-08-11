#pragma warning disable SA1402 // File may only contain a single class
#pragma warning disable SA1649 // File name must match first type name
#pragma warning disable SA1502 // Element must not be on a single line
#pragma warning disable RCS1213 // Remove unused member declaration.
#pragma warning disable IDE0051 // Remove unused private members
namespace AsAcceptOnlyInterface
{
    using Moq;

    public interface ISampleInterface
    {
        int TestProperty { get; set; }

        int Calculate(int a, int b);
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
        private void TestOkAsForInterface()
        {
            var mock = new Mock<BaseSampleClass>();
            mock.As<ISampleInterface>();
        }

        private void TestOkAsForInterfaceWithConfiguration()
        {
            var mock = new Mock<BaseSampleClass>();
            mock.As<ISampleInterface>()
                .Setup(x => x.Calculate(It.IsAny<int>(), It.IsAny<int>()))
                .Returns(10);
        }

        private void TestBadAsForAbstractClass()
        {
            var mock = new Mock<BaseSampleClass>();
            mock.As<BaseSampleClass>();
        }

        private void TestBadAsForNonAbstractClass()
        {
            var mock = new Mock<BaseSampleClass>();
            mock.As<OtherClass>();
        }
    }
}