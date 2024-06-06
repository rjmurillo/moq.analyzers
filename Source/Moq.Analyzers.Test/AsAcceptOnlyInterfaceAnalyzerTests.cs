using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Moq.Analyzers.Test.Helpers;
using Xunit;

namespace Moq.Analyzers.Test;

public class AsAcceptOnlyInterfaceAnalyzerTests : DiagnosticVerifier<AsShouldBeUsedOnlyForInterfaceAnalyzer>
{
    // [Fact]
    public Task ShouldFailWhenUsingAsWithAbstractClass()
    {
        return Verify(VerifyCSharpDiagnostic(
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
                        mock.As<BaseSampleClass>();
                    }
                }
                """
            ));
    }

    // [Fact]
    public Task ShouldFailWhenUsingAsWithConcreteClass()
    {
        return Verify(VerifyCSharpDiagnostic(
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
                        mock.As<OtherClass>();
                    }
                }
                """
            ));
    }

    // [Fact]
    public Task ShouldPassWhenUsingAsWithInterface()
    {
        return Verify(VerifyCSharpDiagnostic(
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
                """
            ));
    }

    // [Fact]
    public Task ShouldPassWhenUsingAsWithInterfaceWithSetup()
    {
        return Verify(VerifyCSharpDiagnostic(
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
                """
            ));
    }
}
