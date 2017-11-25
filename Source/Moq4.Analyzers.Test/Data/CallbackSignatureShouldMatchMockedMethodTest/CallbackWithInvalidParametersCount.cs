using Moq;

namespace CallbackWithInvalidParametersCount
{
    interface IMyService
    {
        int Do(string s);
        int Do(string s1, string s2, string s3);
    }

    class MyUnitTests
    {
        void MyTest()
        {
            var mock = new Mock<IMyService>();
            mock.Setup(x => x.Do(It.IsAny<string>())).Callback((string s1, string s2) => { });
        }
    }
}