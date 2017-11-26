using Moq;

namespace MockInterfaceWithParameters
{
    interface IMyService
    {
        void Do(string s);

    }

    class MyUnitTests
    {
        void TestBad()
        {
            var mock1 = new Moq.Mock<IMyService>(25, true);
            var mock2 = new Mock<IMyService>("123");
            var mock3 = new Mock<MockInterfaceWithParameters.IMyService>(25, true);
            var mock4 = new Moq.Mock<MockInterfaceWithParameters.IMyService>("123");
        }

        void TestBad2()
        {
            var mock1 = new Moq.Mock<IMyService>(Moq.MockBehavior.Default, "123");
            var mock2 = new Mock<IMyService>(MockBehavior.Strict, 25, true);
            var mock3 = new Mock<MockInterfaceWithParameters.IMyService>(Moq.MockBehavior.Default, "123");
            var mock4 = new Moq.Mock<MockInterfaceWithParameters.IMyService>(MockBehavior.Loose, 25, true);
        }

        void TestGood1()
        {
            var mock1 = new Moq.Mock<IMyService>();
            var mock2 = new Moq.Mock<IMyService>(MockBehavior.Default);
            var mock3 = new Mock<IMyService>(MockBehavior.Strict);
            var mock4 = new Mock<MockInterfaceWithParameters.IMyService>(MockBehavior.Loose);
            var mock5 = new Moq.Mock<MockInterfaceWithParameters.IMyService>(MockBehavior.Default);
        }

    }
}