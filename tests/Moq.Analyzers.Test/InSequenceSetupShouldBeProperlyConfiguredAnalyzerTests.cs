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
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();

        // Test cases that SHOULD produce diagnostics (invalid InSequence usage)
        IEnumerable<object[]> invalidUsage = new object[][]
        {
            // InSequence without proper MockSequence parameter (null) -- this is a compile error, not an analyzer warning
            ["var mock = new Mock<IService>();\nmock.InSequence(null).Setup(x => x.DoSomething());"],

            // InSequence with wrong parameter type -- this is a compile error, not an analyzer warning
            ["var mock = new Mock<IService>();\nmock.InSequence(\"wrong\").Setup(x => x.DoSomething());"],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();

        return validUsage.Concat(invalidUsage);
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

    // Add explicit test for CS1503 error on wrong parameter type
    [Theory]
    [InlineData("var mock = new Mock<IService>();\nmock.InSequence(\"wrong\").Setup(x => x.DoSomething());", 17, 24)]
    public async Task ShouldProduceCompilerErrorForWrongParameterType(string mock, int startCol, int endCol)
    {
        string source = $$"""
namespace MyNamespace;

public interface IService
{
    void DoSomething();
    void DoOtherThing();
}

internal class TestClass
{
    private void Test()
    {
        {mock}
    }
}
""";
        var expected = Microsoft.CodeAnalysis.Testing.DiagnosticResult.CompilerError("CS1503").WithSpan("/0/Test1.cs", 14, startCol, 14, endCol).WithArguments("2", "string", "Moq.MockSequence");
        var test = new Moq.Analyzers.Test.Helpers.Test<Moq.Analyzers.InSequenceSetupShouldBeProperlyConfiguredAnalyzer, Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>
        {
            TestCode = source,
            ReferenceAssemblies = Moq.Analyzers.Test.Helpers.ReferenceAssemblyCatalog.Catalog[Moq.Analyzers.Test.Helpers.ReferenceAssemblyCatalog.Net80WithNewMoq],
            ExpectedDiagnostics = { expected }
        };
        await test.RunAsync();
    }
}
