using System;
using System.Collections.Generic;
using Moq;

namespace CallbackSignatureShouldMatchMockedMethod.TestGoodSetupAndReturnsAndCallback;

internal interface IFoo
{
    int Do(string s);

    int Do(int i, string s, DateTime dt);

    int Do(List<string> l);
}

internal class MyUnitTests
{
    private void TestGoodSetupAndReturnsAndCallback()
    {
        var mock = new Mock<IFoo>();
        mock.Setup(x => x.Do(It.IsAny<string>())).Returns(0).Callback((string s) => { });
        mock.Setup(x => x.Do(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>())).Returns(0).Callback((int i, string s, DateTime dt) => { });
        mock.Setup(x => x.Do(It.IsAny<List<string>>())).Returns(0).Callback((List<string> l) => { });
    }
}
