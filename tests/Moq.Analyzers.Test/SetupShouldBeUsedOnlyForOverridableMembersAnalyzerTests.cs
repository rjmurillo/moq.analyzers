using Moq.Analyzers.Test.Helpers;
using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.SetupShouldBeUsedOnlyForOverridableMembersAnalyzer>;

namespace Moq.Analyzers.Test;

public class SetupShouldBeUsedOnlyForOverridableMembersAnalyzerTests(ITestOutputHelper output)
{
    public static IEnumerable<object[]> TestData()
    {
        return new object[][]
        {
            ["""{|Moq1200:new Mock<BaseSampleClass>().Setup(x => x.Calculate())|};"""],
            ["""{|Moq1200:new Mock<SampleClass>().Setup(x => x.Property)|};"""],
            ["""{|Moq1200:new Mock<SampleClass>().Setup(x => x.Calculate(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))|};"""],
            ["""new Mock<BaseSampleClass>().Setup(x => x.Calculate(It.IsAny<int>(), It.IsAny<int>()));"""],
            ["""new Mock<ISampleInterface>().Setup(x => x.Calculate(It.IsAny<int>(), It.IsAny<int>()));"""],
            ["""new Mock<ISampleInterface>().Setup(x => x.TestProperty);"""],
            ["""new Mock<SampleClass>().Setup(x => x.Calculate(It.IsAny<int>(), It.IsAny<int>()));"""],
            ["""new Mock<SampleClass>().Setup(x => x.DoSth());"""],
            ["""new Mock<IAsyncMethods>().Setup(x => x.DoSomethingAsync());"""],
            ["""new Mock<IAsyncMethods>().Setup(x => x.GetBooleanAsync().Result).Returns(true);"""],
            ["""new Mock<IValueTaskMethods>().Setup(x => x.DoSomethingValueTask());"""],
            ["""new Mock<IValueTaskMethods>().Setup(x => x.GetNumberAsync()).Returns(ValueTask.FromResult(42));"""],
            ["""new Mock<SampleClass>().Setup(x => x.Field);"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldAnalyzeSetupForOverridableMembers(string referenceAssemblyGroup, string @namespace, string mock)
    {
        string source = $$"""
                                {{@namespace}}

                                public interface ISampleInterface
                                {
                                    int Calculate(int a, int b);
                                    int TestProperty { get; set; }
                                }

                                public interface IAsyncMethods
                                {
                                    Task DoSomethingAsync();
                                    Task<bool> GetBooleanAsync();
                                }

                                public interface IValueTaskMethods
                                {
                                    ValueTask DoSomethingValueTask();
                                    ValueTask<int> GetNumberAsync();
                                }

                                public abstract class BaseSampleClass
                                {
                                    public int Calculate() => 0;
                                    public abstract int Calculate(int a, int b);
                                    public abstract int Calculate(int a, int b, int c);
                                }

                                public class SampleClass : BaseSampleClass
                                {
                                    public override int Calculate(int a, int b) => 0;
                                    public sealed override int Calculate(int a, int b, int c) => 0;
                                    public virtual int DoSth() => 0;
                                    public int Property { get; set; }
                                    public int Field;
                                }

                                internal class UnitTest
                                {
                                    private void Test()
                                    {
                                        {{mock}}
                                    }
                                }
                                """;

        output.WriteLine(source);

        await Verifier.VerifyAnalyzerAsync(
                source,
                referenceAssemblyGroup);
    }

    #region Doppelganger Tests

    [Theory]
    [MemberData(nameof(DoppelgangerTestHelper.GetAllCustomMockData), MemberType = typeof(DoppelgangerTestHelper))]
    public async Task ShouldPassIfCustomMockClassIsUsed(string mockCode)
    {
        await Verifier.VerifyAnalyzerAsync(
            DoppelgangerTestHelper.CreateTestCode(mockCode),
            ReferenceAssemblyCatalog.Net80WithNewMoq);
    }

    #endregion
}
