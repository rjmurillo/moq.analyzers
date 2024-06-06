namespace Moq.Analyzers.Test;

public class SetupShouldBeUsedOnlyForOverridableMembersAnalyzerTests : DiagnosticVerifier
{
    [Fact]
    public Task ShouldFailWhenSetupIsCalledWithANonVirtualMethod()
    {
        return Verify(VerifyCSharpDiagnostic(
            [
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
                        mock.Setup(x => x.Calculate());
                    }
                }
                """
            ]));
    }

    [Fact]
    public Task ShouldFailWhenSetupIsCalledWithANonVirtualProperty()
    {
        return Verify(VerifyCSharpDiagnostic(
            [
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
                        mock.Setup(x => x.Property);
                    }
                }
                """
            ]));
    }

    [Fact]
    public Task ShouldFailWhenSetupIsCalledWithASealedMethod()
    {
        return Verify(VerifyCSharpDiagnostic(
            [
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
                        mock.Setup(x => x.Calculate(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()));
                    }
                }
                """
            ]));
    }

    [Fact]
    public Task ShouldPassWhenSetupIsCalledWithAnAbstractMethod()
    {
        return Verify(VerifyCSharpDiagnostic(
            [
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
                """
            ]));
    }

    [Fact]
    public Task ShouldPassWhenSetupIsCalledWithAnInterfaceMethod()
    {
        return Verify(VerifyCSharpDiagnostic(
            [
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
                """
            ]));
    }

    [Fact]
    public Task ShouldPassWhenSetupIsCalledWithAnInterfaceProperty()
    {
        return Verify(VerifyCSharpDiagnostic(
            [
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
                """
            ]));
    }

    [Fact]
    public Task ShouldPassWhenSetupIsCalledWithAnOverrideOfAnAbstractMethod()
    {
        return Verify(VerifyCSharpDiagnostic(
            [
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
                """
            ]));
    }

    [Fact]
    public Task ShouldPassWhenSetupIsCalledWithAVirtualMethod()
    {
        return Verify(VerifyCSharpDiagnostic(
            [
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
                """
            ]));
    }

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        return new SetupShouldBeUsedOnlyForOverridableMembersAnalyzer();
    }
}
