namespace Moq.Analyzers.Test.Data.AbstractClass.WithArgsNonePassed;

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
