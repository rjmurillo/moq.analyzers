using Moq;

namespace MockSealedClass
{
    sealed class MyService
    {
        void Do(string s) { }

    }

    class MyUnitTests
    {
        void Test()
        {
            var mock1 = new Moq.Mock<MyService>();
            var mock2 = new Mock<MyService>();
            var mock3 = new Mock<MockSealedClass.MyService>();
            var mock4 = new Moq.Mock<MockSealedClass.MyService>();
        }

        void Test()
        {
            var mock1 = new Moq.Mock<Action<int>>();
            var mock1 = new Moq.Mock<EventHandler>();
        }
    }
}