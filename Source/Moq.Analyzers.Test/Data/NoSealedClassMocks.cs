using System;
using Moq;

namespace NoSealedClassMocks;

internal sealed class FooSealed { }

internal class Foo { }

internal class MyUnitTests
{
    private void Test()
    {
        var mock = new Mock<FooSealed>();
    }

    private void Test2()
    {
        var mock = new Mock<Foo>();
    }
}
