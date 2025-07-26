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
            // Multiple callbacks in chain with correct signatures - should not trigger diagnostic
            [
                """
                var mock = new Mock<IFoo>();
                mock.Setup(x => x.DoWork("test"))
                    .Callback(() => Console.WriteLine("Before"))
                    .Returns(42)
                    .Callback(() => Console.WriteLine("After"));
                """,
            ],

            // Multiple callbacks with wrong signatures - should trigger diagnostics
            [
                """
                var mock = new Mock<IFoo>();
                mock.Setup(x => x.DoWork("test"))
                    .Callback({|Moq1100:(int wrongParam)|} => Console.WriteLine("Before"))
                    .Returns(42)
                    .Callback(() => Console.WriteLine("After"));
                """,
            ],

            // Second callback with wrong signature
            [
                """
                var mock = new Mock<IFoo>();
                mock.Setup(x => x.DoWork("test"))
                    .Callback(() => Console.WriteLine("Before"))
                    .Returns(42)
                    .Callback({|Moq1100:(string wrongParam)|} => Console.WriteLine("After"));
                """,
            ],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    /// <summary>
    /// Test data for generic callback scenarios.
    /// Validates callback signatures for generic method setups.
    /// </summary>
    /// <returns>Test data for generic callback validation scenarios.</returns>
    public static IEnumerable<object[]> GenericCallbackData()
    {
        return new object[][]
        {
            // Generic callback with correct type parameter
            [
                """
                var mock = new Mock<IFoo>();
                mock.Setup(x => x.ProcessGeneric<string>("test"))
                    .Callback<string>(s => Console.WriteLine($"Processing {s}"));
                """,
            ],

            // Generic callback with wrong type parameter - should trigger diagnostic
            [
                """
                var mock = new Mock<IFoo>();
                mock.Setup(x => x.ProcessGeneric<string>("test"))
                    .Callback<int>({|Moq1100:i|} => Console.WriteLine($"Processing {i}"));
                """,
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
            // Delegate-based callback with ref parameter - correct signature
            [
                """
                var mock = new Mock<IFoo>();
                mock.Setup(x => x.ProcessData(ref It.Ref<string>.IsAny))
                    .Callback(new ProcessDataCallback((ref string data) => data = "processed"));
                """,
            ],

            // Delegate-based callback with wrong ref parameter type - should trigger diagnostic
            [
                """
                var mock = new Mock<IFoo>();
                mock.Setup(x => x.ProcessData(ref It.Ref<string>.IsAny))
                    .Callback(new ProcessDataCallback({|Moq1100:(ref int data)|} => data = 42));
                """,
            ],

            // Out parameter delegate callback - correct signature
            [
                """
                var mock = new Mock<IFoo>();
                mock.Setup(x => x.TryProcess(out It.Ref<int>.IsAny))
                    .Callback(new TryProcessCallback((out int result) => { result = 42; }))
                    .Returns(true);
                """,
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

            public interface IFoo
            {
                int DoWork(string input);
                void ProcessData(ref string data);
                bool TryProcess(out int result);
                T ProcessGeneric<T>(T input);
            }

            public delegate void ProcessDataCallback(ref string data);
            public delegate bool TryProcessCallback(out int result);

            public class TestClass
            {
                public void TestDelegateMethod()
                {
                    {{code}}
                }
            }
            """;

        string source = Template(@namespace, testCode);
        await AnalyzerVerifier.VerifyAnalyzerAsync(source, referenceAssemblyGroup);
    }
}
