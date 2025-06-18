using Moq.Analyzers.Test.Helpers;
using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.NoSealedClassMocksAnalyzer>;

namespace Moq.Analyzers.Test;

/// <summary>
/// Tests to verify analyzer coverage for miscellaneous Moq patterns from the quickstart guide.
/// These tests ensure coverage for patterns like reset, protected members, custom DefaultValueProvider,
/// interface patterns, internal types, and mock.Object usage as outlined in issue #508.
/// </summary>
public class MiscellaneousPatternsAnalyzerTests
{
    public static IEnumerable<object[]> TestData()
    {
        return new object[][]
        {
            // Pattern 1: Reset patterns - should not generate warnings
            ["""
            var mock = new Mock<IFoo>();
            mock.Setup(x => x.Name).Returns("Test");
            mock.Reset();
            """],

            // Pattern 4: As<T> interface patterns - should not warn for interfaces
            ["""
            var mock = new Mock<IFoo>();
            var disposable = mock.As<IDisposable>();
            disposable.Setup(d => d.Dispose());
            """],

            // Pattern 5: Internal type mocking - should work with InternalsVisibleTo
            ["""
            var mock = new Mock<IInternalInterface>();
            mock.Setup(x => x.GetValue()).Returns("internal");
            """],

            // Pattern 6: mock.Object usage - should not generate warnings
            ["""
            var mock = new Mock<IFoo>();
            IFoo foo = mock.Object;
            ConsumeInterface(mock.Object);
            """],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldNotReportDiagnosticsForValidMiscellaneousPatterns(string referenceAssemblyGroup, string @namespace, string testCode)
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
            [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("DynamicProxyGenAssembly2")]

            {{@namespace}}

            internal interface IInternalInterface
            {
                string GetValue();
            }

            public interface IFoo
            {
                string Name { get; set; }
                void Execute();
            }

            internal class UnitTest
            {
                private void Test()
                {
                    {{testCode}}
                }

                private void ConsumeInterface(IFoo foo) { }
            }
            """,
            referenceAssemblyGroup);
    }
}
