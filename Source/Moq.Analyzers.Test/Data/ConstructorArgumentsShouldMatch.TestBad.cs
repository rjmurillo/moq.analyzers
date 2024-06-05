using System;
using System.Collections.Generic;
using Moq;

namespace ConstructorArgumentsShouldMatch.TestBad;

internal class Foo
{
    public Foo(string s) { }

    public Foo(bool b, int i) { }

    public Foo(params DateTime[] dates) { }

    public Foo(List<string> l, string s = "A") { }
}

internal class MyUnitTests
{
    private void TestBad()
    {
        var mock1 = new Mock<Foo>(1, true);
        var mock2 = new Mock<Foo>(2, true);
        var mock3 = new Mock<Foo>("1", 3);
        var mock4 = new Mock<Foo>(new int[] { 1, 2, 3 });
    }
}
