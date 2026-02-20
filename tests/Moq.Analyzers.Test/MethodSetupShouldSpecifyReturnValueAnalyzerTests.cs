using Microsoft.CodeAnalysis.Testing;
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

            // Throws on async method should also be recognized
            ["""new Mock<IFoo>().Setup(x => x.BarAsync()).Throws<InvalidOperationException>();"""],

            // Callback before Returns should not cause false positive
            ["""new Mock<IFoo>().Setup(x => x.BarAsync()).Callback(() => { }).Returns(Task.FromResult(1));"""],

            // Callback before ReturnsAsync should not cause false positive
            ["""new Mock<IFoo>().Setup(x => x.BarAsync()).Callback(() => { }).ReturnsAsync(1);"""],

            // Callback before ThrowsAsync should not cause false positive
            ["""new Mock<IFoo>().Setup(x => x.BarAsync()).Callback(() => { }).ThrowsAsync(new InvalidOperationException());"""],

            // Callback before Returns on sync method should not cause false positive
            ["""new Mock<IFoo>().Setup(x => x.DoSomething("test")).Callback(() => { }).Returns(true);"""],

            // Callback before Throws (generic form) should not cause false positive
            ["""new Mock<IFoo>().Setup(x => x.DoSomething("test")).Callback(() => { }).Throws<InvalidOperationException>();"""],

            // Callback before Throws (instance form) should not cause false positive
            ["""new Mock<IFoo>().Setup(x => x.DoSomething("test")).Callback(() => { }).Throws(new InvalidOperationException());"""],
        ];

        return data.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    // Callback-only tests require newer Moq (4.18.4+) because older versions lack
    // the generic ICallback<TMock, TResult> interface that enables Callback on
    // non-void setups.
    public static IEnumerable<object[]> CallbackOnlyNewMoqTestData()
    {
        IEnumerable<object[]> data =
        [

            // Callback alone should still require return value specification
            ["""{|Moq1203:new Mock<IFoo>().Setup(x => x.GetValue())|}.Callback(() => { });"""],

            // Variable mock + Callback-only, no return value (diagnostic expected)
            ["""
            var mock = new Mock<IFoo>();
            {|Moq1203:mock.Setup(x => x.GetValue())|}.Callback(() => { });
            """],
        ];

        return data.WithNamespaces().WithNewMoqReferenceAssemblyGroups();
    }

    // Deliberately uses wrong argument types so Roslyn reports OverloadResolutionFailure.
    // The analyzer should still recognize the return value method via candidate symbols.
    public static IEnumerable<object[]> OverloadResolutionFailureTestData()
    {
        IEnumerable<object[]> data =
        [

            // Mismatched argument type forces Roslyn to report candidates instead of a resolved symbol
            ["""new Mock<IFoo>().Setup(x => x.GetValue()).Returns("wrong type");"""],
        ];

        return data.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    // Deliberately uses wrong argument types on a non-return-value method.
    // No return value specification follows, so Moq1203 should still fire.
    public static IEnumerable<object[]> OverloadResolutionFailureWithDiagnosticTestData()
    {
        IEnumerable<object[]> data =
        [

            // Mismatched argument type on Callback, with no return value specification
            ["""{|Moq1203:new Mock<IFoo>().Setup(x => x.GetValue())|}.Callback("wrong type");"""],
        ];

        return data.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    // Regression test data for https://github.com/rjmurillo/moq.analyzers/issues/887
    // Tests parenthesized Setup expressions that should correctly detect return value specs.
    public static IEnumerable<object[]> Issue887_ParenthesizedSetupTestData()
    {
        IEnumerable<object[]> data =
        [

            // Parenthesized Setup with Returns should not trigger diagnostic
            ["""(new Mock<IFoo>().Setup(x => x.GetValue())).Returns(42);"""],

            // Nested parentheses with Returns should not trigger diagnostic
            ["""((new Mock<IFoo>().Setup(x => x.GetValue()))).Returns(42);"""],

            // Parenthesized Setup with Throws should not trigger diagnostic
            ["""(new Mock<IFoo>().Setup(x => x.DoSomething("test"))).Throws<InvalidOperationException>();"""],

            // Parenthesized Setup with Callback chaining should not trigger diagnostic
            ["""(new Mock<IFoo>().Setup(x => x.GetValue())).Callback(() => { }).Returns(42);"""],

            // Parentheses around intermediate chain node should not trigger diagnostic
            ["""(new Mock<IFoo>().Setup(x => x.GetValue()).Callback(() => { })).Returns(42);"""],

            // Parenthesized Setup with ReturnsAsync should not trigger diagnostic
            ["""(new Mock<IFoo>().Setup(x => x.BarAsync())).ReturnsAsync(1);"""],
        ];

        return data.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    // Parenthesized Setup without return value spec should still trigger Moq1203.
    // Uses discard assignment to keep expressions valid C# while preserving parentheses.
    public static IEnumerable<object[]> Issue887_ParenthesizedSetupWithDiagnosticTestData()
    {
        IEnumerable<object[]> data =
        [

            // Parenthesized Setup WITHOUT return value spec should still trigger diagnostic
            ["""_ = ({|Moq1203:new Mock<IFoo>().Setup(x => x.GetValue())|});"""],

            // Nested parentheses WITHOUT return value spec should still trigger diagnostic
            ["""_ = (({|Moq1203:new Mock<IFoo>().Setup(x => x.GetValue())|}));"""],

            // Parenthesized async Setup WITHOUT return value spec should still trigger diagnostic
            ["""_ = ({|Moq1203:new Mock<IFoo>().Setup(x => x.BarAsync())|});"""],
        ];

        return data.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    // These semantic variations (variable mocks, MockBehavior parameters, variable
    // arguments) produce different Roslyn operation trees than inline literal patterns.
    // Tests ensure the analyzer's return-value detection handles them all.
    public static IEnumerable<object[]> SemanticVariationSetupTestData()
    {
        IEnumerable<object[]> data =
        [

            // Variable mock + ReturnsAsync on async method
            ["""
            var mock = new Mock<IFoo>();
            mock.Setup(x => x.BarAsync()).ReturnsAsync(1);
            """],

            // Variable mock + Returns on sync method
            ["""
            var mock = new Mock<IFoo>();
            mock.Setup(x => x.GetValue()).Returns(42);
            """],

            // MockBehavior.Loose inline + ReturnsAsync
            ["""new Mock<IFoo>(MockBehavior.Loose).Setup(x => x.BarAsync()).ReturnsAsync(1);"""],

            // MockBehavior.Strict inline + Returns
            ["""new Mock<IFoo>(MockBehavior.Strict).Setup(x => x.GetValue()).Returns(42);"""],

            // Variable argument with ReturnsAsync
            ["""
            var val = 1;
            new Mock<IFoo>().Setup(x => x.BarAsync()).ReturnsAsync(val);
            """],

            // Variable mock + Callback chain + ReturnsAsync
            ["""
            var mock = new Mock<IFoo>();
            mock.Setup(x => x.BarAsync()).Callback(() => { }).ReturnsAsync(1);
            """],

            // Variable mock + Callback chain + Returns
            ["""
            var mock = new Mock<IFoo>();
            mock.Setup(x => x.GetValue()).Callback(() => { }).Returns(42);
            """],

            // Variable mock + Throws on sync method
            ["""
            var mock = new Mock<IFoo>();
            mock.Setup(x => x.GetValue()).Throws<InvalidOperationException>();
            """],

            // Variable mock + ThrowsAsync on async method
            ["""
            var mock = new Mock<IFoo>();
            mock.Setup(x => x.BarAsync()).ThrowsAsync(new InvalidOperationException());
            """],

            // Variable mock + Callback chain + ThrowsAsync
            ["""
            var mock = new Mock<IFoo>();
            mock.Setup(x => x.BarAsync()).Callback(() => { }).ThrowsAsync(new InvalidOperationException());
            """],

            // #849 original report snippet 1: variable mock + MockBehavior.Strict + ReturnsAsync
            ["""
            var moq = new Mock<IFoo>(MockBehavior.Strict);
            moq.Setup(x => x.BarAsync()).ReturnsAsync(1);
            """],

            // #849 original report snippet 2: variable mock + MockBehavior.Strict + Callback + Returns(Task.FromResult)
            ["""
            var moq = new Mock<IFoo>(MockBehavior.Strict);
            moq.Setup(x => x.BarAsync()).Callback(() => { }).Returns(Task.FromResult(1));
            """],

            // #849 original report snippet 3 (was already OK): Returns(Task.FromResult) standalone on async method
            ["""
            var moq = new Mock<IFoo>(MockBehavior.Strict);
            moq.Setup(x => x.BarAsync()).Returns(Task.FromResult(1));
            """],

            // #849 original report snippet 4 (was already OK): Returns before Callback on async method
            ["""
            var moq = new Mock<IFoo>(MockBehavior.Strict);
            moq.Setup(x => x.BarAsync()).Returns(Task.FromResult(1)).Callback(() => { });
            """],

            // Variable mock + void method should not trigger Moq1203
            ["""
            var mock = new Mock<IFoo>();
            mock.Setup(x => x.DoVoidMethod());
            """],
        ];

        return data.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    // Diagnostic counterpart to SemanticVariationSetupTestData: variable-based mocks
    // without a return value specification, so Moq1203 should fire.
    public static IEnumerable<object[]> SemanticVariationSetupWithDiagnosticTestData()
    {
        IEnumerable<object[]> data =
        [

            // Variable mock, async, no return value
            ["""
            var mock = new Mock<IFoo>();
            {|Moq1203:mock.Setup(x => x.BarAsync())|};
            """],

            // Variable mock, sync, no return value
            ["""
            var mock = new Mock<IFoo>();
            {|Moq1203:mock.Setup(x => x.GetValue())|};
            """],

            // Variable mock + MockBehavior.Strict, no return value
            ["""
            var mock = new Mock<IFoo>(MockBehavior.Strict);
            {|Moq1203:mock.Setup(x => x.GetValue())|};
            """],

            // MockBehavior.Strict inline, no return value
            ["""{|Moq1203:new Mock<IFoo>(MockBehavior.Strict).Setup(x => x.GetValue())|};"""],

            // MockBehavior.Loose inline, async, no return value
            ["""{|Moq1203:new Mock<IFoo>(MockBehavior.Loose).Setup(x => x.BarAsync())|};"""],

            // Variable mock + MockBehavior.Loose, no return value
            ["""
            var mock = new Mock<IFoo>(MockBehavior.Loose);
            {|Moq1203:mock.Setup(x => x.GetValue())|};
            """],
        ];

        return data.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    // Reproduction of #849 (https://github.com/rjmurillo/moq.analyzers/issues/849#issuecomment-3913143509):
    // custom record type with ReturnsAsync. Verifies ReturnsAsync is recognized when
    // Task<T> has a custom type argument. Each entry includes the mock code, record
    // declaration, and interface method to exercise different record shapes.
    public static IEnumerable<object[]> CustomReturnTypeTestData()
    {
        IEnumerable<object[]> data =
        [

            // Variable mock + ReturnsAsync with custom record type
            ["""
            var expectedValue = new MyValue("test");
            var mock = new Mock<IDatabase>();
            mock.Setup(x => x.GetAsync()).ReturnsAsync(expectedValue);
            """, "public record MyValue(string Name);", "Task<MyValue> GetAsync();"],

            // Same as above but with explicit MockBehavior.Loose, inspired by DamienCassou's #849 reproduction
            ["""
            var expectedValue = new MyValue("test");
            var mock = new Mock<IDatabase>(MockBehavior.Loose);
            mock.Setup(x => x.GetAsync()).ReturnsAsync(expectedValue);
            """, "public record MyValue(string Name);", "Task<MyValue> GetAsync();"],

            // Exact reproduction from DamienCassou's #849 report: parameterless record, MockBehavior.Loose, variable argument
            // https://github.com/rjmurillo/moq.analyzers/issues/849#issuecomment-3913143509
            ["""
            var expectedValue = new MyValue();
            var databaseMock = new Mock<IDatabase>(MockBehavior.Loose);
            databaseMock.Setup(x => x.F()).ReturnsAsync(expectedValue);
            """, "public record MyValue;", "Task<MyValue> F();"],

            // Custom record type + ThrowsAsync on async method
            ["""
            var mock = new Mock<IDatabase>();
            mock.Setup(x => x.GetAsync()).ThrowsAsync(new InvalidOperationException());
            """, "public record MyValue(string Name);", "Task<MyValue> GetAsync();"],

            // Delegate-based ReturnsAsync with parameter: DamienCassou's #849 follow-up
            // https://github.com/rjmurillo/moq.analyzers/issues/849#issuecomment-3925720443
            ["""
            var databaseMock = new Mock<IDatabase>(MockBehavior.Strict);
            databaseMock.Setup(mock => mock.SaveAsync(It.IsAny<MyValue>())).ReturnsAsync((MyValue val) => val);
            """, "public record MyValue;", "Task<MyValue> SaveAsync(MyValue value);"],

            // Delegate-based ReturnsAsync with default MockBehavior, no variable
            ["""
            new Mock<IDatabase>().Setup(x => x.SaveAsync(It.IsAny<MyValue>())).ReturnsAsync((MyValue val) => val);
            """, "public record MyValue;", "Task<MyValue> SaveAsync(MyValue value);"],
        ];

        return data.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    // Negative counterpart to CustomReturnTypeTestData: uses the same IDatabase/MyValue
    // fixture but omits the return value specification, so Moq1203 should fire.
    public static IEnumerable<object[]> CustomReturnTypeMissingReturnValueTestData()
    {
        IEnumerable<object[]> data =
        [

            // Variable mock + custom record type, no return value (diagnostic expected)
            ["""
            var mock = new Mock<IDatabase>();
            {|Moq1203:mock.Setup(x => x.GetAsync())|};
            """, "public record MyValue(string Name);", "Task<MyValue> GetAsync();"],

            // Parameterless record + no return value (diagnostic expected)
            ["""
            var mock = new Mock<IDatabase>();
            {|Moq1203:mock.Setup(x => x.F())|};
            """, "public record MyValue;", "Task<MyValue> F();"],

            // SaveAsync with parameter, no return value (diagnostic expected)
            ["""
            var mock = new Mock<IDatabase>(MockBehavior.Strict);
            {|Moq1203:mock.Setup(x => x.SaveAsync(It.IsAny<MyValue>()))|};
            """, "public record MyValue;", "Task<MyValue> SaveAsync(MyValue value);"],
        ];

        return data.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldAnalyzeMethodSetupReturnValue(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await VerifyMockAsync(referenceAssemblyGroup, @namespace, mock);
    }

    [Theory]
    [MemberData(nameof(Issue849_FalsePositiveTestData))]
    public async Task ShouldNotFlagSetupWithReturnsAsyncOrCallbackChaining(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await VerifyMockAsync(referenceAssemblyGroup, @namespace, mock);
    }

    [Theory]
    [MemberData(nameof(CallbackOnlyNewMoqTestData))]
    public async Task ShouldFlagCallbackOnlySetupOnNewMoq(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await VerifyMockAsync(referenceAssemblyGroup, @namespace, mock);
    }

    [Theory]
    [MemberData(nameof(Issue887_ParenthesizedSetupTestData))]
    public async Task ShouldHandleParenthesizedSetupExpressions(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await VerifyMockAsync(referenceAssemblyGroup, @namespace, mock);
    }

    [Theory]
    [MemberData(nameof(Issue887_ParenthesizedSetupWithDiagnosticTestData))]
    public async Task ShouldFlagParenthesizedSetupWithoutReturnValue(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await VerifyMockAsync(referenceAssemblyGroup, @namespace, mock);
    }

    [Theory]
    [MemberData(nameof(DoppelgangerTestHelper.GetAllCustomMockData), MemberType = typeof(DoppelgangerTestHelper))]
    public async Task ShouldPassIfCustomMockClassIsUsed(string mockCode)
    {
        await Verifier.VerifyAnalyzerAsync(
            DoppelgangerTestHelper.CreateTestCode(mockCode),
            ReferenceAssemblyCatalog.Net80WithNewMoq);
    }

    [Theory]
    [MemberData(nameof(OverloadResolutionFailureTestData))]
    public async Task ShouldRecognizeReturnValueMethodFromCandidateSymbols(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await VerifyMockIgnoringCompilerErrorsAsync(referenceAssemblyGroup, @namespace, mock);
    }

    [Theory]
    [MemberData(nameof(OverloadResolutionFailureWithDiagnosticTestData))]
    public async Task ShouldFlagSetupWhenOnlyNonReturnValueCandidatesExist(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await VerifyMockIgnoringCompilerErrorsAsync(referenceAssemblyGroup, @namespace, mock);
    }

    [Theory]
    [MemberData(nameof(SemanticVariationSetupTestData))]
    public async Task ShouldNotFlagSemanticVariationSetups(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await VerifyMockAsync(referenceAssemblyGroup, @namespace, mock);
    }

    [Theory]
    [MemberData(nameof(SemanticVariationSetupWithDiagnosticTestData))]
    public async Task ShouldFlagSemanticVariationSetupWithoutReturnValue(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await VerifyMockAsync(referenceAssemblyGroup, @namespace, mock);
    }

    [Theory]
    [MemberData(nameof(CustomReturnTypeTestData))]
    public async Task ShouldNotFlagSetupWithCustomReturnType(string referenceAssemblyGroup, string @namespace, string mock, string recordDeclaration, string interfaceMethod)
    {
        await VerifyCustomSourceMockAsync(referenceAssemblyGroup, @namespace, mock, recordDeclaration, interfaceMethod);
    }

    [Theory]
    [MemberData(nameof(CustomReturnTypeMissingReturnValueTestData))]
    public async Task ShouldFlagSetupWithCustomReturnTypeMissingReturnValue(string referenceAssemblyGroup, string @namespace, string mock, string recordDeclaration, string interfaceMethod)
    {
        await VerifyCustomSourceMockAsync(referenceAssemblyGroup, @namespace, mock, recordDeclaration, interfaceMethod);
    }

    private static string BuildSource(string @namespace, string mock)
    {
        return $$"""
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
    }

    private static string BuildCustomSource(string @namespace, string mock, string recordDeclaration, string interfaceMethod)
    {
        return $$"""
            {{@namespace}}

            {{recordDeclaration}}

            public interface IDatabase
            {
                {{interfaceMethod}}
            }

            internal class UnitTest
            {
                private void Test()
                {
                    {{mock}}
                }
            }
            """;
    }

    private async Task VerifyMockAsync(string referenceAssemblyGroup, string @namespace, string mock)
    {
        string source = BuildSource(@namespace, mock);
        output.WriteLine(source);

        await Verifier.VerifyAnalyzerAsync(
            source,
            referenceAssemblyGroup).ConfigureAwait(false);
    }

    private async Task VerifyCustomSourceMockAsync(string referenceAssemblyGroup, string @namespace, string mock, string recordDeclaration, string interfaceMethod)
    {
        string source = BuildCustomSource(@namespace, mock, recordDeclaration, interfaceMethod);
        output.WriteLine(source);

        await Verifier.VerifyAnalyzerAsync(
            source,
            referenceAssemblyGroup).ConfigureAwait(false);
    }

    private async Task VerifyMockIgnoringCompilerErrorsAsync(string referenceAssemblyGroup, string @namespace, string mock)
    {
        string source = BuildSource(@namespace, mock);
        output.WriteLine(source);

        await Verifier.VerifyAnalyzerAsync(
            source,
            referenceAssemblyGroup,
            CompilerDiagnostics.None).ConfigureAwait(false);
    }
}
