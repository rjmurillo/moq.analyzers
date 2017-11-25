using Moq;

namespace MockInterfaceWithParameters
{
    interface IMyService
    {
        void Do(string s);

    }

    class MyUnitTests
    {
        void Test()
        {
            var mock1 = new Moq.Mock<IMyService>(0);
            var mock2 = new Mock<IMyService>(0);
            var mock3 = new Mock<MockInterfaceWithParameters.IMyService>(0);
            var mock4 = new Moq.Mock<MockInterfaceWithParameters.IMyService>(0);
        }
    }
}