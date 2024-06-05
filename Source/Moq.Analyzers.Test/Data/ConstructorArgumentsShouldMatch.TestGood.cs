using System;
using System.Collections.Generic;
using Moq;

namespace ConstructorArgumentsShouldMatch.TestGood;

internal class Foo
{
    public Foo(string s) { }

    public Foo(bool b, int i) { }

    public Foo(params DateTime[] dates) { }

    public Foo(List<string> l, string s = "A") { }
}

internal class MyUnitTests
{
    private void TestGood()
    {
        var mock1 = new Mock<Foo>(MockBehavior.Default);
        var mock2 = new Mock<Foo>(MockBehavior.Strict);
        var mock3 = new Mock<Foo>(MockBehavior.Loose);
        var mock4 = new Mock<Foo>(MockBehavior.Default);

        var mock5 = new Mock<Foo>("3");
        var mock6 = new Mock<Foo>("4");
        var mock7 = new Mock<Foo>(MockBehavior.Default, "5");
        var mock8 = new Mock<Foo>(MockBehavior.Default, "6");

        var mock9 = new Mock<Foo>(false, 0);
        var mock10 = new Mock<Foo>(MockBehavior.Default, true, 1);

        var mock11 = new Mock<Foo>(DateTime.Now, DateTime.Now);
        var mock12 = new Mock<Foo>(MockBehavior.Default, DateTime.Now, DateTime.Now);

        var mock13 = new Mock<Foo>(new List<string>(), "7");
        var mock14 = new Mock<Foo>(new List<string>());
        var mock15 = new Mock<Foo>(MockBehavior.Default, new List<string>(), "8");
        var mock16 = new Mock<Foo>(MockBehavior.Default, new List<string>());
    }
}
