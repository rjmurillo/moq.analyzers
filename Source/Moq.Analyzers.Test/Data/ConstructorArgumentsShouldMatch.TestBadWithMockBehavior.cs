using System;
using System.Collections.Generic;
using Moq;

namespace ConstructorArgumentsShouldMatchTestBadWithMockBehavior;

internal class Foo
{
    public Foo(string s) { }

    public Foo(bool b, int i) { }

    public Foo(params DateTime[] dates) { }

    public Foo(List<string> l, string s = "A") { }
}

internal class MyUnitTests
{
    private void TestBadWithMockBehavior()
    {
        var mock1 = new Mock<Foo>(MockBehavior.Strict, 4, true);
        var mock2 = new Mock<Foo>(MockBehavior.Loose, 5, true);
        var mock3 = new Mock<Foo>(MockBehavior.Loose, "2", 6);
    }
}
