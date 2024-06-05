using Moq;

namespace NoConstructorArgumentsForInterfaceMock.TestBad;

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
}
