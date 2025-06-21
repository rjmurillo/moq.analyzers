using Moq.Analyzers.Test.Helpers;
using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.VerifyShouldBeUsedOnlyForOverridableMembersAnalyzer>;

namespace Moq.Analyzers.Test;

public partial class VerifyShouldBeUsedOnlyForOverridableMembersAnalyzerTests(ITestOutputHelper output)
{
    public static IEnumerable<object[]> TestData()
    {
        IEnumerable<object[]> both = new object[][]
        {
            // Valid in both versions, but flagged as error for non-virtual/invalid targets
            ["""{|Moq1210:new Mock<BaseSampleClass>().Verify(x => x.Calculate())|};"""],
            ["""{|Moq1210:new Mock<SampleClass>().Verify(x => x.Property)|};"""],
            ["""{|Moq1210:new Mock<SampleClass>().Verify(x => x.Calculate(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))|};"""],
            ["""new Mock<BaseSampleClass>().Verify(x => x.Calculate(It.IsAny<int>(), It.IsAny<int>()));"""],
            ["""new Mock<ISampleInterface>().Verify(x => x.Calculate(It.IsAny<int>(), It.IsAny<int>()));"""],

            // VerifyGet tests
            ["""{|Moq1210:new Mock<SampleClass>().VerifyGet(x => x.Property)|};"""],
            ["""new Mock<ISampleInterface>().VerifyGet(x => x.TestProperty);"""],

            // VerifyNoOtherCalls should not trigger any diagnostics
            ["""new Mock<SampleClass>().VerifyNoOtherCalls();"""],
            ["""new Mock<ISampleInterface>().VerifyNoOtherCalls();"""],

            // Valid verifications should not trigger diagnostics
            ["""new Mock<SampleClass>().Verify(x => x.DoSth());"""],
            ["""new Mock<SampleClass>().Verify(x => x.Calculate(It.IsAny<int>(), It.IsAny<int>()));"""],
            ["""new Mock<ISampleInterface>().Verify(x => x.Calculate(It.IsAny<int>(), It.IsAny<int>()));"""],
            ["""new Mock<ISampleInterface>().Verify(x => x.TestProperty);"""],
            ["""{|Moq1210:new Mock<SampleClass>().Verify(x => x.Field)|};"""],
            ["""{|Moq1210:new Mock<SampleClassWithNonVirtualIndexer>().Verify(x => x[0])|};"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();

        IEnumerable<object[]> newMoqOnly = new object[][]
        {
            // VerifySet tests - only available in new Moq versions
            ["""{|Moq1210:new Mock<SampleClass>().VerifySet(x => x.Property = It.IsAny<int>())|};"""],
            ["""new Mock<ISampleInterface>().VerifySet(x => x.TestProperty = It.IsAny<string>());"""],
        }.WithNamespaces().WithNewMoqReferenceAssemblyGroups();

        return both.Concat(newMoqOnly);
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldAnalyzeVerifyForOverridableMembers(string referenceAssemblyGroup, string @namespace, string mock)
    {
        string source = $$"""
                                {{@namespace}}

                                public interface ISampleInterface
                                {
                                    int Calculate(int a, int b);
                                    string TestProperty { get; set; }
                                }

                                public class SampleClassWithVirtualIndexer { public virtual int this[int i] { get => 0; set { } } }
                                public class SampleClassWithNonVirtualIndexer { public int this[int i] { get => 0; set { } } }
                                public interface IExplicitInterface { void ExplicitMethod(); }
                                public class SampleClassWithStaticMembers { public static int StaticField; public const int ConstField = 42; public static readonly int ReadonlyField = 42; public static void StaticMethod() { } }

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
                                    public event EventHandler? TestEvent;
                                }

                                public class SampleClassWithVirtualEvent
                                {
                                    public virtual event EventHandler? TestEvent;
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

    [Theory]
    [MemberData(nameof(DoppelgangerTestHelper.GetAllCustomMockData), MemberType = typeof(DoppelgangerTestHelper))]
    public async Task ShouldPassIfCustomMockClassIsUsed(string mockCode)
    {
        await Verifier.VerifyAnalyzerAsync(
            DoppelgangerTestHelper.CreateTestCode(mockCode),
            ReferenceAssemblyCatalog.Net80WithNewMoq);
    }
}
