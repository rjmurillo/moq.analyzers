using Moq;
using System;

namespace NoSealedClassMocks
{
    sealed class FooSealed
    {
        void Do(string s) { }

    }

    class MyUnitTests
    {
        void Test()
        {
            var mock1 = new Moq.Mock<FooSealed>();
            var mock2 = new Mock<FooSealed>();
            var mock3 = new Mock<NoSealedClassMocks.FooSealed>();
            var mock4 = new Moq.Mock<NoSealedClassMocks.FooSealed>();
        }

        void Tes2t()
        {
            new Mock<Action<int>>();
            new Mock<EventHandler>();
        }
    }
}