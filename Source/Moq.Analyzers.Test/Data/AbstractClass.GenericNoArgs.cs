namespace Moq.Analyzers.Test.Data.AbstractClass.GenericNoArgs;

internal abstract class AbstractGenericClassDefaultCtor<T>
{
    protected AbstractGenericClassDefaultCtor()
    {
    }
}

internal class MyUnitTests
{
    private void TestForBaseGenericNoArgs()
    {
        var mock = new Mock<AbstractGenericClassDefaultCtor<object>>();
        mock.As<AbstractGenericClassDefaultCtor<object>>();

        var mock1 = new Mock<AbstractGenericClassDefaultCtor<object>>();

        var mock2 = new Mock<AbstractGenericClassDefaultCtor<object>>(MockBehavior.Default);
    }
}
