namespace Moq.Analyzers.Test.Data;

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
        Mock<AbstractClassDefaultCtor>? mock = new Mock<AbstractClassDefaultCtor>();
        mock.As<AbstractClassDefaultCtor>();

        Mock<AbstractClassWithCtor>? mock2 = new Mock<AbstractClassWithCtor>();
        Mock<AbstractClassDefaultCtor>? mock3 = new Mock<AbstractClassDefaultCtor>(MockBehavior.Default);
    }

    private void TestForBaseGenericNoArgs()
    {
        Mock<AbstractGenericClassDefaultCtor<object>>? mock = new Mock<AbstractGenericClassDefaultCtor<object>>();
        mock.As<AbstractGenericClassDefaultCtor<object>>();

        Mock<AbstractGenericClassDefaultCtor<object>>? mock1 = new Mock<AbstractGenericClassDefaultCtor<object>>();

        Mock<AbstractGenericClassDefaultCtor<object>>? mock2 = new Mock<AbstractGenericClassDefaultCtor<object>>(MockBehavior.Default);
    }

    // This is syntatically not allowed by C#, but you can do it with Moq
    private void TestForBaseWithArgsNonePassed()
    {
        Mock<AbstractClassWithCtor>? mock = new Mock<AbstractClassWithCtor>();
        mock.As<AbstractClassWithCtor>();
    }

    private void TestForBaseWithArgsPassed()
    {
        Mock<AbstractClassWithCtor>? mock = new Mock<AbstractClassWithCtor>(42);
        Mock<AbstractClassWithCtor>? mock2 = new Mock<AbstractClassWithCtor>(MockBehavior.Default, 42);

        Mock<AbstractClassWithCtor>? mock3 = new Mock<AbstractClassWithCtor>(42, "42");
        Mock<AbstractClassWithCtor>? mock4 = new Mock<AbstractClassWithCtor>(MockBehavior.Default, 42, "42");

        Mock<AbstractGenericClassWithCtor<object>>? mock5 = new Mock<AbstractGenericClassWithCtor<object>>(42);
        Mock<AbstractGenericClassWithCtor<object>>? mock6 = new Mock<AbstractGenericClassWithCtor<object>>(MockBehavior.Default, 42);
    }
}
