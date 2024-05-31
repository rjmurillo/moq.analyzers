#pragma warning disable SA1402 // File may only contain a single class
#pragma warning disable SA1502 // Element must not be on a single line
#pragma warning disable SA1602 // Undocumented enum values
#pragma warning disable SA1649 // File name must match first type name
#pragma warning disable RCS1163 // Unused parameter
#pragma warning disable RCS1213 // Remove unused member declaration
#pragma warning disable IDE0051 // Unused private member
#pragma warning disable IDE0059 // Unnecessary value assignment
#pragma warning disable IDE0060 // Unused parameter
namespace NoMethodsInPropertySetup
{
    using Moq;

    public interface IFoo
    {
        string Prop1 { get; set; }

        string Prop2 { get; }

        string Prop3 { set; }

        string Method();
    }

    public class MyUnitTests
    {
        private void TestBad()
        {
            var mock = new Mock<IFoo>();
            mock.SetupGet(x => x.Method());
            mock.SetupSet(x => x.Method());
        }

        private void TestGood()
        {
            var mock = new Mock<IFoo>();
            mock.SetupGet(x => x.Prop1);
            mock.SetupGet(x => x.Prop2);
            mock.SetupSet(x => x.Prop1 = "1");
            mock.SetupSet(x => x.Prop3 = "2");
            mock.Setup(x => x.Method());
        }
    }
}