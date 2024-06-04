using System;
using Moq;

namespace NoSealedClassMocks;

internal sealed class FooSealed
{
    private void Do(string s) { }
}

internal class MyUnitTests
{
    private void Test()
    {
        var mock1 = new Moq.Mock<FooSealed>();
        var mock2 = new Mock<FooSealed>();
        var mock3 = new Mock<NoSealedClassMocks.FooSealed>();
        var mock4 = new Moq.Mock<NoSealedClassMocks.FooSealed>();
    }

    private void Test2()
    {
        new Mock<Action<int>>();
        new Mock<EventHandler>();
    }
}
