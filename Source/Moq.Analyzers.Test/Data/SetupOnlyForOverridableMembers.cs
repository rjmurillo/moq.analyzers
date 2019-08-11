#pragma warning disable SA1402 // File may only contain a single class
#pragma warning disable SA1502 // Element must not be on a single line
#pragma warning disable SA1602 // Undocumented enum values
#pragma warning disable SA1649 // File name must match first type name
#pragma warning disable RCS1163 // Unused parameter
#pragma warning disable RCS1213 // Remove unused member declaration
#pragma warning disable IDE0051 // Unused private member
#pragma warning disable IDE0059 // Unnecessary value assignment
#pragma warning disable IDE0060 // Unused parameter
namespace SetupOnlyForOverridableMembers
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

        public abstract int Calculate(int a, int b);

        public abstract int Calculate(int a, int b, int c);
    }

    public class SampleClass : BaseSampleClass
    {
        public int Property { get; set; }

        public override int Calculate(int a, int b) => 0;

        public sealed override int Calculate(int a, int b, int c) => 0;

        public virtual int DoSth() => 0;
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
}