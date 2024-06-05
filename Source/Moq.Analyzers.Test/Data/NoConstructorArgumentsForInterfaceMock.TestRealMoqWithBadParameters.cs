namespace NoConstructorArgumentsForInterfaceMock.TestRealMoqWithBadParameters;

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
    private void TestRealMoqWithBadParameters()
    {
        var mock1 = new Moq.Mock<IMyService>(1, true);
        var mock2 = new Moq.Mock<IMyService>("2");
        var mock3 = new Moq.Mock<IMyService>(Moq.MockBehavior.Default, "3");
        var mock4 = new Moq.Mock<IMyService>(MockBehavior.Loose, 4, true);
        var mock5 = new Moq.Mock<IMyService>(MockBehavior.Default);
        var mock6 = new Moq.Mock<IMyService>(MockBehavior.Default);
    }
}
