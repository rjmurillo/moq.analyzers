#pragma warning disable SA1402 // File may only contain a single class
#pragma warning disable SA1502 // Element must not be on a single line
#pragma warning disable SA1602 // Undocumented enum values
#pragma warning disable SA1649 // File name must match first type name
#pragma warning disable RCS1021 // Simplify lambda expression
#pragma warning disable RCS1163 // Unused parameter
#pragma warning disable RCS1213 // Remove unused member declaration
#pragma warning disable IDE0051 // Unused private member
#pragma warning disable IDE0059 // Unnecessary value assignment
#pragma warning disable IDE0060 // Unused parameter
namespace CallbackSignatureShouldMatchMockedMethod
{
    using System;
    using System.Collections.Generic;
    using Moq;

    internal interface IFoo
    {
        int Do(string s);

        int Do(int i, string s, DateTime dt);

        int Do(List<string> l);
    }

    internal class MyUnitTests
    {
        private void TestBadCallbacks()
        {
            var mock = new Mock<IFoo>();
            mock.Setup(x => x.Do(It.IsAny<string>())).Callback((int i) => { });
            mock.Setup(x => x.Do(It.IsAny<string>())).Callback((string s1, string s2) => { });
            mock.Setup(x => x.Do(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>())).Callback((string s1, int i1) => { });
            mock.Setup(x => x.Do(It.IsAny<List<string>>())).Callback((int i) => { });
        }

        private void TestGoodSetupAndParameterlessCallback()
        {
            var mock = new Mock<IFoo>();
            mock.Setup(x => x.Do(It.IsAny<string>())).Callback(() => { });
            mock.Setup(x => x.Do(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>())).Callback(() => { });
            mock.Setup(x => x.Do(It.IsAny<List<string>>())).Callback(() => { });
        }

        private void TestGoodSetupAndCallback()
        {
            var mock = new Mock<IFoo>();
            mock.Setup(x => x.Do(It.IsAny<string>())).Callback((string s) => { });
            mock.Setup(x => x.Do(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>())).Callback((int i, string s, DateTime dt) => { });
            mock.Setup(x => x.Do(It.IsAny<List<string>>())).Callback((List<string> l) => { });
        }

        private void TestGoodSetupAndReturnsAndCallback()
        {
            var mock = new Mock<IFoo>();
            mock.Setup(x => x.Do(It.IsAny<string>())).Returns(0).Callback((string s) => { });
            mock.Setup(x => x.Do(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>())).Returns(0).Callback((int i, string s, DateTime dt) => { });
            mock.Setup(x => x.Do(It.IsAny<List<string>>())).Returns(0).Callback((List<string> l) => { });
        }

        private void MyGoodSetupAndReturns()
        {
            var mock = new Mock<IFoo>();
            mock.Setup(x => x.Do(It.IsAny<string>())).Returns((string s) => { return 0; });
            mock.Setup(x => x.Do(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>())).Returns((int i, string s, DateTime dt) => { return 0; });
            mock.Setup(x => x.Do(It.IsAny<List<string>>())).Returns((List<string> l) => { return 0; });
        }
    }
}