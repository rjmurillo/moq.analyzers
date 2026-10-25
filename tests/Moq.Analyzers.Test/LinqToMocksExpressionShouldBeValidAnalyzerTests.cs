using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.LinqToMocksExpressionShouldBeValidAnalyzer>;

namespace Moq.Analyzers.Test;

public partial class LinqToMocksExpressionShouldBeValidAnalyzerTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper output = output;

    /// <summary>
    /// Provides both Moq reference assembly versions (4.8.2 and 4.18.4) for <c>[Theory]</c> tests.
    /// All analyzer tests must run against both versions to catch version-specific regressions.
    /// </summary>
    /// <returns>One element per Moq reference assembly group.</returns>
    public static IEnumerable<object[]> MoqReferenceAssemblyGroups()
    {
        yield return [ReferenceAssemblyCatalog.Net100WithOldMoq];
        yield return [ReferenceAssemblyCatalog.Net100WithNewMoq];
    }

    // Only one version of each static data source method
    public static IEnumerable<object[]> EdgeCaseExpressionTestData()
    {
        return new object[][]
        {
            // Existing edge cases
            ["""Mock.Of<IRepository>(null);"""],
            ["""Mock.Of<IRepository>(r => 42 == 42);"""],
            ["""Mock.Of<IRepository>(r => r != null);"""],
            ["""Mock.Of<IRepository>(r => new Func<int>(() => 1)() == 1);"""],
            ["""Mock.Of<IRepository>(r => {|Moq1302:object.Equals(r, null)|});"""],

            // New diverse edge cases

            // Using a conditional operator (valid)
            ["""Mock.Of<IRepository>(r => r.IsAuthenticated ? true : false);"""],

            // Using a coalesce operator (valid)
            ["""Mock.Of<IRepository>(r => (r.Name ?? "default") == "test");"""],

            // Using a cast (valid)
            ["""Mock.Of<IRepository>(r => ((object)r) != null);"""],

            // Using a delegate invocation (valid)
            ["""Mock.Of<IRepository>(r => new System.Func<IRepository, bool>(x => true)(r));"""],

            // Using a discard (valid)
            ["""Mock.Of<IRepository>(_ => true);"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> ValidExpressionTestData()
    {
        return new object[][]
        {
            ["""Mock.Of<IRepository>(r => r.IsAuthenticated == true);"""],
            ["""Mock.Of<IRepository>(r => r.Name == "test");"""],
            ["""Mock.Of<IService>(s => s.GetData() == "result");"""],
            ["""Mock.Of<IService>(s => s.Calculate(1, 2) == 3);"""],
            ["""Mock.Of<BaseClass>(b => b.VirtualProperty == "test");"""],
            ["""Mock.Of<BaseClass>(b => b.VirtualMethod() == true);"""],
            ["""Mock.Of<AbstractClass>(a => a.AbstractProperty == 42);"""],
            ["""Mock.Of<AbstractClass>(a => a.AbstractMethod() == "result");"""],
            ["""Mock.Of<IRepository>(r => true);"""],
            ["""Mock.Of<IRepository>(r => false);"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> InvalidExpressionTestData()
    {
        return new object[][]
        {
            ["""Mock.Of<ConcreteClass>(c => {|Moq1302:c.NonVirtualProperty|} == "test");"""],
            ["""Mock.Of<ConcreteClass>(c => {|Moq1302:c.NonVirtualMethod()|} == true);"""],
            ["""Mock.Of<SealedClass>(s => {|Moq1302:s.Property|} == "value");"""],
            ["""Mock.Of<ConcreteClass>(c => {|Moq1302:c.InstanceMethod()|} == 42);"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> ComplexExpressionTestData()
    {
        return new object[][]
        {
            ["""Mock.Of<ConcreteClass>(c => {|Moq1302:c.Field|} == 5);"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(EdgeCaseExpressionTestData))]
    public async Task ShouldHandleEdgeCaseLinqToMocksExpressions(string referenceAssemblyGroup, string @namespace, string mockExpression)
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
              {{@namespace}}

              public interface IRepository
              {
                  bool IsAuthenticated { get; set; }
                  string Name { get; set; }
              }

              internal class UnitTest
              {
                  private void Test()
                  {
                      var mock = {{mockExpression}}
                  }
              }
              """,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(ValidExpressionTestData))]
    public async Task ShouldNotReportDiagnosticForValidLinqToMocksExpressions(string referenceAssemblyGroup, string @namespace, string mockExpression)
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
              {{@namespace}}

              public interface IRepository
              {
                  bool IsAuthenticated { get; set; }
                  string Name { get; set; }
              }

              public interface IService
              {
                  string GetData();
                  int Calculate(int a, int b);
              }

              public class BaseClass
              {
                  public virtual string VirtualProperty { get; set; }
                  public virtual bool VirtualMethod() => true;
              }

              public abstract class AbstractClass
              {
                  public abstract int AbstractProperty { get; set; }
                  public abstract string AbstractMethod();
              }

              internal class UnitTest
              {
                  private void Test()
                  {
                      var mock = {{mockExpression}}
                  }
              }
              """,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(InvalidExpressionTestData))]
    public async Task ShouldReportDiagnosticForInvalidLinqToMocksExpressions(string referenceAssemblyGroup, string @namespace, string mockExpression)
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
              {{@namespace}}

              public class ConcreteClass
              {
                  public string NonVirtualProperty { get; set; }
                  public bool NonVirtualMethod() => true;
                  public int InstanceMethod() => 42;
              }

              public sealed class SealedClass
              {
                  public string Property { get; set; }
              }

              internal class UnitTest
              {
                  private void Test()
                  {
                      var mock = {{mockExpression}}
                  }
              }
              """,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(ComplexExpressionTestData))]
    public async Task ShouldReportDiagnosticForComplexLinqToMocksExpressions(string referenceAssemblyGroup, string @namespace, string mockExpression)
    {
        static string Template(string ns, string mock) =>
            $$"""
              {{ns}}
              using System;

              public class ConcreteClass
              {
                  public int Field;
                  public static int StaticField;
                  public event EventHandler MyEvent;
              }

              public interface IRepository
              {
                  bool IsAuthenticated { get; set; }
              }

              public interface IServiceProvider
              {
                  object GetService(Type serviceType);
              }

              internal class UnitTest
              {
                  private void Test()
                  {
                      var mock = {{mock}}
                  }
              }
              """;

        string o = Template(@namespace, mockExpression);

        output.WriteLine("Original:");
        output.WriteLine(o);

        await Verifier.VerifyAnalyzerAsync(o, referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(MoqReferenceAssemblyGroups))]
    public async Task ShouldNotFlagExactReporterScenarioNestedMockOfInMethodArg(string referenceAssemblyGroup)
    {
        // Exact pattern from https://github.com/rjmurillo/moq.analyzers/issues/1010:
        // Mock.Of inside a method argument, with static const on right side.
        await Verifier.VerifyAnalyzerAsync(
            """
            using Moq;

            public interface IResponse
            {
                int Status { get; }
                bool IsError { get; }
            }

            public static class StatusCodes
            {
                public const int Status200OK = 200;
            }

            public class ServiceUnderTest
            {
                public IResponse CreateResponse(IResponse inner) => inner;
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var sut = new ServiceUnderTest();
                    var result = sut.CreateResponse(Mock.Of<IResponse>(r => r.Status == StatusCodes.Status200OK));
                }
            }
            """,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(MoqReferenceAssemblyGroups))]
    public async Task ShouldNotFlagStaticLambdaWithExternalConstant(string referenceAssemblyGroup)
    {
        // Static lambda prevents closures but should not change analyzer behavior.
        await Verifier.VerifyAnalyzerAsync(
            """
            using Moq;

            public interface IResponse
            {
                int Status { get; }
            }

            public static class StatusCodes
            {
                public const int Status200OK = 200;
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var response = Mock.Of<IResponse>(static r => r.Status == StatusCodes.Status200OK);
                }
            }
            """,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(MoqReferenceAssemblyGroups))]
    public async Task ShouldNotFlagCapturedLocalVariableAsComparisonValue(string referenceAssemblyGroup)
    {
        // Captured local is not rooted in the lambda parameter.
        await Verifier.VerifyAnalyzerAsync(
            """
            using Moq;

            public interface IService
            {
                int Priority { get; }
            }

            internal class UnitTest
            {
                private void Test()
                {
                    int expectedPriority = 5;
                    var svc = Mock.Of<IService>(s => s.Priority == expectedPriority);
                }
            }
            """,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(MoqReferenceAssemblyGroups))]
    public async Task ShouldNotFlagMethodParameterAsComparisonValue(string referenceAssemblyGroup)
    {
        // Method parameter captured in lambda is not rooted in the lambda parameter.
        await Verifier.VerifyAnalyzerAsync(
            """
            using Moq;

            public interface IService
            {
                string Name { get; }
            }

            internal class UnitTest
            {
                private IService CreateMock(string expectedName)
                {
                    return Mock.Of<IService>(s => s.Name == expectedName);
                }
            }
            """,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(MoqReferenceAssemblyGroups))]
    public async Task ShouldNotFlagInstanceFieldAsComparisonValue(string referenceAssemblyGroup)
    {
        // Instance field on 'this' (captured) is not rooted in the lambda parameter.
        await Verifier.VerifyAnalyzerAsync(
            """
            using Moq;

            public interface IService
            {
                string Name { get; }
            }

            internal class UnitTest
            {
                private readonly string _defaultName = "test";

                private void Test()
                {
                    var svc = Mock.Of<IService>(s => s.Name == _defaultName);
                }
            }
            """,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(MoqReferenceAssemblyGroups))]
    public async Task ShouldNotFlagInequalityComparisonWithExternalConstant(string referenceAssemblyGroup)
    {
        // != is also IBinaryOperation; external constant should not be flagged.
        await Verifier.VerifyAnalyzerAsync(
            """
            using Moq;

            public interface IResponse
            {
                int Status { get; }
            }

            public static class StatusCodes
            {
                public const int Status404NotFound = 404;
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var response = Mock.Of<IResponse>(r => r.Status != StatusCodes.Status404NotFound);
                }
            }
            """,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(MoqReferenceAssemblyGroups))]
    public async Task ShouldNotFlagChainedExternalInstanceProperty(string referenceAssemblyGroup)
    {
        // Multi-hop property access on a captured local (not lambda parameter).
        await Verifier.VerifyAnalyzerAsync(
            """
            using Moq;

            public interface IService
            {
                string Name { get; }
            }

            public class AppSettings
            {
                public ServiceConfig Service { get; set; }
            }

            public class ServiceConfig
            {
                public string DefaultName { get; set; }
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var settings = new AppSettings { Service = new ServiceConfig { DefaultName = "test" } };
                    var svc = Mock.Of<IService>(s => s.Name == settings.Service.DefaultName);
                }
            }
            """,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(MoqReferenceAssemblyGroups))]
    public async Task ShouldFlagNonVirtualMethodWithExternalArguments(string referenceAssemblyGroup)
    {
        // The method itself is non-virtual and rooted in lambda parameter.
        // External arguments do not change that the method access is invalid.
        await Verifier.VerifyAnalyzerAsync(
            """
            using Moq;

            public class ConcreteClass
            {
                public string Format(string input) => input;
            }

            public static class Constants
            {
                public const string Template = "hello";
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var mock = Mock.Of<ConcreteClass>(c => {|Moq1302:c.Format(Constants.Template)|} == "result");
                }
            }
            """,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(MoqReferenceAssemblyGroups))]
    public async Task ShouldNotFlagStringConcatenationOnRightSide(string referenceAssemblyGroup)
    {
        // String concatenation produces an IBinaryOperation with Add operator.
        // Neither operand is rooted in the lambda parameter.
        await Verifier.VerifyAnalyzerAsync(
            """
            using Moq;

            public interface IService
            {
                string Name { get; }
            }

            public static class Prefix
            {
                public const string Value = "svc";
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var svc = Mock.Of<IService>(s => s.Name == Prefix.Value + "-default");
                }
            }
            """,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(MoqReferenceAssemblyGroups))]
    public async Task ShouldNotFlagOrComparisonWithAllExternalConstants(string referenceAssemblyGroup)
    {
        // || with interface members and external constants: no false positives.
        await Verifier.VerifyAnalyzerAsync(
            """
            using Moq;

            public interface IResponse
            {
                int Status { get; }
            }

            public static class StatusCodes
            {
                public const int Status200OK = 200;
                public const int Status204NoContent = 204;
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var response = Mock.Of<IResponse>(r =>
                        r.Status == StatusCodes.Status200OK ||
                        r.Status == StatusCodes.Status204NoContent);
                }
            }
            """,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(MoqReferenceAssemblyGroups))]
    public async Task ShouldNotAnalyzeNonMockOfInvocations(string referenceAssemblyGroup)
    {
        await Verifier.VerifyAnalyzerAsync(
            """
            public interface IRepository
            {
                bool IsAuthenticated { get; set; }
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var mock = new Mock<IRepository>();
                    mock.Setup(r => r.IsAuthenticated).Returns(true);
                    
                    // This should not be analyzed by this analyzer
                    var someMethod = SomeMethod(r => r.IsAuthenticated == true);
                }
                
                private string SomeMethod(System.Func<IRepository, bool> predicate) => "test";
            }
            """,
            referenceAssemblyGroup);
    }

    [Fact]
    public async Task ShouldAnalyzeDeepButUnderCapExpression()
    {
        // Boundary case for https://github.com/rjmurillo/moq.analyzers/issues/1261.
        // A left-associative chain of boolean-or clauses nests the leftmost clause deepest in the
        // operation tree, one level per operator. With 39 clauses the deepest clause sits well under
        // MaxAnalysisDepth (64), so its non-virtual member must still be flagged. All shallower
        // clauses reference a virtual member and must NOT be flagged.
        string[] clauses = new string[39];
        clauses[0] = "{|Moq1302:c.NonVirtualProperty|} == \"v0\"";
        for (int i = 1; i < clauses.Length; i++)
        {
            clauses[i] = "c.VirtualProperty == \"v" + i + "\"";
        }

        string expression = string.Join(" || ", clauses);

        await Verifier.VerifyAnalyzerAsync(
            $$"""
              using Moq;

              public class ConcreteClass
              {
                  public virtual string VirtualProperty { get; set; }
                  public string NonVirtualProperty { get; set; }
              }

              internal class UnitTest
              {
                  private void Test()
                  {
                      var mock = Mock.Of<ConcreteClass>(c => {{expression}});
                  }
              }
              """,
            ReferenceAssemblyCatalog.Net100WithNewMoq);
    }

    [Fact]
    public async Task ShouldReportAtExactDepthCap()
    {
        // Exact-boundary case for https://github.com/rjmurillo/moq.analyzers/issues/1261.
        // In a left-associative chain of boolean-or clauses, the leftmost clause nests deepest,
        // one level per operator. Expression lambdas also add a synthesized block-and-return
        // wrapper worth two more levels, so with 62 clauses the deepest member lands at exactly
        // MaxAnalysisDepth of 64. The guard skips operations strictly deeper than the cap, so a
        // member sitting at the cap is still analyzed and its non-virtual member must be flagged.
        // Verified empirically: the deepest clause is the last one reported at 62 clauses and is
        // suppressed at 63. Tightening the guard to also skip the exact cap would break this test.
        // Only the leftmost clause references a non-virtual member, so its diagnostic location is
        // unambiguous. The analyzer resolves member locations by symbol and returns the first match.
        string[] clauses = new string[62];
        clauses[0] = "{|Moq1302:c.NonVirtualProperty|} == \"v0\"";
        for (int i = 1; i < clauses.Length; i++)
        {
            clauses[i] = "c.VirtualProperty == \"v" + i + "\"";
        }

        string expression = string.Join(" || ", clauses);

        await Verifier.VerifyAnalyzerAsync(
            $$"""
              using Moq;

              public class ConcreteClass
              {
                  public virtual string VirtualProperty { get; set; }
                  public string NonVirtualProperty { get; set; }
              }

              internal class UnitTest
              {
                  private void Test()
                  {
                      var mock = Mock.Of<ConcreteClass>(c => {{expression}});
                  }
              }
              """,
            ReferenceAssemblyCatalog.Net100WithNewMoq);
    }

    [Fact]
    public async Task ShouldSuppressJustBeyondDepthCapButStillReportShallowMembers()
    {
        // Just-over-boundary case for https://github.com/rjmurillo/moq.analyzers/issues/1261.
        // With 63 clauses the leftmost (deepest) member sits one level past the cap, so it is
        // suppressed and the deep member carries no markup. The rightmost clause is shallow and its
        // non-virtual member must still be flagged. This proves the cap suppresses only deep members,
        // not the whole expression, and would fail if the guard also skipped members at the exact cap.
        // The deep and shallow members use distinct names so their diagnostic locations do not
        // collide. The analyzer resolves member locations by symbol and returns the first match.
        string[] clauses = new string[63];
        clauses[0] = "c.NonVirtualDeep == \"v0\"";
        for (int i = 1; i < clauses.Length - 1; i++)
        {
            clauses[i] = "c.VirtualProperty == \"v" + i + "\"";
        }

        clauses[clauses.Length - 1] = "{|Moq1302:c.NonVirtualShallow|} == \"v62\"";

        string expression = string.Join(" || ", clauses);

        await Verifier.VerifyAnalyzerAsync(
            $$"""
              using Moq;

              public class ConcreteClass
              {
                  public virtual string VirtualProperty { get; set; }
                  public string NonVirtualDeep { get; set; }
                  public string NonVirtualShallow { get; set; }
              }

              internal class UnitTest
              {
                  private void Test()
                  {
                      var mock = Mock.Of<ConcreteClass>(c => {{expression}});
                  }
              }
              """,
            ReferenceAssemblyCatalog.Net100WithNewMoq);
    }

    [Fact]
    public async Task ShouldBailSilentlyBeyondDepthCapWithoutCrashing()
    {
        // Regression for the stack-overflow crash in https://github.com/rjmurillo/moq.analyzers/issues/1261.
        // A 499-clause boolean-or chain nests the leftmost clause about 499 levels deep, far beyond
        // MaxAnalysisDepth (64). The depth guard stops the walk before reaching that clause, so the
        // non-virtual member there produces NO diagnostic (accepted false negative) and, critically,
        // the analyzer does not overflow the stack. Every clause the walk DOES reach is virtual, so
        // the expected diagnostic count is zero.
        string[] clauses = new string[499];
        clauses[0] = "c.NonVirtualProperty == \"v0\"";
        for (int i = 1; i < clauses.Length; i++)
        {
            clauses[i] = "c.VirtualProperty == \"v" + i + "\"";
        }

        string expression = string.Join(" || ", clauses);

        await Verifier.VerifyAnalyzerAsync(
            $$"""
              using Moq;

              public class ConcreteClass
              {
                  public virtual string VirtualProperty { get; set; }
                  public string NonVirtualProperty { get; set; }
              }

              internal class UnitTest
              {
                  private void Test()
                  {
                      var mock = Mock.Of<ConcreteClass>(c => {{expression}});
                  }
              }
              """,
            ReferenceAssemblyCatalog.Net100WithNewMoq);
    }
}
