using System;
using Moq;

namespace NoSealedClassMocks.Sealed;

internal sealed class FooSealed { }

internal class Foo { }

internal class MyUnitTests
{
    private void Sealed()
    {
        var mock = new Mock<FooSealed>();
    }
}
