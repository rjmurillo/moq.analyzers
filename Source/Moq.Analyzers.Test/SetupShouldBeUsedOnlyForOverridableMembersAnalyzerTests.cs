namespace Moq.Analyzers.Test;

public class SetupShouldBeUsedOnlyForOverridableMembersAnalyzerTests : DiagnosticVerifier<SetupShouldBeUsedOnlyForOverridableMembersAnalyzer>
{
    [Fact]
    public async Task ShouldFailWhenSetupIsCalledWithANonVirtualMethod()
    {
        await VerifyCSharpDiagnostic(
                """
                using Moq;

                namespace SetupOnlyForOverridableMembers.TestBadSetupForNonVirtualMethod;

                public abstract class BaseSampleClass
                {
                    public int Calculate() => 0;

                    public abstract int Calculate(int a, int b);

                    public abstract int Calculate(int a, int b, int c);
                }

                internal class MyUnitTests
                {
                    private void TestBadSetupForNonVirtualMethod()
                    {
                        var mock = new Mock<BaseSampleClass>();
                        mock.Setup(x => {|Moq1200:x.Calculate()|});
                    }
                }
                """);
    }

    [Fact]
    public async Task ShouldFailWhenSetupIsCalledWithANonVirtualProperty()
    {
        await VerifyCSharpDiagnostic(
                """
                using Moq;

                namespace SetupOnlyForOverridableMembers.TestBadSetupForNonVirtualProperty;

                public class SampleClass
                {

                    public int Property { get; set; }
                }

                internal class MyUnitTests
                {
                    private void TestBadSetupForNonVirtualProperty()
                    {
                        var mock = new Mock<SampleClass>();
                        mock.Setup(x => {|Moq1200:x.Property|});
                    }
                }
                """);
    }

    [Fact]
    public async Task ShouldFailWhenSetupIsCalledWithASealedMethod()
    {
        await VerifyCSharpDiagnostic(
                """
                using Moq;

                namespace SetupOnlyForOverridableMembers.TestBadSetupForSealedMethod;

                public abstract class BaseSampleClass
                {
                    public int Calculate() => 0;

                    public abstract int Calculate(int a, int b);

                    public abstract int Calculate(int a, int b, int c);
                }

                public class SampleClass : BaseSampleClass
                {

                    public override int Calculate(int a, int b) => 0;

                    public sealed override int Calculate(int a, int b, int c) => 0;
                }

                internal class MyUnitTests
                {
                    private void TestBadSetupForSealedMethod()
                    {
                        var mock = new Mock<SampleClass>();
                        mock.Setup(x => {|Moq1200:x.Calculate(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>())|});
                    }
                }
                """);
    }

    [Fact]
    public async Task ShouldPassWhenSetupIsCalledWithAnAbstractMethod()
    {
        await VerifyCSharpDiagnostic(
                """
                using Moq;

                namespace SetupOnlyForOverridableMembers.TestOkForAbstractMethod;

                public abstract class BaseSampleClass
                {
                    public int Calculate() => 0;

                    public abstract int Calculate(int a, int b);

                    public abstract int Calculate(int a, int b, int c);
                }

                internal class MyUnitTests
                {
                    private void TestOkForAbstractMethod()
                    {
                        var mock = new Mock<BaseSampleClass>();
                        mock.Setup(x => x.Calculate(It.IsAny<int>(), It.IsAny<int>()));
                    }
                }
                """);
    }

    [Fact]
    public async Task ShouldPassWhenSetupIsCalledWithAnInterfaceMethod()
    {
        await VerifyCSharpDiagnostic(
                """
                using Moq;

                namespace SetupOnlyForOverridableMembers.TestOkForInterfaceMethod;

                public interface ISampleInterface
                {
                    int Calculate(int a, int b);
                }

                internal class MyUnitTests
                {
                    private void TestOkForInterfaceMethod()
                    {
                        var mock = new Mock<ISampleInterface>();
                        mock.Setup(x => x.Calculate(It.IsAny<int>(), It.IsAny<int>()));
                    }
                }
                """);
    }

    [Fact]
    public async Task ShouldPassWhenSetupIsCalledWithAnInterfaceProperty()
    {
        await VerifyCSharpDiagnostic(
                """
                using Moq;

                namespace SetupOnlyForOverridableMembers.TestOkForInterfaceProperty;

                public interface ISampleInterface
                {
                    int TestProperty { get; set; }
                }

                internal class MyUnitTests
                {
                    private void TestOkForInterfaceProperty()
                    {
                        var mock = new Mock<ISampleInterface>();
                        mock.Setup(x => x.TestProperty);
                    }
                }
                """);
    }

    [Fact]
    public async Task ShouldPassWhenSetupIsCalledWithAnOverrideOfAnAbstractMethod()
    {
        await VerifyCSharpDiagnostic(
                """
                using Moq;

                namespace SetupOnlyForOverridableMembers.TestOkForOverrideAbstractMethod;

                public abstract class BaseSampleClass
                {
                    public int Calculate() => 0;

                    public abstract int Calculate(int a, int b);

                    public abstract int Calculate(int a, int b, int c);
                }

                public class SampleClass : BaseSampleClass
                {

                    public override int Calculate(int a, int b) => 0;

                    public sealed override int Calculate(int a, int b, int c) => 0;
                }

                internal class MyUnitTests
                {
                    private void TestOkForOverrideAbstractMethod()
                    {
                        var mock = new Mock<SampleClass>();
                        mock.Setup(x => x.Calculate(It.IsAny<int>(), It.IsAny<int>()));
                    }
                }
                """);
    }

    [Fact]
    public async Task ShouldPassWhenSetupIsCalledWithAVirtualMethod()
    {
        await VerifyCSharpDiagnostic(
                """
                using Moq;

                namespace SetupOnlyForOverridableMembers.TestOkForVirtualMethod;

                public class SampleClass
                {
                    public virtual int DoSth() => 0;
                }

                internal class MyUnitTests
                {
                    private void TestOkForVirtualMethod()
                    {
                        var mock = new Mock<SampleClass>();
                        mock.Setup(x => x.DoSth());
                    }
                }
                """);
    }
}
