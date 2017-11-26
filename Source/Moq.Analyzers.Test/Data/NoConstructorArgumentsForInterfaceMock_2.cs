using Moq;

namespace NoConstructorArgumentsForInterfaceMock_2
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
            var mock2 = new Moq.Mock<NoConstructorArgumentsForInterfaceMock_2.IMyService>("2");
            var mock3 = new Moq.Mock<IMyService>(Moq.MockBehavior.Default, "3");
            var mock4 = new Moq.Mock<NoConstructorArgumentsForInterfaceMock_2.IMyService>(MockBehavior.Loose, 4, true);
            var mock5 = new Moq.Mock<IMyService>(MockBehavior.Default);
            var mock6 = new Moq.Mock<NoConstructorArgumentsForInterfaceMock_2.IMyService>(MockBehavior.Default);
        }

        void TestRealMoqWithGoodParameters()
        {
            var mock1 = new Moq.Mock<IMyService>(Moq.MockBehavior.Default);
            var mock2 = new Moq.Mock<NoConstructorArgumentsForInterfaceMock_2.IMyService>(Moq.MockBehavior.Default);
        }

        void TestFakeMoq()
        {
            var mock1 = new Mock<IMyService>("4");
            var mock2 = new Mock<NoConstructorArgumentsForInterfaceMock_2.IMyService>(5, true);
            var mock3 = new Mock<IMyService>(MockBehavior.Strict, 6, true);
            var mock4 = new Mock<NoConstructorArgumentsForInterfaceMock_2.IMyService>(Moq.MockBehavior.Default, "5");
            var mock5 = new Mock<IMyService>(MockBehavior.Strict);
            var mock6 = new Mock<NoConstructorArgumentsForInterfaceMock_2.IMyService>(MockBehavior.Loose);
        }
    }
}