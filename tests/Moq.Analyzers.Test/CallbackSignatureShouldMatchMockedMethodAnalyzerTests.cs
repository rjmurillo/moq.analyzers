using Microsoft.CodeAnalysis.Testing;
using Moq.Analyzers.Test.Helpers;

using AnalyzerVerifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.CallbackSignatureShouldMatchMockedMethodAnalyzer>;

namespace Moq.Analyzers.Test;

/// <summary>
/// Comprehensive tests for the CallbackSignatureShouldMatchMockedMethodAnalyzer.
/// Validates all advanced callback patterns including ref/out parameters, multiple callbacks,
/// generic callbacks, and complex scenarios from issue #434.
/// </summary>
public class CallbackSignatureShouldMatchMockedMethodAnalyzerTests(ITestOutputHelper output)
{
    /// <summary>
    /// Consolidated test data for all callback validation scenarios.
    /// Combines valid patterns (should not trigger diagnostics) and invalid patterns (should trigger diagnostics).
    /// </summary>
    /// <returns>Test data for comprehensive callback validation scenarios.</returns>
    public static IEnumerable<object[]> CallbackValidationData()
    {
        // Valid patterns that should NOT trigger the analyzer
        IEnumerable<object[]> validPatterns = new object[][]
        {
            // Multiple callbacks with correct signatures
            ["""new Mock<IFoo>().Setup(x => x.DoWork("test")).Callback(() => { }).Returns(42).Callback(() => { });"""],

            // Ref parameter with correct signature
            ["""new Mock<IFoo>().Setup(m => m.DoRef(ref It.Ref<string>.IsAny)).Callback((ref string data) => { });"""],

            // Out parameter with correct signature
            ["""new Mock<IFoo>().Setup(m => m.DoOut(out It.Ref<int>.IsAny)).Callback((out int result) => { result = 42; });"""],

            // Basic callback with correct parameter type
            ["""new Mock<IFoo>().Setup(x => x.DoWork("test")).Callback((string param) => { });"""],

            // No parameters callback for parameterized method (valid pattern)
            ["""new Mock<IFoo>().Setup(x => x.DoWork("test")).Callback(() => { });"""],

            // Complex multiple parameter with correct signatures
            ["""new Mock<IFoo>().Setup(x => x.ProcessMultiple(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>())).Callback((int id, string name, DateTime timestamp) => { });"""],

            // Parenthesized Setup with correct callback
            ["""(new Mock<IFoo>().Setup(x => x.DoWork("test"))).Callback((string param) => { });"""],

            // Double-parenthesized Setup with correct callback
            ["""((new Mock<IFoo>().Setup(x => x.DoWork("test")))).Callback((string param) => { });"""],

            // Implicitly typed lambda with correct parameter type via generic Callback overload
            ["""new Mock<IFoo>().Setup(x => x.DoWork("test")).Callback<string>((x) => { });"""],

            // Implicitly typed lambda with multiple correct parameters via generic Callback overload
            ["""new Mock<IFoo>().Setup(x => x.ProcessMultiple(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>())).Callback<int, string, DateTime>((id, name, timestamp) => { });"""],

            // Explicitly typed lambda with correct parameter type (exercises GetDeclaredSymbol path)
            ["""new Mock<IFoo>().Setup(x => x.DoWork("test")).Callback((string x) => { });"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();

        // Invalid patterns that SHOULD trigger the analyzer
        IEnumerable<object[]> invalidPatterns = new object[][]
        {
            // Basic callback with wrong parameter type
            ["""new Mock<IFoo>().Setup(x => x.DoWork("test")).Callback(({|Moq1100:int wrongParam|}) => { });"""],

            // Ref parameter mismatch (missing ref)
            ["""new Mock<IFoo>().Setup(m => m.DoRef(ref It.Ref<string>.IsAny)).Callback(({|Moq1100:string data|}) => { });"""],

            // Out parameter mismatch (missing out)
            ["""new Mock<IFoo>().Setup(m => m.DoOut(out It.Ref<int>.IsAny)).Callback(({|Moq1100:int result|}) => { });"""],

            // Parenthesized Setup with wrong callback type
            ["""(new Mock<IFoo>().Setup(x => x.DoWork("test"))).Callback(({|Moq1100:int wrongParam|}) => { });"""],

            // Double-parenthesized Setup with wrong callback type
            ["""((new Mock<IFoo>().Setup(x => x.DoWork("test")))).Callback(({|Moq1100:int wrongParam|}) => { });"""],

            // Explicitly typed lambda with wrong parameter type (exercises semantic resolution mismatch)
            ["""new Mock<IFoo>().Setup(x => x.DoWork("test")).Callback(({|Moq1100:DateTime wrongParam|}) => { });"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();

        return validPatterns.Concat(invalidPatterns);
    }

    [Theory]
    [MemberData(nameof(CallbackValidationData))]
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
        output.WriteLine(source);
        await AnalyzerVerifier.VerifyAnalyzerAsync(source, referenceAssemblyGroup);
    }

    /// <summary>
    /// Verifies that implicitly typed lambda parameters without a generic Callback overload
    /// do not trigger a diagnostic. The compiler cannot resolve the parameter type, so the
    /// analyzer treats it as unresolvable and skips validation.
    /// </summary>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ImplicitlyTypedLambdaWithoutGenericOverload_SkipsValidation()
    {
        const string source = """
            using Moq;

            public interface IFoo
            {
                int DoWork(string input);
            }

            public class TestClass
            {
                public void TestMethod()
                {
                    // Implicitly typed lambda without generic overload: type is unresolvable
                    new Mock<IFoo>().Setup(x => x.DoWork("test")).Callback((x) => { });
                }
            }
            """;

        // CompilerDiagnostics.None suppresses CS8917/CS1660 from the unresolvable delegate type.
        // The analyzer should not report Moq1100 because the parameter type cannot be resolved.
        await AnalyzerVerifier.VerifyAnalyzerAsync(source, "Net80WithOldMoq", CompilerDiagnostics.None);
    }

    /// <summary>
    /// Test to document the known limitation with generic callback validation.
    /// This test documents that .Callback&lt;T&gt;() with wrong type parameters is NOT currently validated.
    /// This is an accepted limitation as the explicit generic syntax is rarely used in practice.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Analysis shows zero real-world usage of the explicit generic .Callback&lt;T&gt;() syntax in open-source projects.
    /// The recommended approach is to use lambda parameter inference which provides full type validation:
    /// <c>.Callback(param => { })</c> or <c>.Callback((string param) => { })</c>.
    /// </para>
    /// <para>
    /// See docs/rules/Moq1100.md "Known Limitations" section for best practices.
    /// </para>
    /// </remarks>
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
                    // Note: This does NOT trigger a diagnostic (known limitation)
                    // Best practice: Use .Callback(param => { }) instead of .Callback<T>(param => { })
                    mock.Setup(x => x.DoWork("test"))
                        .Callback<int>(wrongTypeParam => { }); // Generic syntax not validated
                }
            }
            """;

        // This test documents the known limitation - no diagnostic is expected
        await AnalyzerVerifier.VerifyAnalyzerAsync(source, "Net80WithOldMoq");
    }
}
