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
        var mock = new Mock<AbstractClassDefaultCtor>();
        mock.As<AbstractClassDefaultCtor>();

        var mock2 = new Mock<AbstractClassWithCtor>();
        var mock3 = new Mock<AbstractClassDefaultCtor>(MockBehavior.Default);
    }

    private void TestForBaseGenericNoArgs()
    {
        var mock = new Mock<AbstractGenericClassDefaultCtor<object>>();
        mock.As<AbstractGenericClassDefaultCtor<object>>();

        var mock1 = new Mock<AbstractGenericClassDefaultCtor<object>>();

        var mock2 = new Mock<AbstractGenericClassDefaultCtor<object>>(MockBehavior.Default);
    }

    // This is syntatically not allowed by C#, but you can do it with Moq
    private void TestForBaseWithArgsNonePassed()
    {
        var mock = new Mock<AbstractClassWithCtor>();
        mock.As<AbstractClassWithCtor>();
    }

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

internal class MyBadUnitTests
{
    private void TestBad()
    {
        // The class has a ctor that takes an Int32 but passes a String
        var mock = new Mock<AbstractClassWithCtor>("42");

        // The class has a ctor with two arguments [Int32, String], but they are passed in reverse order
        var mock1 = new Mock<AbstractClassWithCtor>("42", 42);

        // The class has a ctor but does not take any arguments
        var mock2 = new Mock<AbstractClassDefaultCtor>(42);
    }

    private void TestBadWithGeneric()
    {
        // The class has a constructor that takes an Int32 but passes a String
        var mock = new Mock<AbstractGenericClassWithCtor<object>>("42");

        // The class has a ctor with two arguments [Int32, String], but they are passed in reverse order
        var mock1 = new Mock<AbstractGenericClassWithCtor<object>>("42", 42);

        // The class has a ctor but does not take any arguments
        var mock2 = new Mock<AbstractGenericClassDefaultCtor<object>>(42);
    }
}
