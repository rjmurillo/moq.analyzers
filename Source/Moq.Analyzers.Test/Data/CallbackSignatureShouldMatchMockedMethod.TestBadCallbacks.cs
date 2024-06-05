using System;
using System.Collections.Generic;
using Moq;

namespace CallbackSignatureShouldMatchMockedMethod.TestBadCallbacks;

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
}
