using Moq;

namespace NoMethodsInPropertySetup.Bad;

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
}
