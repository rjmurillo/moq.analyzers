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

            // Setup with property access (should not trigger analyzer)
            ["""new Mock<IFoo>().Setup(x => x.Name);"""],
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
        ];

        return both.Union(edge).WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldAnalyzeMethodSetupReturnValue(string referenceAssemblyGroup, string @namespace, string mock)
    {
        string source = $$"""
            {{@namespace}}

            public interface IFoo
            {
                bool DoSomething(string value);
                int GetValue();
                int Calculate(int a, int b);
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
