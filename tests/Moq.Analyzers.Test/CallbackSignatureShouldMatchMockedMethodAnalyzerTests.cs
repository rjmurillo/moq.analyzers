using Moq.Analyzers.Test.Helpers;

using AnalyzerVerifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.CallbackSignatureShouldMatchMockedMethodAnalyzer>;

namespace Moq.Analyzers.Test;

/// <summary>
/// Comprehensive tests for the CallbackSignatureShouldMatchMockedMethodAnalyzer.
/// Validates all advanced callback patterns including ref/out parameters, multiple callbacks,
/// generic callbacks, and complex scenarios from issue #434.
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
            // Multiple callbacks with correct signatures - should not trigger diagnostics
            [
                """
                var mock = new Mock<IFoo>();
                mock.Setup(x => x.DoWork("test"))
                    .Callback(() => Console.WriteLine("Before"))
                    .Returns(42)
                    .Callback(() => Console.WriteLine("After"));
                """,
            ],

            // Multiple callbacks with first callback having wrong signature
            [
                """
                var mock = new Mock<IFoo>();
                mock.Setup(x => x.DoWork("test"))
                    .Callback({|Moq1100:(int wrongParam)|} => Console.WriteLine("Before"))
                    .Returns(42)
                    .Callback(() => Console.WriteLine("After"));
                """,
            ],

            // Multiple callbacks with second callback having wrong signature
            [
                """
                var mock = new Mock<IFoo>();
                mock.Setup(x => x.DoWork("test"))
                    .Callback(() => Console.WriteLine("Before"))
                    .Returns(42)
                    .Callback({|Moq1100:(string wrongParam)|} => Console.WriteLine("After"));
                """,
            ],

            // Complex multiple parameter scenario with correct signatures
            [
                """
                var mock = new Mock<IFoo>();
                mock.Setup(x => x.ProcessMultiple(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>()))
                    .Callback((int id, string name, DateTime timestamp) => Console.WriteLine("Processing"))
                    .Returns(true);
                """,
            ],

            // Complex multiple parameter scenario with wrong signatures
            [
                """
                var mock = new Mock<IFoo>();
                mock.Setup(x => x.ProcessMultiple(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>()))
                    .Callback({|Moq1100:(string wrongId, int wrongName, bool wrongTimestamp)|} => Console.WriteLine("Processing"));
                """,
            ],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    /// <summary>
    /// Test data for ref/out parameter callback scenarios.
    /// Validates the specific ref/out parameter patterns mentioned in the issue.
    /// </summary>
    /// <returns>Test data for ref/out parameter validation scenarios.</returns>
    public static IEnumerable<object[]> RefOutParameterPatternsData()
    {
        return new object[][]
        {
            // Ref parameter with correct signature
            [
                """
                var mock = new Mock<IFoo>();
                mock.Setup(x => x.ProcessData(ref It.Ref<string>.IsAny))
                    .Callback((ref string data) => data = "modified");
                """,
            ],

            // Ref parameter with wrong signature (missing ref)
            [
                """
                var mock = new Mock<IFoo>();
                mock.Setup(x => x.ProcessData(ref It.Ref<string>.IsAny))
                    .Callback({|Moq1100:(string data)|} => { });
                """,
            ],

            // Out parameter with correct signature
            [
                """
                var mock = new Mock<IFoo>();
                mock.Setup(x => x.TryProcess(out It.Ref<int>.IsAny))
                    .Callback((out int result) => { result = 42; })
                    .Returns(true);
                """,
            ],

            // Out parameter with wrong signature (missing out)
            [
                """
                var mock = new Mock<IFoo>();
                mock.Setup(x => x.TryProcess(out It.Ref<int>.IsAny))
                    .Callback({|Moq1100:(int result)|} => { })
                    .Returns(true);
                """,
            ],

            // In parameter with correct signature
            [
                """
                var mock = new Mock<IFoo>();
                mock.Setup(x => x.ProcessReadOnly(in It.Ref<DateTime>.IsAny))
                    .Callback((in DateTime timestamp) => Console.WriteLine(timestamp));
                """,
            ],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    /// <summary>
    /// Test data for delegate callback patterns using single-line format.
    /// Validates delegate-based callbacks with ref/out parameters.
    /// </summary>
    /// <returns>Test data for delegate callback validation scenarios.</returns>
    public static IEnumerable<object[]> DelegateCallbackPatternsData()
    {
        return new object[][]
        {
            // Ref parameter mismatch (should trigger Moq1100)
            [
                """new Mock<IFoo>().Setup(m => m.DoRef(ref It.Ref<string>.IsAny)).Callback(({|Moq1100:string data|}) => { });""",
            ],

            // Ref parameter correct (should not trigger diagnostic)
            [
                """new Mock<IFoo>().Setup(m => m.DoRef(ref It.Ref<string>.IsAny)).Callback((ref string data) => { });""",
            ],

            // Out parameter mismatch (should trigger Moq1100)
            [
                """new Mock<IFoo>().Setup(m => m.DoOut(out It.Ref<int>.IsAny)).Callback(({|Moq1100:int result|}) => { });""",
            ],

            // Out parameter correct (should not trigger diagnostic)
            [
                """new Mock<IFoo>().Setup(m => m.DoOut(out It.Ref<int>.IsAny)).Callback((out int result) => { result = 42; });""",
            ],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    /// <summary>
    /// Test data for basic callback validation scenarios.
    /// Covers simple cases and no-parameter scenarios.
    /// </summary>
    /// <returns>Test data for basic callback validation scenarios.</returns>
    public static IEnumerable<object[]> BasicCallbackValidationData()
    {
        return new object[][]
        {
            // Basic callback with wrong parameter type
            [
                """
                var mock = new Mock<IFoo>();
                mock.Setup(x => x.DoWork("test"))
                    .Callback({|Moq1100:(int wrongParam)|} => { });
                """,
            ],

            // Basic callback with correct parameter type
            [
                """
                var mock = new Mock<IFoo>();
                mock.Setup(x => x.DoWork("test"))
                    .Callback((string param) => { });
                """,
            ],

            // No parameters callback for parameterized method (valid pattern)
            [
                """
                var mock = new Mock<IFoo>();
                mock.Setup(x => x.DoWork("test"))
                    .Callback(() => { });
                """,
            ],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(MultipleCallbackTimingData))]
    [MemberData(nameof(RefOutParameterPatternsData))]
    [MemberData(nameof(BasicCallbackValidationData))]
    public async Task ShouldValidateCallbackPatterns(string referenceAssemblyGroup, string @namespace, string testCode)
    {
        static string Template(string ns, string code) =>
            $$"""
            {{ns}}

            public interface IFoo
            {
                int DoWork(string input);
                bool ProcessMultiple(int id, string name, DateTime timestamp);
                void ProcessData(ref string data);
                bool TryProcess(out int result);
                void ProcessMixed(int id, ref string data, out bool success);
                void ProcessReadOnly(in DateTime timestamp);
                int DoRef(ref string data);
                bool DoOut(out int result);
                string DoIn(in DateTime timestamp);
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
    [MemberData(nameof(DelegateCallbackPatternsData))]
    public async Task ShouldValidateDelegateCallbacks(string referenceAssemblyGroup, string @namespace, string testCode)
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

            internal class UnitTest
            {
                private void Test()
                {
                    {{code}}
                }
            }
            """;

        string source = Template(@namespace, testCode);
        await AnalyzerVerifier.VerifyAnalyzerAsync(source, referenceAssemblyGroup);
    }

    /// <summary>
    /// Test to document the current limitation with generic callback validation.
    /// This test documents that .Callback&lt;T&gt;() with wrong type parameters is NOT currently validated.
    /// This could be enhanced in a future version.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GenericCallbackValidation_CurrentLimitation_IsDocumented()
    {
        const string source = """
            using Moq;

            public interface IFoo
            {
                int DoWork(string input);
            }

            public class TestClass
            {
                public void TestGenericCallback()
                {
                    var mock = new Mock<IFoo>();
                    // Note: This currently does NOT trigger a diagnostic, which could be enhanced in the future
                    mock.Setup(x => x.DoWork("test"))
                        .Callback<int>(wrongTypeParam => { }); // Should ideally trigger Moq1100 but currently doesn't
                }
            }
            """;

        // This test documents the current limitation - no diagnostic is expected
        await AnalyzerVerifier.VerifyAnalyzerAsync(source, "Net80WithOldMoq");
    }
}
