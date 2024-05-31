namespace Moq.Analyzers.Test.Data
{
    internal class MyBadUnitTests
    {
        private void TestBad()
        {
            // The class has a constructor that takes an int but passes a string
            var mock = new Mock<AbstractClassWithCtor>("42");

            var mock1 = new Mock<AbstractClassWithCtor>("42", 42);

            var mock2 = new Mock<AbstractClassWithCtor>(42);

            var mock3 = new Mock<AbstractClassDefaultCtor>(42);
        }

        private void TestBadWithGeneric()
        {
            // The class has a constructor that takes an int but passes a string
            var mock = new Mock<AbstractGenericClassWithCtor<object>>("42");

            var mock1 = new Mock<AbstractGenericClassWithCtor<object>>("42", 42);

            var mock2 = new Mock<AbstractGenericClassWithCtor<object>>(42);

            var mock3 = new Mock<AbstractGenericClassDefaultCtor<object>>(42);
        }
    }
}
