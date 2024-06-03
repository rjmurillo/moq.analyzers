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
        var mock1 = new Moq.Mock<IMyService>(1, true);
        var mock2 = new Moq.Mock<NoConstructorArgumentsForInterfaceMock_2.IMyService>("2");
        var mock3 = new Moq.Mock<IMyService>(Moq.MockBehavior.Default, "3");
        var mock4 = new Moq.Mock<NoConstructorArgumentsForInterfaceMock_2.IMyService>(MockBehavior.Loose, 4, true);
        var mock5 = new Moq.Mock<IMyService>(MockBehavior.Default);
        var mock6 = new Moq.Mock<NoConstructorArgumentsForInterfaceMock_2.IMyService>(MockBehavior.Default);
    }

    private void TestRealMoqWithGoodParameters()
    {
        var mock1 = new Moq.Mock<IMyService>(Moq.MockBehavior.Default);
        var mock2 = new Moq.Mock<NoConstructorArgumentsForInterfaceMock_2.IMyService>(Moq.MockBehavior.Default);
    }

    private void TestFakeMoq()
    {
        var mock1 = new Mock<IMyService>("4");
        var mock2 = new Mock<NoConstructorArgumentsForInterfaceMock_2.IMyService>(5, true);
        var mock3 = new Mock<IMyService>(MockBehavior.Strict, 6, true);
        var mock4 = new Mock<NoConstructorArgumentsForInterfaceMock_2.IMyService>(Moq.MockBehavior.Default, "5");
        var mock5 = new Mock<IMyService>(MockBehavior.Strict);
        var mock6 = new Mock<NoConstructorArgumentsForInterfaceMock_2.IMyService>(MockBehavior.Loose);
    }
}
