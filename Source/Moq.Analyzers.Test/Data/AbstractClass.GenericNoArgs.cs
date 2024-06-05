namespace Moq.Analyzers.Test.Data.AbstractClass.GenericNoArgs;

internal abstract class AbstractGenericClassDefaultCtor<T>
{
    protected AbstractGenericClassDefaultCtor()
    {
    }
}

internal abstract class AbstractGenericClassWithCtor<T>
{
    protected AbstractGenericClassWithCtor(int a)
    {
    }

    protected AbstractGenericClassWithCtor(int a, string b)
    {
    }
}

internal abstract class AbstractClassDefaultCtor
{
    protected AbstractClassDefaultCtor()
    {
    }
}

internal abstract class AbstractClassWithCtor
{
    protected AbstractClassWithCtor(int a)
    {
    }

    protected AbstractClassWithCtor(int a, string b)
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
