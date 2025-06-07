using Moq.Analyzers.Test.Helpers;
using Xunit;

namespace Moq.Analyzers.Test;

/// <summary>
/// Tests to validate scenarios from the Moq quickstart are properly handled by analyzers.
/// This ensures coverage of patterns customers would immediately encounter from the official documentation.
/// The current tests verify that these patterns compile without triggering existing analyzer warnings,
/// serving as a baseline for identifying gaps in analyzer coverage.
/// </summary>
public class MoqQuickstartScenarioTests
{
    /// <summary>
    /// Test basic event setup scenarios from the Moq quickstart that should not trigger warnings.
    /// These patterns currently have NO dedicated analyzer coverage.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task ShouldNotFlagValidEventSetupPatterns()
    {
        const string source = """
            using Moq;

            public interface IFoo 
            { 
                event System.EventHandler FooEvent; 
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var mock = new Mock<IFoo>();
                    mock.SetupAdd(m => m.FooEvent += It.IsAny<System.EventHandler>());
                    mock.SetupRemove(m => m.FooEvent -= It.IsAny<System.EventHandler>());
                    mock.Raise(m => m.FooEvent += null, System.EventArgs.Empty);
                }
            }
            """;

        // Use an existing analyzer to verify the code compiles and doesn't trigger basic warnings
        await AnalyzerVerifier<SetupShouldBeUsedOnlyForOverridableMembersAnalyzer>.VerifyAnalyzerAsync(source, ReferenceAssemblyCatalog.Net80WithNewMoq);
    }

    /// <summary>
    /// Test basic verification scenarios from the Moq quickstart that should not trigger warnings.
    /// These patterns currently have NO dedicated analyzer coverage.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task ShouldNotFlagValidVerificationPatterns()
    {
        const string source = """
            using Moq;

            public interface IFoo 
            { 
                bool DoSomething(string value); 
                string Name { get; set; }
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var mock = new Mock<IFoo>();
                    mock.Verify(foo => foo.DoSomething("ping"));
                    mock.Verify(foo => foo.DoSomething("ping"), Times.Never());
                    mock.VerifyGet(foo => foo.Name);
                    mock.VerifySet(foo => foo.Name = "foo");
                    mock.VerifyNoOtherCalls();
                }
            }
            """;

        await AnalyzerVerifier<SetupShouldBeUsedOnlyForOverridableMembersAnalyzer>.VerifyAnalyzerAsync(source, ReferenceAssemblyCatalog.Net80WithNewMoq);
    }

    /// <summary>
    /// Test LINQ to Mocks scenarios from the Moq quickstart that should not trigger warnings.
    /// These patterns currently have NO dedicated analyzer coverage.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task ShouldNotFlagValidLinqToMocksPatterns()
    {
        const string source = """
            using Moq;

            public interface IRepository 
            { 
                bool IsAuthenticated { get; } 
            }

            public interface IServiceProvider 
            { 
                object GetService(System.Type serviceType); 
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var repo = Mock.Of<IRepository>(r => r.IsAuthenticated == true);
                    var services = Mock.Of<IServiceProvider>(sp =>
                        sp.GetService(typeof(IRepository)) == Mock.Of<IRepository>(r => r.IsAuthenticated == true));
                }
            }
            """;

        await AnalyzerVerifier<SetupShouldBeUsedOnlyForOverridableMembersAnalyzer>.VerifyAnalyzerAsync(source, ReferenceAssemblyCatalog.Net80WithNewMoq);
    }

    /// <summary>
    /// Test sequence scenarios from the Moq quickstart that should not trigger warnings.
    /// These patterns currently have NO dedicated analyzer coverage.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task ShouldNotFlagValidSequencePatterns()
    {
        const string source = """
            using Moq;

            public interface IFoo 
            { 
                int GetCount(); 
                void FooMethod(); 
            }

            public interface IBar 
            { 
                void BarMethod(); 
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var mock = new Mock<IFoo>();
                    mock.SetupSequence(f => f.GetCount())
                        .Returns(3)
                        .Returns(2)
                        .Returns(1)
                        .Throws(new System.InvalidOperationException());

                    var fooMock = new Mock<IFoo>(MockBehavior.Strict);
                    var barMock = new Mock<IBar>(MockBehavior.Strict);
                    var sequence = new MockSequence();
                    fooMock.InSequence(sequence).Setup(x => x.FooMethod());
                    barMock.InSequence(sequence).Setup(x => x.BarMethod());
                }
            }
            """;

        await AnalyzerVerifier<SetupShouldBeUsedOnlyForOverridableMembersAnalyzer>.VerifyAnalyzerAsync(source, ReferenceAssemblyCatalog.Net80WithNewMoq);
    }

    /// <summary>
    /// Test MockRepository scenarios from the Moq quickstart that should not trigger warnings.
    /// These patterns currently have NO dedicated analyzer coverage.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task ShouldNotFlagValidMockRepositoryPatterns()
    {
        const string source = """
            using Moq;

            public interface IFoo { }
            public interface IBar { }

            internal class UnitTest
            {
                private void Test()
                {
                    var repository = new MockRepository(MockBehavior.Strict) { DefaultValue = DefaultValue.Mock };
                    var fooMock = repository.Create<IFoo>();
                    var barMock = repository.Create<IBar>(MockBehavior.Loose);
                    repository.Verify();
                }
            }
            """;

        await AnalyzerVerifier<SetupShouldBeUsedOnlyForOverridableMembersAnalyzer>.VerifyAnalyzerAsync(source, ReferenceAssemblyCatalog.Net80WithNewMoq);
    }

    /// <summary>
    /// Test advanced callback scenarios from the Moq quickstart to ensure existing coverage works.
    /// These patterns should be covered by existing analyzers.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    [Fact]
    public async Task ShouldNotFlagValidCallbackPatterns()
    {
        const string source = """
            using Moq;

            public interface IFoo 
            { 
                bool DoSomething(string value); 
                bool DoSomething(int number, string value); 
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var mock = new Mock<IFoo>();
                    var calls = 0;
                    var callArgs = new System.Collections.Generic.List<string>();

                    // Basic callback
                    mock.Setup(foo => foo.DoSomething("ping"))
                        .Callback(() => calls++)
                        .Returns(true);

                    // Callback with parameter access
                    mock.Setup(foo => foo.DoSomething(It.IsAny<string>()))
                        .Callback((string s) => callArgs.Add(s))
                        .Returns(true);

                    // Multi-parameter callback
                    mock.Setup(foo => foo.DoSomething(It.IsAny<int>(), It.IsAny<string>()))
                        .Callback<int, string>((i, s) => callArgs.Add(s))
                        .Returns(true);
                }
            }
            """;

        await AnalyzerVerifier<CallbackSignatureShouldMatchMockedMethodAnalyzer>.VerifyAnalyzerAsync(source, ReferenceAssemblyCatalog.Net80WithNewMoq);
    }
}
