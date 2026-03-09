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

            // In parameter with correct signature
            ["""new Mock<IFoo>().Setup(m => m.DoIn(in It.Ref<DateTime>.IsAny)).Callback((in DateTime timestamp) => { });"""],

            // Implicitly typed lambda with correct parameter type via generic Callback overload
            ["""new Mock<IFoo>().Setup(x => x.DoWork("test")).Callback<string>((x) => { });"""],

            // Implicitly typed lambda with multiple correct parameters via generic Callback overload
            ["""new Mock<IFoo>().Setup(x => x.ProcessMultiple(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>())).Callback<int, string, DateTime>((id, name, timestamp) => { });"""],

            // Explicitly typed lambda with correct parameter type (exercises GetDeclaredSymbol path)
            ["""new Mock<IFoo>().Setup(x => x.DoWork("test")).Callback((string x) => { });"""],

            // Simple lambda in delegate constructor with correct type (issue #1012)
            ["""new Mock<IFoo>().Setup(x => x.DoWork("test")).Callback(new Action<string>(x => { }));"""],

            // Simple lambda in delegate constructor with correct type using Returns (issue #1012)
            ["""new Mock<IFoo>().Setup(x => x.DoWork("test")).Returns(new Func<string, int>(x => 42));"""],

            // Parenthesized lambda in delegate constructor with correct type (issue #1012)
            ["""new Mock<IFoo>().Setup(x => x.DoWork("test")).Callback(new Action<string>((string x) => { }));"""],
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

            // In parameter mismatch (missing in modifier)
            ["""new Mock<IFoo>().Setup(m => m.DoIn(in It.Ref<DateTime>.IsAny)).Callback(({|Moq1100:DateTime timestamp|}) => { });"""],

            // Parenthesized Setup with wrong callback type
            ["""(new Mock<IFoo>().Setup(x => x.DoWork("test"))).Callback(({|Moq1100:int wrongParam|}) => { });"""],

            // Double-parenthesized Setup with wrong callback type
            ["""((new Mock<IFoo>().Setup(x => x.DoWork("test")))).Callback(({|Moq1100:int wrongParam|}) => { });"""],

            // Explicitly typed lambda with wrong parameter type (exercises semantic resolution mismatch)
            ["""new Mock<IFoo>().Setup(x => x.DoWork("test")).Callback(({|Moq1100:DateTime wrongParam|}) => { });"""],

            // Simple lambda in delegate constructor with wrong type (issue #1012)
            ["""new Mock<IFoo>().Setup(x => x.DoWork("test")).Callback(new Action<int>({|Moq1100:x|} => { }));"""],

            // Simple lambda in delegate constructor with argument count mismatch (issue #1012)
            ["""new Mock<IFoo>().Setup(x => x.ProcessMultiple(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>())).Callback(new Action<int>({|Moq1100:x|} => { }));"""],

            // Parenthesized lambda in delegate constructor with wrong type (issue #1012)
            ["""new Mock<IFoo>().Setup(x => x.DoWork("test")).Callback(new Action<int>(({|Moq1100:int x|}) => { }));"""],

            // Parenthesized lambda in delegate constructor with wrong argument count (issue #1012)
            ["""new Mock<IFoo>().Setup(x => x.ProcessMultiple(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>())).Callback(new Action<int>({|Moq1100:(int x)|} => { }));"""],
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
    /// do not trigger a diagnostic. Even though the delegate type is ambiguous (CS8917),
    /// Roslyn's semantic model resolves the parameter type via best-effort binding.
    /// The analyzer uses GetDeclaredSymbol to obtain the inferred type and validates correctly.
    /// </summary>
    /// <param name="referenceAssemblyGroup">The Moq version reference assembly group.</param>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Theory]
    [InlineData("Net80WithOldMoq")]
    [InlineData("Net80WithNewMoq")]
    public async Task ImplicitlyTypedLambdaWithoutGenericOverload_NoFalsePositive(string referenceAssemblyGroup)
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
                    // Implicitly typed lambda without generic overload.
                    // Roslyn resolves 'x' to 'string' via best-effort binding.
                    new Mock<IFoo>().Setup(x => x.DoWork("test")).Callback((x) => { });
                }
            }
            """;

        // CompilerDiagnostics.None suppresses CS8917/CS1660 from the ambiguous delegate type.
        // No Moq1100 is expected because GetDeclaredSymbol resolves 'x' to 'string' which matches.
        await AnalyzerVerifier.VerifyAnalyzerAsync(source, referenceAssemblyGroup, CompilerDiagnostics.None);
    }

    /// <summary>
    /// Proves the thesis of issue #995: when a lambda parameter uses an unresolvable type,
    /// the analyzer reports a diagnostic instead of silently suppressing it. Before the fix,
    /// the old code returned true ("assume ok") when type resolution failed, hiding real
    /// mismatches from users.
    /// </summary>
    /// <param name="referenceAssemblyGroup">The Moq version reference assembly group.</param>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Theory]
    [InlineData("Net80WithOldMoq")]
    [InlineData("Net80WithNewMoq")]
    public async Task UnresolvableParameterType_ReportsDiagnostic(string referenceAssemblyGroup)
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
                    // 'NonExistentType' does not resolve, producing TypeKind.Error.
                    // The analyzer treats this as a mismatch rather than silently suppressing Moq1100.
                    new Mock<IFoo>().Setup(x => x.DoWork("test")).Callback(({|Moq1100:NonExistentType x|}) => { });
                }
            }
            """;

        // CompilerDiagnostics.None suppresses CS0246 from the unresolvable type name.
        await AnalyzerVerifier.VerifyAnalyzerAsync(source, referenceAssemblyGroup, CompilerDiagnostics.None);
    }

    /// <summary>
    /// Verifies that .Callback&lt;T&gt;() with a wrong type parameter produces a diagnostic.
    /// The analyzer uses symbol-based resolution of the generic type argument to validate the
    /// callback parameter type. It correctly detects the type mismatch between .Callback&lt;int&gt;()
    /// and the mocked method parameter type (string).
    /// </summary>
    /// <param name="referenceAssemblyGroup">The Moq version reference assembly group.</param>
    /// <returns>A task representing the asynchronous unit test.</returns>
    [Theory]
    [InlineData("Net80WithOldMoq")]
    [InlineData("Net80WithNewMoq")]
    public async Task GenericCallbackWithWrongType_ProducesDiagnostic(string referenceAssemblyGroup)
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
                    // .Callback<int>() mismatches the mocked method parameter type (string).
                    // The analyzer detects this via symbol-based resolution of the generic type argument.
                    mock.Setup(x => x.DoWork("test"))
                        .Callback<int>({|Moq1100:wrongTypeParam|} => { });
                }
            }
            """;

        await AnalyzerVerifier.VerifyAnalyzerAsync(source, referenceAssemblyGroup);
    }
}
