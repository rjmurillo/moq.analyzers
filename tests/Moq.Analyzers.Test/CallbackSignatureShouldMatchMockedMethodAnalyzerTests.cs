using Moq.Analyzers.Test.Helpers;

using AnalyzerVerifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.CallbackSignatureShouldMatchMockedMethodAnalyzer>;

namespace Moq.Analyzers.Test;

/// <summary>
/// Tests for the CallbackSignatureShouldMatchMockedMethodAnalyzer to validate advanced callback patterns.
/// This test class focuses on analyzer-only scenarios (not code fixes).
/// </summary>
public class CallbackSignatureShouldMatchMockedMethodAnalyzerTests
{
    /// <summary>
    /// Test data for multiple callback timing scenarios.
    /// These test cases validate the analyzer's ability to handle callback chains.
    /// </summary>
    /// <returns>Test data for multiple callback timing scenarios.</returns>
    public static IEnumerable<object[]> MultipleCallbackTimingData()
    {
        return new object[][]
        {
            // Simple multiple callbacks with correct signatures - should not trigger diagnostic
            [
                """new Mock<IFoo>().Setup(x => x.DoWork("test")).Callback(() => { }).Returns(42).Callback(() => { });""",
            ],

            // Multiple callbacks with wrong signature in first callback
            [
                """new Mock<IFoo>().Setup(x => x.DoWork("test")).Callback(({|Moq1100:int wrongParam|}) => { }).Returns(42).Callback(() => { });""",
            ],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    /// <summary>
    /// Test data for generic callback scenarios.
    /// Validates callback signatures for generic method setups.
    /// Note: Generic type parameter validation is currently a limitation - not detected by the analyzer.
    /// </summary>
    /// <returns>Test data for generic callback validation scenarios.</returns>
    public static IEnumerable<object[]> GenericCallbackData()
    {
        return new object[][]
        {
            // Generic callback with correct type parameter
            [
                """new Mock<IFoo>().Setup(x => x.ProcessGeneric<string>("test")).Callback<string>(s => { });""",
            ],

            // Note: This currently does NOT trigger a diagnostic - limitation in the current analyzer
            // Generic callback with wrong type parameter - currently not detected
            [
                """new Mock<IFoo>().Setup(x => x.ProcessGeneric<string>("test")).Callback<int>(i => { });""",
            ],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    /// <summary>
    /// Test data for complex delegate callback patterns.
    /// Validates delegate-based callbacks with ref/out parameters.
    /// </summary>
    /// <returns>Test data for complex delegate callback validation scenarios.</returns>
    public static IEnumerable<object[]> ComplexDelegateCallbackData()
    {
        return new object[][]
        {
            // Ref parameter with correct signature (using simple lambda)
            [
                """new Mock<IFoo>().Setup(x => x.DoRef(ref It.Ref<string>.IsAny)).Callback((ref string data) => data = "processed");""",
            ],

            // Ref parameter with wrong signature (missing ref) - should trigger diagnostic
            [
                """new Mock<IFoo>().Setup(x => x.DoRef(ref It.Ref<string>.IsAny)).Callback(({|Moq1100:string data|}) => { });""",
            ],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(MultipleCallbackTimingData))]
    public async Task ShouldValidateMultipleCallbackTiming(string referenceAssemblyGroup, string @namespace, string testCode)
    {
        static string Template(string ns, string code) =>
            $$"""
            {{ns}}

            public interface IFoo
            {
                int DoWork(string input);
                void ProcessData(ref string data);
                bool TryProcess(out int result);
                T ProcessGeneric<T>(T input);
            }

            public class TestClass
            {
                public void TestMethod()
                {
                    {{code}}
                }
            }
            """;

        string source = Template(@namespace, testCode);
        await AnalyzerVerifier.VerifyAnalyzerAsync(source, referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(GenericCallbackData))]
    public async Task ShouldValidateGenericCallbacks(string referenceAssemblyGroup, string @namespace, string testCode)
    {
        static string Template(string ns, string code) =>
            $$"""
            {{ns}}

            public interface IFoo
            {
                int DoWork(string input);
                void ProcessData(ref string data);
                bool TryProcess(out int result);
                T ProcessGeneric<T>(T input);
            }

            public class TestClass
            {
                public void TestGenericMethod()
                {
                    {{code}}
                }
            }
            """;

        string source = Template(@namespace, testCode);
        await AnalyzerVerifier.VerifyAnalyzerAsync(source, referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(ComplexDelegateCallbackData))]
    public async Task ShouldValidateComplexDelegateCallbacks(string referenceAssemblyGroup, string @namespace, string testCode)
    {
        static string Template(string ns, string code) =>
            $$"""
            {{ns}}

            internal interface IFoo
            {
                int DoWork(string input);
                int DoRef(ref string data);
                bool DoOut(out int result);
                string DoIn(in DateTime timestamp);
                T ProcessGeneric<T>(T input);
            }

            internal class TestClass
            {
                private void TestDelegateMethod()
                {
                    {{code}}
                }
            }
            """;

        string source = Template(@namespace, testCode);
        await AnalyzerVerifier.VerifyAnalyzerAsync(source, referenceAssemblyGroup);
    }
}
