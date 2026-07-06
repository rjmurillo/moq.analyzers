using Microsoft.CodeAnalysis.Testing;
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

            // It.IsInRange for numeric matching (supported in both versions, using qualified name to avoid ambiguity)
            ["""new Mock<ISampleInterface>().Setup(x => x.Calculate(It.IsInRange(1, 10, Moq.Range.Inclusive), It.IsAny<int>()));"""],
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

            // Default interface members follow Moq 4.18.4 runtime behavior.
            ["""new Mock<IDefaultInterfaceMembers>().Setup(x => x.AbstractMethod());"""],
            ["""new Mock<IDefaultInterfaceMembers>().Setup(x => x.DefaultMethod());"""],
            ["""{|Moq1200:new Mock<IDefaultInterfaceMembers>().Setup(x => x.SealedDefaultMethod())|};"""],
            ["""{|Moq1200:new Mock<IStaticInterfaceMembers>().Setup(x => IStaticInterfaceMembers.StaticMethod())|};"""],

            // It.Ref<T>.IsAny for ref parameters (requires Moq 4.8+, should work in newer versions)
            ["""new Mock<ISampleInterface>().Setup(x => x.ProcessRef(ref It.Ref<int>.IsAny)).Returns(true);"""],

            // Static members referenced from a Setup lambda are not overridable and are flagged.
            // These bind the generic Setup<TResult>(Expression<Func<T, TResult>>) overload.
            ["""{|Moq1200:new Mock<SampleClassWithStaticMembers>().Setup(x => SampleClassWithStaticMembers.StaticField)|};"""],
            ["""{|Moq1200:new Mock<SampleClassWithStaticMembers>().Setup(x => SampleClassWithStaticMembers.ConstField)|};"""],
            ["""{|Moq1200:new Mock<SampleClassWithStaticMembers>().Setup(x => SampleClassWithStaticMembers.ReadonlyField)|};"""],
            ["""{|Moq1200:new Mock<SampleClassWithStaticMembers>().Setup(x => SampleClassWithStaticMembers.StaticProperty)|};"""],
            ["""{|Moq1200:new Mock<SampleClassWithStaticMembers>().Setup(x => SampleClassWithStaticMembers.GetStaticValue())|};"""],

            // Static void methods are also flagged as non-overridable.
            ["""{|Moq1200:new Mock<SampleClassWithStaticMembers>().Setup(x => SampleClassWithStaticMembers.StaticMethod())|};"""],

            // Extension methods cannot be overridden and are flagged.
            ["""{|Moq1200:new Mock<ISampleInterface>().Setup(x => x.CalculateTwice())|};"""],

            // Nested-generic mocked type with an overridable interface member - not flagged.
            ["""new Mock<IGenericInterface<IGenericInterface<int>>>().Setup(x => x.GetValue());"""],
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
                                    bool ProcessRef(ref int value);
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
                                public interface IGenericInterface<T> { T GetValue(); }
                                public static class SampleExtensions { public static int CalculateTwice(this ISampleInterface s) => s.Calculate(1, 2) * 2; }
                                public class SampleClassWithVirtualIndexer { public virtual int this[int i] { get => 0; set { } } }
                                public class SampleClassWithNonVirtualIndexer { public int this[int i] { get => 0; set { } } }
                                public interface IExplicitInterface { void ExplicitMethod(); }
                                public interface IDefaultInterfaceMembers
                                {
                                    void AbstractMethod();
                                    void DefaultMethod() { }
                                    sealed void SealedDefaultMethod() { }
                                }
                                public interface IStaticInterfaceMembers { static void StaticMethod() { } }
                                public class SampleClassWithStaticMembers { public static int StaticField; public const int ConstField = 42; public static readonly int ReadonlyField = 42; public static void StaticMethod() { } public static int StaticProperty { get; set; } public static int GetStaticValue() => 0; }

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

    /// <summary>
    /// An incomplete Setup lambda (mid-edit code) must not crash the analyzer and
    /// produces no diagnostic (current behavior).
    /// </summary>
    /// <param name="referenceAssemblyGroup">The Moq version reference assembly group.</param>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Theory]
    [InlineData("Net80WithOldMoq")]
    [InlineData("Net80WithNewMoq")]
    public async Task ShouldNotReportOnIncompleteSetupLambda(string referenceAssemblyGroup)
    {
        const string source = """
            public interface ISampleInterface
            {
                int Calculate(int a, int b);
            }

            internal class UnitTest
            {
                private void Test()
                {
                    new Mock<ISampleInterface>().Setup(x => x.
                }
            }
            """;

        // CompilerDiagnostics.None suppresses CS1001/CS1026/CS1002 from the incomplete lambda.
        await Verifier.VerifyAnalyzerAsync(source, referenceAssemblyGroup, CompilerDiagnostics.None);
    }
}
