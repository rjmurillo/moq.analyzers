namespace Moq.Analyzers.Test.Data
{
    internal abstract class AbstractClassDefaultCtor
    {
        protected AbstractClassDefaultCtor()
        {
        }
    }

    internal abstract class AbstractClassWithCtor
    {
        protected AbstractClassWithCtor(int a)
        {
        }
    }

    internal class MyUnitTests
    {
        // Base case that we can handle abstract types
        private void TestForBaseNoArgs()
        {
            var mock = new Mock<AbstractClassDefaultCtor>();
            mock.As<AbstractClassDefaultCtor>();

            var mock2 = new Mock<AbstractClassWithCtor>();
            var mock3 = new Mock<AbstractClassDefaultCtor>(MockBehavior.Default);
        }

        // This is syntatically not allowed by C#, but you can do it with Moq
        private void TestForBaseWithArgsNonePassed()
        {
            var mock = new Mock<AbstractClassWithCtor>();
            mock.As<AbstractClassWithCtor>();
        }

        private void TestForBaseWithArgsPassed()
        {
            var mock2 = new Mock<AbstractClassWithCtor>(42);
            var mock3 = new Mock<AbstractClassWithCtor>(MockBehavior.Default, 42);
        }
    }
}
