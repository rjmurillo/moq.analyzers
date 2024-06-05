using Moq;

namespace NoConstructorArgumentsForInterfaceMock.TestBadWithMockBehavior;

internal interface IMyService
{
    void Do(string s);
}

internal class MyUnitTests
{
    private void TestBadWithMockBehavior()
    {
        var mock1 = new Mock<IMyService>(MockBehavior.Default, "123");
        var mock2 = new Mock<IMyService>(MockBehavior.Strict, 25, true);
        var mock3 = new Mock<IMyService>(MockBehavior.Default, "123");
        var mock4 = new Mock<IMyService>(MockBehavior.Loose, 25, true);
    }
}
