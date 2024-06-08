using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.NoMethodsInPropertySetupAnalyzer>;

namespace Moq.Analyzers.Test;

public class NoMethodsInPropertySetupAnalyzerTests
{
    [Fact]
    public async Task ShouldPassWhenPropertiesUsePropertySetup()
    {
        await Verifier.VerifyAnalyzerAsync(
                """
                namespace NoMethodsInPropertySetup.Good;

                public interface IFoo
                {
                    string Prop1 { get; set; }

                    string Prop2 { get; }

                    string Prop3 { set; }

                    string Method();
                }

                public class MyUnitTests
                {
                    private void TestGood()
                    {
                        var mock = new Mock<IFoo>();
                        mock.SetupGet(x => x.Prop1);
                        mock.SetupGet(x => x.Prop2);
                        mock.SetupSet(x => x.Prop1 = "1");
                        mock.SetupSet(x => x.Prop3 = "2");
                        mock.Setup(x => x.Method());
                    }
                }
                """);
    }

    [Fact]
    public async Task ShouldFailWhenMethodsUsePropertySetup()
    {
        await Verifier.VerifyAnalyzerAsync(
                """
                namespace NoMethodsInPropertySetup.Bad;

                public interface IFoo
                {
                    string Prop1 { get; set; }

                    string Prop2 { get; }

                    string Prop3 { set; }

                    string Method();
                }

                public class MyUnitTests
                {
                    private void TestBad()
                    {
                        var mock = new Mock<IFoo>();
                        mock.SetupGet(x => {|Moq1101:x.Method()|});
                        mock.SetupSet(x => {|Moq1101:x.Method()|});
                    }
                }
                """);
    }
}
