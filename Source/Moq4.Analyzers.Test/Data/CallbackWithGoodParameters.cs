using Moq;
using System;
using System.Collections.Generic;

namespace CallbackWithGoodParameters
{
    interface IMyService
    {
        int DoString(string s);

        int DoMultipleParameters(int i, string s, DateTime dt);

        int DoList(List<string> l);
    }

    class MyUnitTests
    {
        void MyTest()
        {
            var mock = new Mock<IMyService>();
            mock.Setup(x => x.DoString(It.IsAny<string>())).Callback((string s) => { });
            mock.Setup(x => x.DoMultipleParameters(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>())).Callback((int i, string s, DateTime dt) => { });
            mock.Setup(x => x.DoList(It.IsAny<List<string>>())).Callback((List<string> l) => { });
        }
    }
}