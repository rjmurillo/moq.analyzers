namespace NoConstructorArgumentsForInterfaceMock.TestRealMoqWithGoodParameters;

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
    private void TestRealMoqWithGoodParameters()
    {
        var mock1 = new Moq.Mock<IMyService>(Moq.MockBehavior.Default);
        var mock2 = new Moq.Mock<IMyService>(Moq.MockBehavior.Default);
    }
}
