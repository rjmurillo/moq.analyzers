namespace Moq.Analyzers.Test.Data
{
    internal class MyBadUnitTests
    {
        private void TestBad()
        {
            // The class has a constructor that takes an int but passes a string
            var mock = new Mock<AbstractClassWithCtor>("42");
        }
    }
}
