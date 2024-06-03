using System;
using System.Collections.Generic;
using Moq;

#pragma warning disable SA1402 // File may only contain a single class
#pragma warning disable SA1502 // Element must not be on a single line
#pragma warning disable SA1602 // Undocumented enum values
#pragma warning disable SA1649 // File name must match first type name
#pragma warning disable RCS1160 // Public constructor in abstract class
#pragma warning disable RCS1163 // Unused parameter
#pragma warning disable RCS1213 // Remove unused member declaration
#pragma warning disable IDE0051 // Unused private member
#pragma warning disable IDE0059 // Unnecessary value assignment
#pragma warning disable IDE0060 // Unused parameter
namespace ConstructorArgumentsShouldMatch;

#pragma warning disable SA1402 // File may only contain a single class
internal class Foo
{
    public Foo(string s) { }

    public Foo(bool b, int i) { }

    public Foo(params DateTime[] dates) { }

    public Foo(List<string> l, string s = "A") { }
}

internal class MyUnitTests
#pragma warning restore SA1402 // File may only contain a single class
{
    private void TestBad()
    {
        var mock1 = new Moq.Mock<Foo>(1, true);
        var mock2 = new Mock<ConstructorArgumentsShouldMatch.Foo>(2, true);
        var mock3 = new Mock<ConstructorArgumentsShouldMatch.Foo>("1", 3);
        var mock4 = new Mock<ConstructorArgumentsShouldMatch.Foo>(new int[] { 1, 2, 3 });
    }

    private void TestBad2()
    {
        var mock1 = new Mock<Foo>(MockBehavior.Strict, 4, true);
        var mock2 = new Moq.Mock<ConstructorArgumentsShouldMatch.Foo>(MockBehavior.Loose, 5, true);
        var mock3 = new Moq.Mock<ConstructorArgumentsShouldMatch.Foo>(MockBehavior.Loose, "2", 6);
    }

    private void TestGood1()
    {
        var mock1 = new Moq.Mock<Foo>(MockBehavior.Default);
        var mock2 = new Mock<Foo>(MockBehavior.Strict);
        var mock3 = new Mock<ConstructorArgumentsShouldMatch.Foo>(MockBehavior.Loose);
        var mock4 = new Moq.Mock<ConstructorArgumentsShouldMatch.Foo>(MockBehavior.Default);

        var mock5 = new Mock<Foo>("3");
        var mock6 = new Moq.Mock<ConstructorArgumentsShouldMatch.Foo>("4");
        var mock7 = new Moq.Mock<Foo>(Moq.MockBehavior.Default, "5");
        var mock8 = new Mock<ConstructorArgumentsShouldMatch.Foo>(Moq.MockBehavior.Default, "6");

        var mock9 = new Moq.Mock<ConstructorArgumentsShouldMatch.Foo>(false, 0);
        var mock10 = new Moq.Mock<ConstructorArgumentsShouldMatch.Foo>(Moq.MockBehavior.Default, true, 1);

        var mock11 = new Mock<Foo>(DateTime.Now, DateTime.Now);
        var mock12 = new Mock<Foo>(MockBehavior.Default, DateTime.Now, DateTime.Now);

        var mock13 = new Mock<Foo>(new List<string>(), "7");
        var mock14 = new Mock<Foo>(new List<string>());
        var mock15 = new Mock<Foo>(MockBehavior.Default, new List<string>(), "8");
        var mock16 = new Mock<Foo>(MockBehavior.Default, new List<string>());
    }
}
