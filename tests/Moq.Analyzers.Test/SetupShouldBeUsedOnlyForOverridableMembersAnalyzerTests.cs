using Moq.Analyzers.Test.Helpers;
using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.SetupShouldBeUsedOnlyForOverridableMembersAnalyzer>;

namespace Moq.Analyzers.Test;

public partial class SetupShouldBeUsedOnlyForOverridableMembersAnalyzerTests(ITestOutputHelper output)
{
    public static IEnumerable<object[]> TestData()
    {
        IEnumerable<object[]> both = new object[][]
        {
            // Valid in both versions, but flagged as error for non-virtual/invalid targets
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
            ["""{|Moq1200:new Mock<SampleClass>().Setup(x => x.Field)|};"""],
            ["""{|Moq1200:new Mock<SampleClassWithNonVirtualIndexer>().Setup(x => x[0])|};"""],

            // Additional argument matcher patterns - It.Is with predicates (supported in both versions)
            ["""new Mock<ISampleInterface>().Setup(x => x.Calculate(It.Is<int>(i => i > 0), It.IsAny<int>()));"""],
            ["""new Mock<ISampleInterface>().Setup(x => x.Calculate(It.Is<int>(i => i % 2 == 0), It.Is<int>(j => j < 100)));"""],

            // It.IsRegex for string matching (supported in both versions)
            ["""new Mock<ISampleInterface>().Setup(x => x.GetString(It.IsRegex(@"\d+")));"""],
            ["""new Mock<ISampleInterface>().Setup(x => x.GetString(It.IsRegex("[a-zA-Z]+", System.Text.RegularExpressions.RegexOptions.IgnoreCase)));"""],
        }.WithNamespaces().WithOldMoqReferenceAssemblyGroups();

        IEnumerable<object[]> @new = new object[][]
        {
            // Only supported in Moq 4.18.4+
            // SetupAdd/SetupRemove for virtual event (should NOT be flagged)
            ["""new Mock<SampleClass>().SetupAdd(x => x.TestEvent += It.IsAny<EventHandler>());"""],
            ["""new Mock<SampleClass>().SetupRemove(x => x.TestEvent -= It.IsAny<EventHandler>());"""],
            ["""new Mock<SampleClassWithVirtualEvent>().SetupAdd(x => x.TestEvent += It.IsAny<EventHandler>());"""],
            ["""new Mock<SampleClassWithVirtualEvent>().SetupRemove(x => x.TestEvent -= It.IsAny<EventHandler>());"""],

            // Indexer on interface and virtual indexer (should NOT be flagged)
            ["""new Mock<IIndexerInterface>().Setup(x => x[0]);"""],
            ["""new Mock<SampleClassWithVirtualIndexer>().Setup(x => x[0]);"""],

            // Explicit interface implementation (should NOT be flagged in new)
            ["""new Mock<IExplicitInterface>().Setup(x => ((IExplicitInterface)x).ExplicitMethod());"""],
        }.WithNamespaces().WithNewMoqReferenceAssemblyGroups();

        return both.Concat(@new);
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
                                    string GetString(string input);
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

                                public interface IIndexerInterface { int this[int i] { get; set; } }
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
