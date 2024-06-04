using Moq;

#pragma warning disable SA1402 // File may only contain a single class
#pragma warning disable SA1502 // Element must not be on a single line
#pragma warning disable SA1602 // Undocumented enum values
#pragma warning disable SA1649 // File name must match first type name
#pragma warning disable RCS1163 // Unused parameter
#pragma warning disable RCS1213 // Remove unused member declaration
#pragma warning disable IDE0051 // Unused private member
#pragma warning disable IDE0059 // Unnecessary value assignment
#pragma warning disable IDE0060 // Unused parameter
namespace NoConstructorArgumentsForInterfaceMock_1;

internal interface IMyService
{
    void Do(string s);
}

internal class MyUnitTests
{
    private void TestBad()
    {
        Mock<IMyService>? mock1 = new Moq.Mock<IMyService>(25, true);
        Mock<IMyService>? mock2 = new Mock<IMyService>("123");
        Mock<IMyService>? mock3 = new Mock<NoConstructorArgumentsForInterfaceMock_1.IMyService>(25, true);
        Mock<IMyService>? mock4 = new Moq.Mock<NoConstructorArgumentsForInterfaceMock_1.IMyService>("123");
    }

    private void TestBad2()
    {
        Mock<IMyService>? mock1 = new Moq.Mock<IMyService>(Moq.MockBehavior.Default, "123");
        Mock<IMyService>? mock2 = new Mock<IMyService>(MockBehavior.Strict, 25, true);
        Mock<IMyService>? mock3 = new Mock<NoConstructorArgumentsForInterfaceMock_1.IMyService>(Moq.MockBehavior.Default, "123");
        Mock<IMyService>? mock4 = new Moq.Mock<NoConstructorArgumentsForInterfaceMock_1.IMyService>(MockBehavior.Loose, 25, true);
    }

    private void TestGood1()
    {
        Mock<IMyService>? mock1 = new Moq.Mock<IMyService>();
        Mock<IMyService>? mock2 = new Moq.Mock<IMyService>(MockBehavior.Default);
        Mock<IMyService>? mock3 = new Mock<IMyService>(MockBehavior.Strict);
        Mock<IMyService>? mock4 = new Mock<NoConstructorArgumentsForInterfaceMock_1.IMyService>(MockBehavior.Loose);
        Mock<IMyService>? mock5 = new Moq.Mock<NoConstructorArgumentsForInterfaceMock_1.IMyService>(MockBehavior.Default);
    }
}
