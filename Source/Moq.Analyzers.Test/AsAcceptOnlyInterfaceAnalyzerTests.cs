using Moq.Analyzers.Test.Helpers;

namespace Moq.Analyzers.Test;

public class AsAcceptOnlyInterfaceAnalyzerTests : DiagnosticVerifier<AsShouldBeUsedOnlyForInterfaceAnalyzer>
{
    [Fact]
    public async Task ShouldFailWhenUsingAsWithAbstractClass()
    {
        await VerifyCSharpDiagnostic(
                """
                using Moq;

                namespace AsAcceptOnlyInterface.TestBadAsForAbstractClass;

                public abstract class BaseSampleClass
                {
                    public int Calculate() => 0;
                }

                internal class MyUnitTests
                {
                    private void TestBadAsForAbstractClass()
                    {
                        var mock = new Mock<BaseSampleClass>();
                        mock.As<{|Moq1300:BaseSampleClass|}>();
                    }
                }
                """);
    }

    [Fact]
    public async Task ShouldFailWhenUsingAsWithConcreteClass()
    {
        await VerifyCSharpDiagnostic(
                """
                using Moq;

                namespace AsAcceptOnlyInterface.TestBadAsForNonAbstractClass;

                public interface ISampleInterface
                {
                    int Calculate(int a, int b);
                }

                public abstract class BaseSampleClass
                {
                    public int Calculate() => 0;
                }

                public class OtherClass
                {

                    public int Calculate() => 0;
                }

                internal class MyUnitTests
                {
                    private void TestBadAsForNonAbstractClass()
                    {
                        var mock = new Mock<BaseSampleClass>();
                        mock.As<{|Moq1300:OtherClass|}>();
                    }
                }
                """);
    }

    [Fact]
    public async Task ShouldPassWhenUsingAsWithInterface()
    {
        await VerifyCSharpDiagnostic(
                """
                using Moq;

                namespace AsAcceptOnlyInterface.TestOkAsForInterface;

                public interface ISampleInterface
                {
                    int Calculate(int a, int b);
                }

                public class SampleClass
                {
                    public int Calculate() => 0;
                }

                internal class MyUnitTests
                {
                    private void TestOkAsForInterface()
                    {
                        var mock = new Mock<SampleClass>();
                        mock.As<ISampleInterface>();
                    }
                }
                """);
    }

    [Fact]
    public async Task ShouldPassWhenUsingAsWithInterfaceWithSetup()
    {
        await VerifyCSharpDiagnostic(
                """
                using Moq;

                namespace AsAcceptOnlyInterface.TestOkAsForInterfaceWithConfiguration;

                public interface ISampleInterface
                {
                    int Calculate(int a, int b);
                }

                public class SampleClass
                {
                    public int Calculate() => 0;
                }

                internal class MyUnitTests
                {
                    private void TestOkAsForInterfaceWithConfiguration()
                    {
                        var mock = new Mock<SampleClass>();
                        mock.As<ISampleInterface>()
                            .Setup(x => x.Calculate(It.IsAny<int>(), It.IsAny<int>()))
                            .Returns(10);
                    }
                }
                """);
    }
}
