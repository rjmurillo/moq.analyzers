using Moq.Analyzers.Test.Helpers;

using AnalyzerVerifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.CallbackSignatureShouldMatchMockedMethodAnalyzer>;

namespace Moq.Analyzers.Test;

/// <summary>
/// Comprehensive tests validating that the CallbackSignatureShouldMatchMockedMethodAnalyzer
/// correctly handles advanced callback patterns including ref/out parameters and complex scenarios.
/// This provides complete coverage for the patterns mentioned in issue #434.
/// </summary>
public class CallbackAdvancedPatternsValidationTests
{
    /// <summary>
    /// Test data for multiple callback timing scenarios (before and after Returns).
    /// Validates that the analyzer correctly handles callback chains with proper signatures.
    /// </summary>
    /// <returns>Test data for multiple callback validation scenarios.</returns>
    public static IEnumerable<object[]> MultipleCallbackPatternsData()
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
    /// Test data for delegate-based callback patterns.
    /// Validates advanced delegate constructor callback scenarios.
    /// </summary>
    /// <returns>Test data for delegate callback validation scenarios.</returns>
    public static IEnumerable<object[]> DelegateCallbackPatternsData()
    {
        return new object[][]
        {
            // Delegate-based callback with correct signature
            [
                """
                delegate void ProcessDataCallback(ref string data);
                var mock = new Mock<IFoo>();
                mock.Setup(x => x.ProcessData(ref It.Ref<string>.IsAny))
                    .Callback(new ProcessDataCallback((ref string data) => data = "processed"));
                """,
            ],

            // Delegate-based callback with wrong parameter type
            [
                """
                delegate void ProcessDataCallback(ref int data);
                var mock = new Mock<IFoo>();
                mock.Setup(x => x.ProcessData(ref It.Ref<string>.IsAny))
                    .Callback(new ProcessDataCallback({|Moq1100:(ref int data)|} => data = 42));
                """,
            ],

            // Out parameter delegate callback with correct signature
            [
                """
                delegate bool TryProcessCallback(out int result);
                var mock = new Mock<IFoo>();
                mock.Setup(x => x.TryProcess(out It.Ref<int>.IsAny))
                    .Callback(new TryProcessCallback((out int result) => { result = 42; }))
                    .Returns(true);
                """,
            ],

            // Multiple parameter delegate callback with mixed ref/out
            [
                """
                delegate void MixedCallback(int id, ref string data, out bool success);
                var mock = new Mock<IFoo>();
                mock.Setup(x => x.ProcessMixed(It.IsAny<int>(), ref It.Ref<string>.IsAny, out It.Ref<bool>.IsAny))
                    .Callback(new MixedCallback((int id, ref string data, out bool success) => 
                    { 
                        data = $"processed_{id}"; 
                        success = true; 
                    }));
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

    [Theory]
    [MemberData(nameof(MultipleCallbackPatternsData))]
    public async Task ShouldValidateMultipleCallbackPatterns(string referenceAssemblyGroup, string @namespace, string testCode)
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
            }

            public class TestClass
            {
                public void TestMultipleCallbacks()
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
    public async Task ShouldValidateDelegateCallbackPatterns(string referenceAssemblyGroup, string @namespace, string testCode)
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
            }

            public class TestClass
            {
                public void TestDelegateCallbacks()
                {
                    {{code}}
                }
            }
            """;

        string source = Template(@namespace, testCode);
        await AnalyzerVerifier.VerifyAnalyzerAsync(source, referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(RefOutParameterPatternsData))]
    public async Task ShouldValidateRefOutParameterPatterns(string referenceAssemblyGroup, string @namespace, string testCode)
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
            }

            public class TestClass
            {
                public void TestRefOutCallbacks()
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

    /// <summary>
    /// Test that validates correct generic callback usage works as expected.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GenericCallbackValidation_CorrectUsage_NoDignostic()
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
                    // This should not trigger any diagnostic (correct usage)
                    mock.Setup(x => x.DoWork("test"))
                        .Callback<string>(param => Console.WriteLine($"Processing: {param}"));
                }
            }
            """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(source, "Net80WithOldMoq");
    }
}
