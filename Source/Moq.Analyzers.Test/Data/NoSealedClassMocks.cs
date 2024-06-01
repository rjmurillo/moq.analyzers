using System;
using Moq;

#pragma warning disable SA1402 // File may only contain a single class
#pragma warning disable SA1502 // Element must not be on a single line
#pragma warning disable SA1602 // Undocumented enum values
#pragma warning disable SA1649 // File name must match first type name
#pragma warning disable RCS1163 // Unused parameter
#pragma warning disable RCS1213 // Remove unused member declaration
#pragma warning disable IDE0051 // Unused private member
#pragma warning disable IDE0059 // Unnecessary value assignment
#pragma warning disable IDE0060 // Unused parameter
namespace NoSealedClassMocks
{
    internal sealed class FooSealed
    {
        private void Do(string s) { }
    }

    internal class MyUnitTests
    {
        private void Test()
        {
            var mock1 = new Moq.Mock<FooSealed>();
            var mock2 = new Mock<FooSealed>();
            var mock3 = new Mock<NoSealedClassMocks.FooSealed>();
            var mock4 = new Moq.Mock<NoSealedClassMocks.FooSealed>();
        }

        private void Test2()
        {
            new Mock<Action<int>>();
            new Mock<EventHandler>();
        }
    }
}
