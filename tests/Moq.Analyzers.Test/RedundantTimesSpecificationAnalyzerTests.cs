using Moq.Analyzers.Test.Helpers;
using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.RedundantTimesSpecificationAnalyzer>;

namespace Moq.Analyzers.Test;

public class RedundantTimesSpecificationAnalyzerTests(ITestOutputHelper output)
{
    public static IEnumerable<object[]> TestData()
    {
        IEnumerable<object[]> both = new object[][]
        {
            // Should detect redundant Times.AtLeastOnce()
            ["new Mock<ISampleInterface>().Verify(x => x.TestMethod(), {|Moq1420:Times.AtLeastOnce()|});"],

            // Should NOT detect redundant for Times.AtMostOnce()
            ["new Mock<ISampleInterface>().Verify(x => x.TestMethod(), Times.AtMostOnce());"],

            // Should NOT detect redundant for Times.AtLeast(1)
            ["new Mock<ISampleInterface>().Verify(x => x.TestMethod(), Times.AtLeast(1));"],

            // Should NOT detect redundant for Times.AtMost(1)
            ["new Mock<ISampleInterface>().Verify(x => x.TestMethod(), Times.AtMost(1));"],

            // Should NOT detect other Times specifications
            ["new Mock<ISampleInterface>().Verify(x => x.TestMethod(), Times.Never());"],
            ["new Mock<ISampleInterface>().Verify(x => x.TestMethod(), Times.Once());"],
            ["new Mock<ISampleInterface>().Verify(x => x.TestMethod(), Times.Exactly(3));"],

            // Should NOT detect variable/expression-based Times
            ["int times = 2; new Mock<ISampleInterface>().Verify(x => x.TestMethod(), Times.Exactly(times));"],
            ["int times = 2; new Mock<ISampleInterface>().Verify(x => x.TestMethod(), Times.AtLeast(times));"],

            // Should NOT detect Verify calls without Times parameter (default behavior)
            ["new Mock<ISampleInterface>().Verify(x => x.TestMethod());"],

            // Should detect redundant Times.AtLeastOnce() in VerifyGet
            ["new Mock<ISampleInterface>().VerifyGet(x => x.TestProperty, {|Moq1420:Times.AtLeastOnce()|});"],

            // Should NOT detect redundant for Times.AtMostOnce() in VerifyGet
            ["new Mock<ISampleInterface>().VerifyGet(x => x.TestProperty, Times.AtMostOnce());"],

            // Should NOT detect other Times in VerifyGet
            ["new Mock<ISampleInterface>().VerifyGet(x => x.TestProperty, Times.Never());"],
            ["new Mock<ISampleInterface>().VerifyGet(x => x.TestProperty);"],

            // Should NOT detect variable/expression-based Times in VerifyGet
            ["int times = 2; new Mock<ISampleInterface>().VerifyGet(x => x.TestProperty, Times.Exactly(times));"],

            // Should detect redundant Times with message argument
            ["new Mock<ISampleInterface>().Verify(x => x.TestMethod(), {|Moq1420:Times.AtLeastOnce()|}, \"should be called at least once\");"],
            ["new Mock<ISampleInterface>().VerifyGet(x => x.TestProperty, {|Moq1420:Times.AtLeastOnce()|}, \"should be called at least once\");"],

        }.WithNamespaces().WithMoqReferenceAssemblyGroups();

        IEnumerable<object[]> newMoqOnly = new object[][]
        {
            // Should detect redundant Times.AtLeastOnce() in VerifySet (only available in new Moq versions)
            ["new Mock<ISampleInterface>().VerifySet(x => { x.TestProperty = It.IsAny<string>(); }, {|Moq1420:Times.AtLeastOnce()|});"],

            // Should NOT detect redundant for Times.AtMostOnce() in VerifySet
            ["new Mock<ISampleInterface>().VerifySet(x => { x.TestProperty = It.IsAny<string>(); }, Times.AtMostOnce());"],

            // Should NOT detect other Times in VerifySet
            ["new Mock<ISampleInterface>().VerifySet(x => { x.TestProperty = It.IsAny<string>(); }, Times.Never());"],
            ["new Mock<ISampleInterface>().VerifySet(x => { x.TestProperty = It.IsAny<string>(); });"],

            // Should NOT detect variable/expression-based Times in VerifySet
            ["int times = 2; new Mock<ISampleInterface>().VerifySet(x => { x.TestProperty = It.IsAny<string>(); }, Times.Exactly(times));"],
        }.WithNamespaces().WithNewMoqReferenceAssemblyGroups();

        return both.Concat(newMoqOnly);
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldAnalyzeTimesUsage(string referenceAssemblyGroup, string @namespace, string sourceCode)
    {
        static string Template(string ns, string code) =>
$$"""
{{ns}}

public interface ISampleInterface
{
    void TestMethod();
    string TestProperty { get; set; }
}

public class TestClass
{
    public void TestMethod()
    {
        {{code}}
    }
}
""";

        string source = Template(@namespace, sourceCode);
        output.WriteLine(source);
        await Verifier.VerifyAnalyzerAsync(source, referenceAssemblyGroup);
    }

    [Fact]
    public async Task ShouldNotTriggerForNonMoqMethods()
    {
        const string source = """
namespace Test
{
    public interface INotMoq
    {
        void Verify();
    }

    public class TestClass
    {
        public void TestMethod()
        {
            var notMoq = new NotMoqImpl();
            notMoq.Verify();
        }
    }

    public class NotMoqImpl : INotMoq
    {
        public void Verify() { }
    }
}
""";

        await Verifier.VerifyAnalyzerAsync(source, ReferenceAssemblyCatalog.Net80WithNewMoq);
    }
}
