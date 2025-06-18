using Moq.Analyzers.Test.Helpers;
using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.InSequenceSetupShouldBeProperlyConfiguredAnalyzer>;

namespace Moq.Analyzers.Test;

public class InSequenceSetupShouldBeProperlyConfiguredAnalyzerTests
{
    public static IEnumerable<object[]> TestData()
    {
        // Test cases that should NOT produce diagnostics (valid InSequence usage)
        IEnumerable<object[]> validUsage = new object[][]
        {
            // Valid InSequence with MockSequence followed by Setup
            ["""
             var sequence = new MockSequence();
             var mock = new Mock<IService>();
             mock.InSequence(sequence).Setup(x => x.DoSomething());
             """],

            // Multiple InSequence calls with same sequence
            ["""
             var sequence = new MockSequence();
             var mock1 = new Mock<IService>();
             var mock2 = new Mock<IService>();
             mock1.InSequence(sequence).Setup(x => x.DoSomething());
             mock2.InSequence(sequence).Setup(x => x.DoOtherThing());
             """],

            // InSequence with variable
            ["""
             var sequence = new MockSequence();
             var mock = new Mock<IService>();
             var inSequenceMock = mock.InSequence(sequence);
             inSequenceMock.Setup(x => x.DoSomething());
             """],

            // InSequence with null parameter - currently not triggering analyzer
            ["""
             var mock = new Mock<IService>();
             MockSequence? sequence = null;
             mock.InSequence(sequence!).Setup(x => x.DoSomething());
             """],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();

        return validUsage;
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldAnalyzeInSequenceSetup(string referenceAssemblyGroup, string @namespace, string mock)
    {
        string source =
            $$"""
              {{@namespace}}

              public interface IService
              {
                  void DoSomething();
                  void DoOtherThing();
              }

              internal class TestClass
              {
                  private void Test()
                  {
                      {{mock}}
                  }
              }
              """;

        await Verifier.VerifyAnalyzerAsync(source, referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(DoppelgangerTestHelper.GetAllCustomMockData), MemberType = typeof(DoppelgangerTestHelper))]
    public async Task ShouldPassIfCustomMockClassIsUsed(string mockCode)
    {
        string referenceAssemblyGroup = Helpers.ReferenceAssemblyCatalog.Net80WithNewMoq;
        string source = DoppelgangerTestHelper.CreateTestCode(mockCode);
        await Verifier.VerifyAnalyzerAsync(source, referenceAssemblyGroup);
    }

    [Fact]
    public async Task ShouldNotTriggerOnNonInSequenceMethods()
    {
        string source = """
            using Moq;

            public interface IService
            {
                void DoSomething();
            }

            internal class TestClass
            {
                private void Test()
                {
                    var mock = new Mock<IService>();
                    mock.Setup(x => x.DoSomething()); // Not InSequence, should not trigger
                }
            }
            """;

        await Verifier.VerifyAnalyzerAsync(source, ReferenceAssemblyCatalog.Net80WithNewMoq);
    }

    [Fact]
    public async Task ShouldHandleInSequenceWithObjectParameter()
    {
        string source = """
            using Moq;

            public interface IService
            {
                void DoSomething();
            }

            internal class TestClass
            {
                private void Test()
                {
                    var mock = new Mock<IService>();
                    MockSequence? sequence = null;
                    mock.InSequence(sequence!).Setup(x => x.DoSomething());
                }
            }
            """;

        await Verifier.VerifyAnalyzerAsync(source, ReferenceAssemblyCatalog.Net80WithNewMoq);
    }
}
