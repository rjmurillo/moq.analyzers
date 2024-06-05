using Moq;

namespace NoConstructorArgumentsForInterfaceMock_1;

internal interface IMyService
{
    void Do(string s);
}

internal class MyUnitTests
{
    private void TestBad()
    {
        var mock1 = new Mock<IMyService>(25, true);
        var mock2 = new Mock<IMyService>("123");
        var mock3 = new Mock<IMyService>(25, true);
        var mock4 = new Mock<IMyService>("123");
    }

    private void TestBad2()
    {
        var mock1 = new Mock<IMyService>(MockBehavior.Default, "123");
        var mock2 = new Mock<IMyService>(MockBehavior.Strict, 25, true);
        var mock3 = new Mock<IMyService>(MockBehavior.Default, "123");
        var mock4 = new Mock<IMyService>(MockBehavior.Loose, 25, true);
    }

    private void TestGood1()
    {
        var mock1 = new Mock<IMyService>();
        var mock2 = new Mock<IMyService>(MockBehavior.Default);
        var mock3 = new Mock<IMyService>(MockBehavior.Strict);
        var mock4 = new Mock<IMyService>(MockBehavior.Loose);
        var mock5 = new Mock<IMyService>(MockBehavior.Default);
    }
}
