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
        Mock<Foo>? mock1 = new Moq.Mock<Foo>(1, true);
        Mock<Foo>? mock2 = new Mock<ConstructorArgumentsShouldMatch.Foo>(2, true);
        Mock<Foo>? mock3 = new Mock<ConstructorArgumentsShouldMatch.Foo>("1", 3);
        Mock<Foo>? mock4 = new Mock<ConstructorArgumentsShouldMatch.Foo>(new int[] { 1, 2, 3 });
    }

    private void TestBad2()
    {
        Mock<Foo>? mock1 = new Mock<Foo>(MockBehavior.Strict, 4, true);
        Mock<Foo>? mock2 = new Moq.Mock<ConstructorArgumentsShouldMatch.Foo>(MockBehavior.Loose, 5, true);
        Mock<Foo>? mock3 = new Moq.Mock<ConstructorArgumentsShouldMatch.Foo>(MockBehavior.Loose, "2", 6);
    }

    private void TestGood1()
    {
        Mock<Foo>? mock1 = new Moq.Mock<Foo>(MockBehavior.Default);
        Mock<Foo>? mock2 = new Mock<Foo>(MockBehavior.Strict);
        Mock<Foo>? mock3 = new Mock<ConstructorArgumentsShouldMatch.Foo>(MockBehavior.Loose);
        Mock<Foo>? mock4 = new Moq.Mock<ConstructorArgumentsShouldMatch.Foo>(MockBehavior.Default);

        Mock<Foo>? mock5 = new Mock<Foo>("3");
        Mock<Foo>? mock6 = new Moq.Mock<ConstructorArgumentsShouldMatch.Foo>("4");
        Mock<Foo>? mock7 = new Moq.Mock<Foo>(Moq.MockBehavior.Default, "5");
        Mock<Foo>? mock8 = new Mock<ConstructorArgumentsShouldMatch.Foo>(Moq.MockBehavior.Default, "6");

        Mock<Foo>? mock9 = new Moq.Mock<ConstructorArgumentsShouldMatch.Foo>(false, 0);
        Mock<Foo>? mock10 = new Moq.Mock<ConstructorArgumentsShouldMatch.Foo>(Moq.MockBehavior.Default, true, 1);

        Mock<Foo>? mock11 = new Mock<Foo>(DateTime.Now, DateTime.Now);
        Mock<Foo>? mock12 = new Mock<Foo>(MockBehavior.Default, DateTime.Now, DateTime.Now);

        Mock<Foo>? mock13 = new Mock<Foo>(new List<string>(), "7");
        Mock<Foo>? mock14 = new Mock<Foo>(new List<string>());
        Mock<Foo>? mock15 = new Mock<Foo>(MockBehavior.Default, new List<string>(), "8");
        Mock<Foo>? mock16 = new Mock<Foo>(MockBehavior.Default, new List<string>());
    }
}
