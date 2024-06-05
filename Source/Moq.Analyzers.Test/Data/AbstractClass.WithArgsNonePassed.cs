namespace Moq.Analyzers.Test.Data.AbstractClass.WithArgsNonePassed;

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
    // This is syntatically not allowed by C#, but you can do it with Moq
    private void TestForBaseWithArgsNonePassed()
    {
        var mock = new Mock<AbstractClassWithCtor>();
        mock.As<AbstractClassWithCtor>();
    }
}
