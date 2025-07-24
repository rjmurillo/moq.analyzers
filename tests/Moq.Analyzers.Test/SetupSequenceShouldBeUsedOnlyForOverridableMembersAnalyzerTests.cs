using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.SetupSequenceShouldBeUsedOnlyForOverridableMembersAnalyzer>;

namespace Moq.Analyzers.Test;

public class SetupSequenceShouldBeUsedOnlyForOverridableMembersAnalyzerTests
{
    public static IEnumerable<object[]> TestData()
    {
        IEnumerable<object[]> both = new object[][]
        {
            // Valid in both versions - should NOT trigger diagnostic
            ["""new Mock<ISampleInterface>().SetupSequence(x => x.Calculate(It.IsAny<int>(), It.IsAny<int>()));"""],
            ["""new Mock<ISampleInterface>().SetupSequence(x => x.TestProperty);"""],
            ["""new Mock<SampleClass>().SetupSequence(x => x.Calculate(It.IsAny<int>(), It.IsAny<int>()));"""],
            ["""new Mock<SampleClass>().SetupSequence(x => x.DoSth());"""],
            ["""new Mock<BaseSampleClass>().SetupSequence(x => x.Calculate(It.IsAny<int>(), It.IsAny<int>()));"""],

            // Valid async method setups
            ["""new Mock<IAsyncMethods>().SetupSequence(x => x.DoSomethingAsync());"""],
            ["""new Mock<IAsyncMethods>().SetupSequence(x => x.GetBooleanAsync().Result).Returns(true);"""],
            ["""new Mock<IValueTaskMethods>().SetupSequence(x => x.DoSomethingValueTask());"""],
            ["""new Mock<IValueTaskMethods>().SetupSequence(x => x.GetNumberAsync()).Returns(ValueTask.FromResult(42));"""],

            // Invalid - should trigger Moq1800 diagnostic
            ["""new Mock<BaseSampleClass>().SetupSequence(x => {|Moq1800:x.Calculate()|});"""],
            ["""new Mock<SampleClass>().SetupSequence(x => {|Moq1800:x.Property|});"""],
            ["""new Mock<SampleClass>().SetupSequence(x => {|Moq1800:x.Calculate(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())|});"""],
            ["""new Mock<SampleClass>().SetupSequence(x => {|Moq1800:x.Field|});"""],
            ["""new Mock<SampleClassWithNonVirtualIndexer>().SetupSequence(x => {|Moq1800:x[0]|});"""],

            // Additional argument matcher patterns - It.Is with predicates (supported in both versions)
            ["""new Mock<ISampleInterface>().SetupSequence(x => x.Calculate(It.Is<int>(i => i > 0), It.IsAny<int>()));"""],
            ["""new Mock<ISampleInterface>().SetupSequence(x => x.Calculate(It.Is<int>(i => i % 2 == 0), It.Is<int>(j => j < 100)));"""],

            // It.IsRegex for string matching (supported in both versions)
            ["""new Mock<ISampleInterface>().SetupSequence(x => x.GetString(It.IsRegex(@"\d+")));"""],
            ["""new Mock<ISampleInterface>().SetupSequence(x => x.GetString(It.IsRegex("[a-zA-Z]+", System.Text.RegularExpressions.RegexOptions.IgnoreCase)));"""],

            // It.IsInRange for numeric matching (supported in both versions, using qualified name to avoid ambiguity)
            ["""new Mock<ISampleInterface>().SetupSequence(x => x.Calculate(It.IsInRange(1, 10, Moq.Range.Inclusive), It.IsAny<int>()));"""],
        }.WithNamespaces().WithOldMoqReferenceAssemblyGroups();

        IEnumerable<object[]> @new = new object[][]
        {
            // SetupSequence with virtual indexer (should NOT be flagged) - Moq 4.18.4+ only
            ["""new Mock<SampleClassWithVirtualIndexer>().SetupSequence(x => x[0]);"""],
        }.WithNamespaces().WithNewMoqReferenceAssemblyGroups();

        return both.Concat(@new);
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldAnalyzeSetupSequenceUsage(string referenceAssemblyGroup, string @namespace, string testCase)
    {
        string sourceCode = $$"""
            {{@namespace}}

            public interface ISampleInterface
            {
                int Calculate(int x, int y);
                string GetString(string input);
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

            public class BaseSampleClass
            {
                public virtual int Calculate(int x, int y) => x + y;
                public int Calculate() => 42; // Non-virtual overload
            }

            public class SampleClass : BaseSampleClass
            {
                public int Property { get; set; } // Non-virtual property
                public int Field; // Field
                public virtual void DoSth() { }
                public override int Calculate(int x, int y) => x * y;
                public int Calculate(int x, int y, int z) => x + y + z; // Non-virtual overload
            }

            public class SampleClassWithNonVirtualIndexer
            {
                public int this[int index] => index; // Non-virtual indexer
            }

            public class SampleClassWithVirtualIndexer
            {
                public virtual int this[int index] => index; // Virtual indexer
            }

            public class TestClass
            {
                public void TestMethod()
                {
                    {{testCase}}
                }
            }
            """;

        await Verifier.VerifyAnalyzerAsync(sourceCode, referenceAssemblyGroup);
    }
}
