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
        var mock1 = new Moq.Mock<IMyService>(25, true);
        var mock2 = new Mock<IMyService>("123");
        var mock3 = new Mock<NoConstructorArgumentsForInterfaceMock_1.IMyService>(25, true);
        var mock4 = new Moq.Mock<NoConstructorArgumentsForInterfaceMock_1.IMyService>("123");
    }

    private void TestBad2()
    {
        var mock1 = new Moq.Mock<IMyService>(Moq.MockBehavior.Default, "123");
        var mock2 = new Mock<IMyService>(MockBehavior.Strict, 25, true);
        var mock3 = new Mock<NoConstructorArgumentsForInterfaceMock_1.IMyService>(Moq.MockBehavior.Default, "123");
        var mock4 = new Moq.Mock<NoConstructorArgumentsForInterfaceMock_1.IMyService>(MockBehavior.Loose, 25, true);
    }

    private void TestGood1()
    {
        var mock1 = new Moq.Mock<IMyService>();
        var mock2 = new Moq.Mock<IMyService>(MockBehavior.Default);
        var mock3 = new Mock<IMyService>(MockBehavior.Strict);
        var mock4 = new Mock<NoConstructorArgumentsForInterfaceMock_1.IMyService>(MockBehavior.Loose);
        var mock5 = new Moq.Mock<NoConstructorArgumentsForInterfaceMock_1.IMyService>(MockBehavior.Default);
    }
}
