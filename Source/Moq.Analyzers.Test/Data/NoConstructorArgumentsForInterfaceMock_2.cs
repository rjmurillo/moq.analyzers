#pragma warning disable SA1402 // File may only contain a single class
#pragma warning disable SA1502 // Element must not be on a single line
#pragma warning disable SA1602 // Undocumented enum values
#pragma warning disable SA1649 // File name must match first type name
#pragma warning disable RCS1163 // Unused parameter
#pragma warning disable RCS1213 // Remove unused member declaration
#pragma warning disable IDE0051 // Unused private member
#pragma warning disable IDE0059 // Unnecessary value assignment
#pragma warning disable IDE0060 // Unused parameter
namespace NoConstructorArgumentsForInterfaceMock_2;

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
        Moq.Mock<IMyService>? mock1 = new Moq.Mock<IMyService>(1, true);
        Moq.Mock<IMyService>? mock2 = new Moq.Mock<NoConstructorArgumentsForInterfaceMock_2.IMyService>("2");
        Moq.Mock<IMyService>? mock3 = new Moq.Mock<IMyService>(Moq.MockBehavior.Default, "3");
        Moq.Mock<IMyService>? mock4 = new Moq.Mock<NoConstructorArgumentsForInterfaceMock_2.IMyService>(MockBehavior.Loose, 4, true);
        Moq.Mock<IMyService>? mock5 = new Moq.Mock<IMyService>(MockBehavior.Default);
        Moq.Mock<IMyService>? mock6 = new Moq.Mock<NoConstructorArgumentsForInterfaceMock_2.IMyService>(MockBehavior.Default);
    }

    private void TestRealMoqWithGoodParameters()
    {
        Moq.Mock<IMyService>? mock1 = new Moq.Mock<IMyService>(Moq.MockBehavior.Default);
        Moq.Mock<IMyService>? mock2 = new Moq.Mock<NoConstructorArgumentsForInterfaceMock_2.IMyService>(Moq.MockBehavior.Default);
    }

    private void TestFakeMoq()
    {
        Mock<IMyService>? mock1 = new Mock<IMyService>("4");
        Mock<IMyService>? mock2 = new Mock<NoConstructorArgumentsForInterfaceMock_2.IMyService>(5, true);
        Mock<IMyService>? mock3 = new Mock<IMyService>(MockBehavior.Strict, 6, true);
        Mock<IMyService>? mock4 = new Mock<NoConstructorArgumentsForInterfaceMock_2.IMyService>(Moq.MockBehavior.Default, "5");
        Mock<IMyService>? mock5 = new Mock<IMyService>(MockBehavior.Strict);
        Mock<IMyService>? mock6 = new Mock<NoConstructorArgumentsForInterfaceMock_2.IMyService>(MockBehavior.Loose);
    }
}
