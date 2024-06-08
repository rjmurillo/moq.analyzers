using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.ConstructorArgumentsShouldMatchAnalyzer>;

namespace Moq.Analyzers.Test;

public class AbstractClassTests
{
    public static IEnumerable<object[]> TestData()
    {
        foreach (string @namespace in new[] { string.Empty, "namespace MyNamespace;" })
        {
            yield return
            [
                @namespace, """new Mock<AbstractGenericClassWithCtor<object>>{|Moq1002:("42")|}; // The class has a constructor that takes an Int32 but passes a string""",
            ];

            yield return
            [
                @namespace, """new Mock<AbstractGenericClassWithCtor<object>>{|Moq1002:("42", 42)|}; // The class has a ctor with two arguments [Int32, String], but they are passed in reverse order""",
            ];

            yield return
            [
                @namespace, """new Mock<AbstractGenericClassDefaultCtor<object>>{|Moq1002:(42)|}; // The class has a ctor but does not take any arguments""",
            ];

            yield return
            [
                // TODO: Review use of `.As<>()` in this test case. It is not clear what purpose it serves.
                @namespace, """new Mock<AbstractGenericClassDefaultCtor<object>>().As<AbstractGenericClassDefaultCtor<object>>();""",
            ];

            yield return
            [
                @namespace, """new Mock<AbstractGenericClassDefaultCtor<object>>();""",
            ];

            yield return
            [
                @namespace, """new Mock<AbstractGenericClassDefaultCtor<object>>(MockBehavior.Default);""",
            ];

            // TODO: "I think this _should_ fail, but currently passes. Tracked by #55."
            // yield return
            // [
            //     // TODO: Review use of `.As<>()` in this test case. It is not clear what purpose it serves.
            //     @namespace, """new Mock<AbstractClassWithCtor>().As<AbstractClassWithCtor>();""",
            // ];

            yield return
            [
                @namespace, """new Mock<AbstractClassWithCtor>{|Moq1002:("42")|}; // The class has a ctor that takes an Int32 but passes a String""",
            ];

            yield return
            [
                @namespace, """new Mock<AbstractClassWithCtor>{|Moq1002:("42", 42)|}; // The class has a ctor with two arguments [Int32, String], but they are passed in reverse order""",
            ];

            yield return
            [
                @namespace, """new Mock<AbstractClassDefaultCtor>{|Moq1002:(42)|}; // The class has a ctor but does not take any arguments""",
            ];

            yield return
            [
                // TODO: Review use of `.As<>()` in this test case. It is not clear what purpose it serves.
                @namespace, """new Mock<AbstractClassDefaultCtor>().As<AbstractClassDefaultCtor>();""",
            ];

            yield return
            [
                @namespace, """new Mock<AbstractClassWithCtor>(42);""",
            ];

            yield return
            [
                @namespace,
                """new Mock<AbstractClassWithCtor>(MockBehavior.Default, 42);""",
            ];

            yield return
            [
                @namespace, """new Mock<AbstractClassWithCtor>(42, "42");""",
            ];

            yield return
            [
                @namespace, """new Mock<AbstractClassWithCtor>(MockBehavior.Default, 42, "42");""",
            ];

            yield return
            [
                @namespace, """new Mock<AbstractGenericClassWithCtor<object>>(42);""",
            ];

            yield return
            [
                @namespace, """new Mock<AbstractGenericClassWithCtor<object>>(MockBehavior.Default, 42);""",
            ];
        }
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldAnalyzeAbstractClasses(string @namespace, string mock)
    {
        await Verifier.VerifyAnalyzerAsync(
                $$"""
                {{@namespace}}

                internal abstract class AbstractClassDefaultCtor
                {
                    protected AbstractClassDefaultCtor()
                    {
                    }
                }

                internal abstract class AbstractGenericClassDefaultCtor<T>
                {
                    protected AbstractGenericClassDefaultCtor()
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

                internal abstract class AbstractGenericClassWithCtor<T>
                {
                    protected AbstractGenericClassWithCtor(int a)
                    {
                    }

                    protected AbstractGenericClassWithCtor(int a, string b)
                    {
                    }
                }

                internal class UnitTest
                {
                    private void Test()
                    {
                        {{mock}}
                    }
                }
                """);
    }
}
