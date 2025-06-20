using Moq.Analyzers.Test.Helpers;
using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.NoSealedClassMocksAnalyzer>;

namespace Moq.Analyzers.Test;

/// <summary>
/// Tests to verify analyzer coverage for custom DefaultValueProvider patterns from the quickstart guide.
/// These tests ensure that valid patterns don't trigger warnings from ANY analyzer.
/// This covers Pattern 3 from issue #508: Custom DefaultValueProvider.
/// </summary>
public class DefaultValueProviderPatternsAnalyzerTests
{
    public static IEnumerable<object[]> TestData()
    {
        return new object[][]
        {
            // Pattern 3a: Custom DefaultValueProvider - should not generate warnings
            ["""
            var mock = new Mock<IFoo>(MockBehavior.Strict) { DefaultValueProvider = new MyEmptyDefaultValueProvider() };
            """],

            // Pattern 3b: Default value usage - should not generate warnings
            ["""
            var mock = new Mock<IFoo>(MockBehavior.Strict) { DefaultValue = DefaultValue.Mock };
            """],

            // Pattern 3c: MockRepository with DefaultValue - should not generate warnings
            ["""
            var repository = new MockRepository(MockBehavior.Strict) { DefaultValue = DefaultValue.Mock };
            var fooMock = repository.Create<IFoo>();
            """],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups().Where(x => x[0]?.ToString()?.Contains("NewMoq") == true);
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldNotReportDiagnosticsForValidDefaultValueProviderPatterns(string referenceAssemblyGroup, string @namespace, string testCode)
    {
        await AllAnalyzersVerifier.VerifyAllAnalyzersAsync(
            $$"""
            [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("DynamicProxyGenAssembly2")]

            {{@namespace}}

            public interface IFoo
            {
                string Name { get; set; }
                void Execute();
            }

            public class MyEmptyDefaultValueProvider : Moq.LookupOrFallbackDefaultValueProvider
            {
                public MyEmptyDefaultValueProvider()
                {
                    Register(typeof(string), (type, mock) => "?");
                }
            }

            internal class UnitTest
            {
                private void Test()
                {
                    {{testCode}}
                }
            }
            """,
            referenceAssemblyGroup);
    }
}
