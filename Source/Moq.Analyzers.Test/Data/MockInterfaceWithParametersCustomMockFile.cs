using Moq;

namespace MockInterfaceWithParametersCustomMockFile
{
    interface IMyService
    {
        void Do(string s);
    }

    public class Mock<T> where T : class
    {
        public Mock() { }
        public Mock(params object[] ar) { }
        public Mock(MockBehavior behavior) { }
        public Mock(MockBehavior behavior, params object[] args) { }
    }

    public enum MockBehavior {
        Default,
        Strict,
        Loose
    }

    class MyUnitTests
    {
        void TestRealMoqWithBadParameters()
        {
            var mock1 = new Moq.Mock<IMyService>(1, true);
            var mock2 = new Moq.Mock<MockInterfaceWithParametersCustomMockFile.IMyService>("2");
            var mock3 = new Moq.Mock<IMyService>(Moq.MockBehavior.Default, "3");
            var mock4 = new Moq.Mock<MockInterfaceWithParametersCustomMockFile.IMyService>(MockBehavior.Loose, 4, true);
            var mock5 = new Moq.Mock<IMyService>(MockBehavior.Default);
            var mock6 = new Moq.Mock<MockInterfaceWithParametersCustomMockFile.IMyService>(MockBehavior.Default);
        }

        void TestRealMoqWithGoodParameters()
        {
            var mock1 = new Moq.Mock<IMyService>(Moq.MockBehavior.Default);
            var mock2 = new Moq.Mock<MockInterfaceWithParametersCustomMockFile.IMyService>(Moq.MockBehavior.Default);
        }

        void TestFakeMoq()
        {
            var mock1 = new Mock<IMyService>("4");
            var mock2 = new Mock<MockInterfaceWithParametersCustomMockFile.IMyService>(5, true);
            var mock3 = new Mock<IMyService>(MockBehavior.Strict, 6, true);
            var mock4 = new Mock<MockInterfaceWithParametersCustomMockFile.IMyService>(Moq.MockBehavior.Default, "5");
            var mock5 = new Mock<IMyService>(MockBehavior.Strict);
            var mock6 = new Mock<MockInterfaceWithParametersCustomMockFile.IMyService>(MockBehavior.Loose);
        }
    }
}