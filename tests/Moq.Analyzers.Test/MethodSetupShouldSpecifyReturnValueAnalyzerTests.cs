using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.MethodSetupShouldSpecifyReturnValueAnalyzer>;

namespace Moq.Analyzers.Test;

public class MethodSetupShouldSpecifyReturnValueAnalyzerTests(ITestOutputHelper output)
{
    public static IEnumerable<object[]> TestData()
    {
        IEnumerable<object[]> edge =
        [

            // Null lambda (should not crash, should not report diagnostic)
            ["""new Mock<IFoo>().Setup(null);"""],

            // Constant expression (should not report diagnostic)
            ["""new Mock<IFoo>().Setup(x => 42);"""],

            // Setup with method group (should report Moq1203 diagnostic)
            ["""{|Moq1203:new Mock<IFoo>().Setup(x => x.ToString())|};"""],

            // Setup with nested lambda (should report Moq1203 diagnostic)
            ["""{|Moq1203:new Mock<IFoo>().Setup(x => new Func<int>(() => 1)())|};"""],
        ];

        // Test cases where a method setup should specify return value but doesn't
        IEnumerable<object[]> both =
        [

            // Method with return type should specify return value
            ["""{|Moq1203:new Mock<IFoo>().Setup(x => x.DoSomething("test"))|};"""],
            ["""{|Moq1203:new Mock<IFoo>().Setup(x => x.GetValue())|};"""],
            ["""{|Moq1203:new Mock<IFoo>().Setup(x => x.Calculate(1, 2))|};"""],

            // Valid cases - method with return type that does specify return value
            ["""new Mock<IFoo>().Setup(x => x.DoSomething("test")).Returns(true);"""],
            ["""new Mock<IFoo>().Setup(x => x.GetValue()).Returns(42);"""],
            ["""new Mock<IFoo>().Setup(x => x.Calculate(1, 2)).Returns(10);"""],
            ["""new Mock<IFoo>().Setup(x => x.DoSomething("test")).Throws<InvalidOperationException>();"""],
            ["""new Mock<IFoo>().Setup(x => x.DoSomething("test")).Throws(new ArgumentException());"""],

            // Void methods should not trigger the analyzer
            ["""new Mock<IFoo>().Setup(x => x.DoVoidMethod());"""],
            ["""new Mock<IFoo>().Setup(x => x.ProcessData("test"));"""],

            // Property setups should not trigger this analyzer
            ["""new Mock<IFoo>().Setup(x => x.Name);"""],
            ["""new Mock<IFoo>().SetupGet(x => x.Name);"""],

            // Async method without return specification should flag
            ["""{|Moq1203:new Mock<IFoo>().Setup(x => x.BarAsync())|};"""],
        ];

        return both.Union(edge).WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    // Regression test data for https://github.com/rjmurillo/moq.analyzers/issues/849
    // Tests valid return value specifications that should NOT trigger Moq1203.
    public static IEnumerable<object[]> Issue849_FalsePositiveTestData()
    {
        IEnumerable<object[]> data =
        [

            // ReturnsAsync recognized as a return value specification
            ["""new Mock<IFoo>().Setup(x => x.BarAsync()).ReturnsAsync(1);"""],

            // ThrowsAsync recognized as a return value specification
            ["""new Mock<IFoo>().Setup(x => x.BarAsync()).ThrowsAsync(new InvalidOperationException());"""],

            // Callback before Returns should not cause false positive
            ["""new Mock<IFoo>().Setup(x => x.BarAsync()).Callback(() => { }).Returns(Task.FromResult(1));"""],

            // Callback before ReturnsAsync should not cause false positive
            ["""new Mock<IFoo>().Setup(x => x.BarAsync()).Callback(() => { }).ReturnsAsync(1);"""],

            // Callback before ThrowsAsync should not cause false positive
            ["""new Mock<IFoo>().Setup(x => x.BarAsync()).Callback(() => { }).ThrowsAsync(new InvalidOperationException());"""],

            // Callback before Returns on sync method should not cause false positive
            ["""new Mock<IFoo>().Setup(x => x.DoSomething("test")).Callback(() => { }).Returns(true);"""],

            // Callback before Throws should not cause false positive
            ["""new Mock<IFoo>().Setup(x => x.DoSomething("test")).Callback(() => { }).Throws<InvalidOperationException>();"""],
        ];

        return data.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    // Callback-only tests require newer Moq (4.18.4+) API shapes.
    public static IEnumerable<object[]> CallbackOnlyNewMoqTestData()
    {
        IEnumerable<object[]> data =
        [

            // Callback alone should still require return value specification
            ["""{|Moq1203:new Mock<IFoo>().Setup(x => x.GetValue())|}.Callback(() => { });"""],
        ];

        return data.WithNamespaces().WithNewMoqReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldAnalyzeMethodSetupReturnValue(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await VerifyMock(referenceAssemblyGroup, @namespace, mock);
    }

    [Theory]
    [MemberData(nameof(Issue849_FalsePositiveTestData))]
    public async Task ShouldNotFlagSetupWithReturnsAsyncOrCallbackChaining(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await VerifyMock(referenceAssemblyGroup, @namespace, mock);
    }

    [Theory]
    [MemberData(nameof(CallbackOnlyNewMoqTestData))]
    public async Task ShouldFlagCallbackOnlySetupOnNewMoq(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await VerifyMock(referenceAssemblyGroup, @namespace, mock);
    }

    [Theory]
    [MemberData(nameof(DoppelgangerTestHelper.GetAllCustomMockData), MemberType = typeof(DoppelgangerTestHelper))]
    public async Task ShouldPassIfCustomMockClassIsUsed(string mockCode)
    {
        await Verifier.VerifyAnalyzerAsync(
            DoppelgangerTestHelper.CreateTestCode(mockCode),
            ReferenceAssemblyCatalog.Net80WithNewMoq);
    }

    private async Task VerifyMock(string referenceAssemblyGroup, string @namespace, string mock)
    {
        string source = $$"""
            {{@namespace}}

            public interface IFoo
            {
                bool DoSomething(string value);
                int GetValue();
                int Calculate(int a, int b);
                Task<int> BarAsync();
                void DoVoidMethod();
                void ProcessData(string data);
                string Name { get; set; }
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
            referenceAssemblyGroup).ConfigureAwait(false);
    }
}
