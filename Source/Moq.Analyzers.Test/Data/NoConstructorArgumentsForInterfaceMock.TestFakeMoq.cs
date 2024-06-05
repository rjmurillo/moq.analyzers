namespace NoConstructorArgumentsForInterfaceMock.TestFakeMoq;

public enum MockBehavior
{
    Default,
    Strict,
    Loose,
}

internal interface IMyService
{
    void Do(string s);
}

public class Mock<T>
    where T : class
{
    public Mock() { }

    public Mock(params object[] ar) { }

    public Mock(MockBehavior behavior) { }

    public Mock(MockBehavior behavior, params object[] args) { }
}

internal class MyUnitTests
{
    private void TestFakeMoq()
    {
        var mock1 = new Mock<IMyService>("4");
        var mock2 = new Mock<IMyService>(5, true);
        var mock3 = new Mock<IMyService>(MockBehavior.Strict, 6, true);
        var mock4 = new Mock<IMyService>(Moq.MockBehavior.Default, "5");
        var mock5 = new Mock<IMyService>(MockBehavior.Strict);
        var mock6 = new Mock<IMyService>(MockBehavior.Loose);
    }
}
