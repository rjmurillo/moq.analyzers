using Moq;
using System;
using System.Collections.Generic;

namespace MockClassWithParameters
{
    class Foo
    {
        public Foo(string s) { }

        public Foo(bool b, int i) { }

        public Foo(params DateTime[] dates) { }

        public Foo(List<string> l, string s = "A") { }
    }

    abstract class FooAbstract
    {
        public FooAbstract(string s) { }
    }

    class MyUnitTests
    {
        void TestBad()
        {
            var mock1 = new Moq.Mock<Foo>(1, true);
            var mock2 = new Mock<MockClassWithParameters.Foo>(2, true);
            var mock3 = new Mock<MockClassWithParameters.Foo>("1", 3);
            var mock4 = new Mock<MockClassWithParameters.Foo>(new int[] { 1, 2, 3 });
        }

        void TestBad2()
        {
            var mock1 = new Mock<Foo>(MockBehavior.Strict, 4, true);
            var mock2 = new Moq.Mock<MockClassWithParameters.Foo>(MockBehavior.Loose, 5, true);
            var mock3 = new Moq.Mock<MockClassWithParameters.Foo>(MockBehavior.Loose, "2", 6);
        }

        void TestGood1()
        {
            var mock1 = new Moq.Mock<Foo>(MockBehavior.Default);
            var mock2 = new Mock<Foo>(MockBehavior.Strict);
            var mock3 = new Mock<MockClassWithParameters.Foo>(MockBehavior.Loose);
            var mock4 = new Moq.Mock<MockClassWithParameters.Foo>(MockBehavior.Default);

            var mock5 = new Mock<Foo>("3");
            var mock6 = new Moq.Mock<MockClassWithParameters.Foo>("4");
            var mock7 = new Moq.Mock<Foo>(Moq.MockBehavior.Default, "5");
            var mock8 = new Mock<MockClassWithParameters.Foo>(Moq.MockBehavior.Default, "6");

            var mock9 = new Moq.Mock<MockClassWithParameters.Foo>(false, 0);
            var mock10 = new Moq.Mock<MockClassWithParameters.Foo>(Moq.MockBehavior.Default, true, 1);

            var mock11 = new Mock<Foo>(DateTime.Now, DateTime.Now);
            var mock12 = new Mock<Foo>(MockBehavior.Default, DateTime.Now, DateTime.Now);

            var mock13 = new Mock<Foo>(new List<string>(), "7");
            var mock14 = new Mock<Foo>(new List<string>());
            var mock15 = new Mock<Foo>(MockBehavior.Default, new List<string>(), "8");
            var mock16 = new Mock<Foo>(MockBehavior.Default, new List<string>());
        }

        void TestAbstractGood()
        {
            var mock1 = new Mock<FooAbstract>("9");
            var mock2 = new Mock<FooAbstract>(MockBehavior.Default, "10");
        }

    }
}