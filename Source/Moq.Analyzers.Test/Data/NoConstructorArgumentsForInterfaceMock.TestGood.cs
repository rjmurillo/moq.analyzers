using Moq;

namespace NoConstructorArgumentsForInterfaceMock.TestGood;

internal interface IMyService
{
    void Do(string s);
}

internal class MyUnitTests
{
    private void TestGood()
    {
        var mock1 = new Mock<IMyService>();
        var mock2 = new Mock<IMyService>(MockBehavior.Default);
        var mock3 = new Mock<IMyService>(MockBehavior.Strict);
        var mock4 = new Mock<IMyService>(MockBehavior.Loose);
    }
}
