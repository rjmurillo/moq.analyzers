using System;
using Moq;

namespace NoSealedClassMocks.NotSealed;

internal sealed class FooSealed { }

internal class Foo { }

internal class MyUnitTests
{
    private void NotSealed()
    {
        var mock = new Mock<Foo>();
    }
}
