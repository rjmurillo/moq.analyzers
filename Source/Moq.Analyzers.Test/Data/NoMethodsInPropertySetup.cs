using Moq;

namespace NoMethodsInPropertySetup;

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
