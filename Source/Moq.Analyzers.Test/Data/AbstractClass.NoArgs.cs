namespace Moq.Analyzers.Test.Data.AbstractClass.NoArgs;

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
    // Base case that we can handle abstract types
    private void TestForBaseNoArgs()
    {
        var mock = new Mock<AbstractClassDefaultCtor>();
        mock.As<AbstractClassDefaultCtor>();

        var mock2 = new Mock<AbstractClassWithCtor>();
        var mock3 = new Mock<AbstractClassDefaultCtor>(MockBehavior.Default);
    }
}
