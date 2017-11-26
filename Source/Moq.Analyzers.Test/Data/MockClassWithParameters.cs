using Moq;
using System;
using System.Collections.Generic;

namespace MockClassWithParameters
{
    class MyService
    {
        public MyService(string s) { }

        public MyService(bool b, int i) { }

        public MyService(params DateTime[] dates) { }

        public MyService(List<string> l, string s = "A") { }
    }

    class MyUnitTests
    {
        void TestBad()
        {
            var mock1 = new Moq.Mock<MyService>(1, true);
            var mock2 = new Mock<MockClassWithParameters.MyService>(2, true);
            var mock3 = new Mock<MockClassWithParameters.MyService>("1", 3);
            var mock4 = new Mock<MockClassWithParameters.MyService>(new int[] { 1, 2, 3 });
        }

        void TestBad2()
        {
            var mock1 = new Mock<MyService>(MockBehavior.Strict, 4, true);
            var mock2 = new Moq.Mock<MockClassWithParameters.MyService>(MockBehavior.Loose, 5, true);
            var mock3 = new Moq.Mock<MockClassWithParameters.MyService>(MockBehavior.Loose, "2", 6);
        }

        void TestGood1()
        {
            var mock1 = new Moq.Mock<MyService>(MockBehavior.Default);
            var mock2 = new Mock<MyService>(MockBehavior.Strict);
            var mock3 = new Mock<MockClassWithParameters.MyService>(MockBehavior.Loose);
            var mock4 = new Moq.Mock<MockClassWithParameters.MyService>(MockBehavior.Default);

            var mock5 = new Mock<MyService>("3");
            var mock6 = new Moq.Mock<MockClassWithParameters.MyService>("4");
            var mock7 = new Moq.Mock<MyService>(Moq.MockBehavior.Default, "5");
            var mock8 = new Mock<MockClassWithParameters.MyService>(Moq.MockBehavior.Default, "6");

            var mock9 = new Moq.Mock<MockClassWithParameters.MyService>(false, 0);
            var mock10 = new Moq.Mock<MockClassWithParameters.MyService>(Moq.MockBehavior.Default, true, 1);

            var mock11 = new Mock<MyService>(DateTime.Now, DateTime.Now);
            var mock12 = new Mock<MyService>(MockBehavior.Default, DateTime.Now, DateTime.Now);

            var mock13 = new Mock<MyService>(new List<string>(), "7");
            var mock14 = new Mock<MyService>(new List<string>());
            var mock15 = new Mock<MyService>(MockBehavior.Default, new List<string>(), "8");
            var mock16 = new Mock<MyService>(MockBehavior.Default, new List<string>());
        }

    }
}