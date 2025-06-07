using Moq.Analyzers.Test.Helpers;
using Xunit;

namespace Moq.Analyzers.Test;

/// <summary>
/// Tests demonstrating potential analyzer opportunities identified from Moq quickstart analysis.
/// These tests document scenarios where new analyzers COULD provide helpful guidance
/// but currently pass without warnings.
///
/// Note: These tests currently PASS (no warnings) but represent opportunities for future analyzer development.
/// </summary>
public class MoqQuickstartAnalyzerOpportunitiesTests
{
    /// <summary>
    /// Demonstrates verification scenarios that now trigger analyzer guidance.
    /// The analyzer warns about verifying non-overridable members.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task VerificationPatternsOpportunities()
    {
        const string source = """
            using Moq;

            public class SampleClass
            {
                // Non-virtual method - verification might not work as expected
                public bool NonVirtualMethod() => true;
            }

            public interface IFoo 
            {
                bool DoSomething(string value);
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var mock = new Mock<SampleClass>();
                    // ANALYZER WARNING: Warn about verifying non-virtual members
                    // Verification might not work as expected
                    {|Moq1500:mock.Verify(x => x.NonVirtualMethod())|};

                    var ifoo = new Mock<IFoo>();
                    // Interface methods are always overridable, so no warning here
                    ifoo.Setup(x => x.DoSomething("test")).Returns(true);
                    ifoo.Verify(x => x.DoSomething("different"), Times.AtLeastOnce());
                }
            }
            """;

        // Now triggers warnings for verifying non-virtual method
        await AnalyzerVerifier<VerifyOnlyUsedForOverridableMembersAnalyzer>.VerifyAnalyzerAsync(source, ReferenceAssemblyCatalog.Net80WithNewMoq);
    }

    /// <summary>
    /// Demonstrates LINQ to Mocks scenarios that now trigger analyzer guidance.
    /// The analyzer warns about complex expressions in Mock.Of.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task LinqToMocksOpportunities()
    {
        const string source = """
            using Moq;

            public interface IRepository 
            { 
                bool IsAuthenticated { get; }
                string GetData(int id);
            }

            internal class UnitTest
            {
                private void Test()
                {
                    // Simple property assignments are fine
                    var simple = Mock.Of<IRepository>(r => r.IsAuthenticated == true);

                    // ANALYZER WARNING: Complex expressions with method calls
                    {|Moq1600:Mock.Of<IRepository>(r => 
                        r.IsAuthenticated == true && 
                        r.GetData(It.IsAny<int>()) == "test")|};

                    // ANALYZER WARNING: Nested Mock.Of patterns  
                    {|Moq1600:Mock.Of<IRepository>(r =>
                        r.GetData(1) == Mock.Of<IRepository>(inner => inner.IsAuthenticated == true).GetData(2))|};
                }
            }
            """;

        // Now triggers warnings for complex Mock.Of expressions
        await AnalyzerVerifier<MockOfComplexExpressionAnalyzer>.VerifyAnalyzerAsync(source, ReferenceAssemblyCatalog.Net80WithNewMoq);
    }

    /// <summary>
    /// Demonstrates sequence scenarios that could benefit from analyzer guidance.
    /// Currently these pass without warnings, but analyzers could help with:
    /// - Validating sequence setup patterns.
    /// - Warning about potential ordering issues.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task SequenceOpportunities()
    {
        const string source = """
            using Moq;

            public interface IFoo 
            { 
                int GetCount();
                void Process();
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var mock = new Mock<IFoo>();

                    // POTENTIAL ANALYZER OPPORTUNITY: Validate sequence setup correctness
                    // Missing Returns() after SetupSequence might cause issues
                    mock.SetupSequence(f => f.GetCount())
                        .Returns(1)
                        .Returns(2); // Could warn if no final behavior specified

                    // POTENTIAL ANALYZER OPPORTUNITY: Detect sequence ordering issues
                    var sequence = new MockSequence();
                    mock.InSequence(sequence).Setup(x => x.Process());
                    // Setting up the same method again might break the sequence
                    mock.Setup(x => x.Process()); // Could conflict with sequence
                }
            }
            """;

        await AnalyzerVerifier<SetupShouldBeUsedOnlyForOverridableMembersAnalyzer>.VerifyAnalyzerAsync(source, ReferenceAssemblyCatalog.Net80WithNewMoq);
    }

    /// <summary>
    /// Demonstrates event scenarios that could benefit from analyzer guidance.
    /// Currently these pass without warnings, but analyzers could help with:
    /// - Validating event handler signatures.
    /// - Warning about incorrect event patterns.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task EventOpportunities()
    {
        const string source = """
            using Moq;

            public interface IFoo 
            { 
                event System.EventHandler<string> StringEvent;
                event System.EventHandler BasicEvent;
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var mock = new Mock<IFoo>();

                    // POTENTIAL ANALYZER OPPORTUNITY: Validate event handler signature matching
                    // Correct usage - should match event handler type exactly
                    mock.SetupAdd(m => m.StringEvent += It.IsAny<System.EventHandler<string>>());

                    // POTENTIAL ANALYZER OPPORTUNITY: Guide proper event raising patterns
                    // Correct event argument types for EventHandler<string>
                    mock.Raise(m => m.StringEvent += null, "test data");
                }
            }
            """;

        await AnalyzerVerifier<SetupShouldBeUsedOnlyForOverridableMembersAnalyzer>.VerifyAnalyzerAsync(source, ReferenceAssemblyCatalog.Net80WithNewMoq);
    }

    /// <summary>
    /// Demonstrates MockRepository scenarios that could benefit from analyzer guidance.
    /// Currently these pass without warnings, but analyzers could help with:
    /// - Ensuring repository.Verify() is called.
    /// - Validating consistent repository usage.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task MockRepositoryOpportunities()
    {
        const string source = """
            using Moq;

            public interface IFoo { }

            internal class UnitTest
            {
                private void Test()
                {
                    var repository = new MockRepository(MockBehavior.Strict);
                    var fooMock = repository.Create<IFoo>();
                    
                    // POTENTIAL ANALYZER OPPORTUNITY: Warn about missing repository.Verify()
                    // When using MockRepository, forgetting to call Verify() can miss verification errors
                    // repository.Verify(); // This line is commented out - could be flagged
                }

                private void TestMixed()
                {
                    var repository = new MockRepository(MockBehavior.Strict);
                    var fooMock = repository.Create<IFoo>();
                    
                    // POTENTIAL ANALYZER OPPORTUNITY: Detect mixed mock creation patterns
                    // Mixing repository-created mocks with direct Mock creation might be confusing
                    var directMock = new Mock<IFoo>(); // Different from repository pattern above
                    
                    repository.Verify(); // Only verifies repository mocks, not directMock
                }
            }
            """;

        await AnalyzerVerifier<SetupShouldBeUsedOnlyForOverridableMembersAnalyzer>.VerifyAnalyzerAsync(source, ReferenceAssemblyCatalog.Net80WithNewMoq);
    }
}
