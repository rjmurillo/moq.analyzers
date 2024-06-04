namespace Moq.Analyzers.Test.Data;

internal class MyBadUnitTests
{
    private void TestBad()
    {
        // The class has a ctor that takes an Int32 but passes a String
        Mock<AbstractClassWithCtor>? mock = new Mock<AbstractClassWithCtor>("42");

        // The class has a ctor with two arguments [Int32, String], but they are passed in reverse order
        Mock<AbstractClassWithCtor>? mock1 = new Mock<AbstractClassWithCtor>("42", 42);

        // The class has a ctor but does not take any arguments
        Mock<AbstractClassDefaultCtor>? mock2 = new Mock<AbstractClassDefaultCtor>(42);
    }

    private void TestBadWithGeneric()
    {
        // The class has a constructor that takes an Int32 but passes a String
        Mock<AbstractGenericClassWithCtor<object>>? mock = new Mock<AbstractGenericClassWithCtor<object>>("42");

        // The class has a ctor with two arguments [Int32, String], but they are passed in reverse order
        Mock<AbstractGenericClassWithCtor<object>>? mock1 = new Mock<AbstractGenericClassWithCtor<object>>("42", 42);

        // The class has a ctor but does not take any arguments
        Mock<AbstractGenericClassDefaultCtor<object>>? mock2 = new Mock<AbstractGenericClassDefaultCtor<object>>(42);
    }
}
