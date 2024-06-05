namespace Moq.Analyzers.Test.DataAbstractClass.WithArgsPassed;

internal abstract class AbstractGenericClassWithCtor<T>
{
    protected AbstractGenericClassWithCtor(int a)
    {
    }

    protected AbstractGenericClassWithCtor(int a, string b)
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
    private void TestForBaseWithArgsPassed()
    {
        var mock = new Mock<AbstractClassWithCtor>(42);
        var mock2 = new Mock<AbstractClassWithCtor>(MockBehavior.Default, 42);

        var mock3 = new Mock<AbstractClassWithCtor>(42, "42");
        var mock4 = new Mock<AbstractClassWithCtor>(MockBehavior.Default, 42, "42");

        var mock5 = new Mock<AbstractGenericClassWithCtor<object>>(42);
        var mock6 = new Mock<AbstractGenericClassWithCtor<object>>(MockBehavior.Default, 42);
    }
}
