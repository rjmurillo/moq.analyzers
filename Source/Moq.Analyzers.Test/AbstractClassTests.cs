using System.IO;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace Moq.Analyzers.Test;

public class AbstractClassTests : DiagnosticVerifier
{
    // TODO: Review use of `.As<>()` in the test cases. It is not clear what purpose it serves.
    // [Fact]
    public Task ShouldFailOnGenericTypesWithMismatchArgs()
    {
        return Verify(VerifyCSharpDiagnostic(
            [
                """
                namespace Moq.Analyzers.Test.Data.AbstractClass.GenericMistmatchArgs;

                internal abstract class AbstractGenericClassDefaultCtor<T>
                {
                    protected AbstractGenericClassDefaultCtor()
                    {
                    }
                }

                internal abstract class AbstractGenericClassWithCtor<T>
                {
                    protected AbstractGenericClassWithCtor(int a)
                    {
                    }

                    protected AbstractGenericClassWithCtor(int a, string b)
                    {
                    }
                }

                internal class MyUnitTests
                {
                    private void TestBadWithGeneric()
                    {
                        // The class has a constructor that takes an Int32 but passes a String
                        var mock = new Mock<AbstractGenericClassWithCtor<object>>("42");

                        // The class has a ctor with two arguments [Int32, String], but they are passed in reverse order
                        var mock1 = new Mock<AbstractGenericClassWithCtor<object>>("42", 42);

                        // The class has a ctor but does not take any arguments
                        var mock2 = new Mock<AbstractGenericClassDefaultCtor<object>>(42);
                    }
                }
                """
            ]));
    }

    // [Fact]
    public Task ShouldPassOnGenericTypesWithNoArgs()
    {
        return Verify(VerifyCSharpDiagnostic(
            [
                """
                namespace Moq.Analyzers.Test.Data.AbstractClass.GenericNoArgs;

                internal abstract class AbstractGenericClassDefaultCtor<T>
                {
                    protected AbstractGenericClassDefaultCtor()
                    {
                    }
                }

                internal class MyUnitTests
                {
                    private void TestForBaseGenericNoArgs()
                    {
                        var mock = new Mock<AbstractGenericClassDefaultCtor<object>>();
                        mock.As<AbstractGenericClassDefaultCtor<object>>();

                        var mock1 = new Mock<AbstractGenericClassDefaultCtor<object>>();

                        var mock2 = new Mock<AbstractGenericClassDefaultCtor<object>>(MockBehavior.Default);
                    }
                }
                """
            ]));
    }

    // [Fact]
    public Task ShouldFailOnMismatchArgs()
    {
        return Verify(VerifyCSharpDiagnostic(
            [
                """
                namespace Moq.Analyzers.Test.Data.AbstractClass.MismatchArgs;

                internal abstract class AbstractClassDefaultCtor
                {
                    protected AbstractClassDefaultCtor()
                    {
                    }
                }

                internal abstract class AbstractClassWithCtor
                {
                    protected AbstractClassWithCtor(int a)
                    {
                    }

                    protected AbstractClassWithCtor(int a, string b)
                    {
                    }
                }

                internal class MyUnitTests
                {
                    private void TestBad()
                    {
                        // The class has a ctor that takes an Int32 but passes a String
                        var mock = new Mock<AbstractClassWithCtor>("42");

                        // The class has a ctor with two arguments [Int32, String], but they are passed in reverse order
                        var mock1 = new Mock<AbstractClassWithCtor>("42", 42);

                        // The class has a ctor but does not take any arguments
                        var mock2 = new Mock<AbstractClassDefaultCtor>(42);
                    }
                }
                """
            ]));
    }

    // [Fact]
    public Task ShouldPassWithNoArgs()
    {
        return Verify(VerifyCSharpDiagnostic(
            [
                """
                namespace Moq.Analyzers.Test.Data.AbstractClass.NoArgs;

                internal abstract class AbstractClassDefaultCtor
                {
                    protected AbstractClassDefaultCtor()
                    {
                    }
                }

                internal class MyUnitTests
                {
                    // Base case that we can handle abstract types
                    private void TestForBaseNoArgs()
                    {
                        var mock = new Mock<AbstractClassDefaultCtor>();
                        mock.As<AbstractClassDefaultCtor>();
                    }
                }
                """
            ]));
    }

    // [Fact(Skip = "I think this _should_ fail, but currently passes. Tracked by #55.")]
    public Task ShouldFailWithArgsNonePassed()
    {
        return Verify(VerifyCSharpDiagnostic(
            [
                """
                namespace Moq.Analyzers.Test.Data.AbstractClass.WithArgsNonePassed;

                internal abstract class AbstractClassWithCtor
                {
                    protected AbstractClassWithCtor(int a)
                    {
                    }

                    protected AbstractClassWithCtor(int a, string b)
                    {
                    }
                }

                internal class MyUnitTests
                {
                    // This is syntatically not allowed by C#, but you can do it with Moq
                    private void TestForBaseWithArgsNonePassed()
                    {
                        var mock = new Mock<AbstractClassWithCtor>();
                        mock.As<AbstractClassWithCtor>();

                        var mock2 = new Mock<AbstractClassDefaultCtor>(MockBehavior.Default);
                    }
                }
                """
            ]));
    }

    // [Fact]
    public Task ShouldPassWithArgsPassed()
    {
        return Verify(VerifyCSharpDiagnostic(
            [
                """
                namespace Moq.Analyzers.Test.DataAbstractClass.WithArgsPassed;

                internal abstract class AbstractGenericClassWithCtor<T>
                {
                    protected AbstractGenericClassWithCtor(int a)
                    {
                    }

                    protected AbstractGenericClassWithCtor(int a, string b)
                    {
                    }
                }

                internal abstract class AbstractClassWithCtor
                {
                    protected AbstractClassWithCtor(int a)
                    {
                    }

                    protected AbstractClassWithCtor(int a, string b)
                    {
                    }
                }

                internal class MyUnitTests
                {
                    private void TestForBaseWithArgsPassed()
                    {
                        var mock = new Mock<AbstractClassWithCtor>(42);
                        var mock2 = new Mock<AbstractClassWithCtor>(MockBehavior.Default, 42);

                        var mock3 = new Mock<AbstractClassWithCtor>(42, "42");
                        var mock4 = new Mock<AbstractClassWithCtor>(MockBehavior.Default, 42, "42");

                        var mock5 = new Mock<AbstractGenericClassWithCtor<object>>(42);
                        var mock6 = new Mock<AbstractGenericClassWithCtor<object>>(MockBehavior.Default, 42);
                    }
                }
                """
            ]));
    }

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        return new ConstructorArgumentsShouldMatchAnalyzer();
    }
}
