﻿namespace Moq.Analyzers.Test.Data.AbstractClass.MismatchArgs;

internal abstract class AbstractGenericClassDefaultCtor<T>
{
    protected AbstractGenericClassDefaultCtor()
    {
    }
}

internal abstract class AbstractGenericClassWithCtor<T>
{
    protected AbstractGenericClassWithCtor(int a)
    {
    }

    protected AbstractGenericClassWithCtor(int a, string b)
    {
    }
}

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

    protected AbstractClassWithCtor(int a, string b)
    {
    }
}

internal class MyUnitTests
{
    private void TestBad()
    {
        // The class has a ctor that takes an Int32 but passes a String
        var mock = new Mock<AbstractClassWithCtor>("42");

        // The class has a ctor with two arguments [Int32, String], but they are passed in reverse order
        var mock1 = new Mock<AbstractClassWithCtor>("42", 42);

        // The class has a ctor but does not take any arguments
        var mock2 = new Mock<AbstractClassDefaultCtor>(42);
    }
}
