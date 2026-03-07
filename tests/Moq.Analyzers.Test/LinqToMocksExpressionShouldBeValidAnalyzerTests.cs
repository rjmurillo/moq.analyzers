using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.LinqToMocksExpressionShouldBeValidAnalyzer>;

namespace Moq.Analyzers.Test;

public class LinqToMocksExpressionShouldBeValidAnalyzerTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper output = output;

    /// <summary>
    /// Provides both Moq reference assembly versions (4.8.2 and 4.18.4) for <c>[Theory]</c> tests.
    /// All analyzer tests must run against both versions to catch version-specific regressions.
    /// </summary>
    /// <returns>One element per Moq reference assembly group.</returns>
    public static IEnumerable<object[]> MoqReferenceAssemblyGroups()
    {
        yield return [ReferenceAssemblyCatalog.Net80WithOldMoq];
        yield return [ReferenceAssemblyCatalog.Net80WithNewMoq];
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
    public async Task ShouldNotFlagStaticConstOnRightSideOfComparison(string referenceAssemblyGroup)
    {
        // Repro for https://github.com/rjmurillo/moq.analyzers/issues/1010
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
                    var response = Mock.Of<IResponse>(r => r.Status == StatusCodes.Status200OK);
                }
            }
            """,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(MoqReferenceAssemblyGroups))]
    public async Task ShouldNotFlagStaticPropertyOnRightSideOfComparison(string referenceAssemblyGroup)
    {
        await Verifier.VerifyAnalyzerAsync(
            """
            using Moq;

            public interface IResponse
            {
                int Status { get; }
            }

            public static class StatusCodes
            {
                public static int Status202Accepted => 202;
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var response = Mock.Of<IResponse>(r => r.Status == StatusCodes.Status202Accepted);
                }
            }
            """,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(MoqReferenceAssemblyGroups))]
    public async Task ShouldNotFlagEnumValueOnRightSideOfComparison(string referenceAssemblyGroup)
    {
        await Verifier.VerifyAnalyzerAsync(
            """
            using Moq;

            public enum UserStatus { Active, Inactive }

            public interface IUser
            {
                UserStatus Status { get; }
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var user = Mock.Of<IUser>(u => u.Status == UserStatus.Active);
                }
            }
            """,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(MoqReferenceAssemblyGroups))]
    public async Task ShouldNotFlagStaticFieldOnLeftSideOfComparison(string referenceAssemblyGroup)
    {
        // Value expression on the left, mocked member on the right
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
                    var response = Mock.Of<IResponse>(r => StatusCodes.Status200OK == r.Status);
                }
            }
            """,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(MoqReferenceAssemblyGroups))]
    public async Task ShouldNotFlagExternalMembersInChainedComparisons(string referenceAssemblyGroup)
    {
        // Multiple comparisons joined with && using external constants
        await Verifier.VerifyAnalyzerAsync(
            """
            using Moq;

            public interface IResponse
            {
                int Status { get; }
                string ReasonPhrase { get; }
            }

            public static class StatusCodes
            {
                public const int Status200OK = 200;
            }

            public static class Reasons
            {
                public const string Ok = "OK";
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var response = Mock.Of<IResponse>(r =>
                        r.Status == StatusCodes.Status200OK &&
                        r.ReasonPhrase == Reasons.Ok);
                }
            }
            """,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(MoqReferenceAssemblyGroups))]
    public async Task ShouldNotFlagStaticMethodCallOnRightSideOfComparison(string referenceAssemblyGroup)
    {
        await Verifier.VerifyAnalyzerAsync(
            """
            using Moq;

            public interface ITimer
            {
                int Timeout { get; }
            }

            public class Defaults
            {
                public static int GetTimeout() => 30;
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var timer = Mock.Of<ITimer>(t => t.Timeout == Defaults.GetTimeout());
                }
            }
            """,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(MoqReferenceAssemblyGroups))]
    public async Task ShouldStillFlagNonVirtualMemberOnLambdaParameter(string referenceAssemblyGroup)
    {
        // The fix must not suppress true positives: non-virtual members accessed
        // through the lambda parameter should still be flagged.
        await Verifier.VerifyAnalyzerAsync(
            """
            using Moq;

            public class ConcreteClass
            {
                public string NonVirtualProperty { get; set; }
            }

            public static class Constants
            {
                public const string DefaultValue = "default";
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var mock = Mock.Of<ConcreteClass>(c => {|Moq1302:c.NonVirtualProperty|} == Constants.DefaultValue);
                }
            }
            """,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(MoqReferenceAssemblyGroups))]
    public async Task ShouldStillFlagFieldOnLambdaParameterWithExternalConstant(string referenceAssemblyGroup)
    {
        // Fields on the lambda parameter are always invalid, even when the
        // other side of the comparison is an external constant.
        await Verifier.VerifyAnalyzerAsync(
            """
            using Moq;

            public class ConcreteClass
            {
                public int Field;
            }

            public static class Constants
            {
                public const int Value = 42;
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var mock = Mock.Of<ConcreteClass>(c => {|Moq1302:c.Field|} == Constants.Value);
                }
            }
            """,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(MoqReferenceAssemblyGroups))]
    public async Task ShouldNotFlagInstancePropertyOnExternalObjectInComparison(string referenceAssemblyGroup)
    {
        // Instance property on a local variable (not the lambda parameter) should not be flagged
        await Verifier.VerifyAnalyzerAsync(
            """
            using Moq;

            public interface IService
            {
                string Name { get; }
            }

            public class Config
            {
                public string ServiceName { get; set; }
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var config = new Config { ServiceName = "test" };
                    var svc = Mock.Of<IService>(s => s.Name == config.ServiceName);
                }
            }
            """,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(MoqReferenceAssemblyGroups))]
    public async Task ShouldNotFlagTernaryWithStaticMember(string referenceAssemblyGroup)
    {
        // Ternary (conditional) expression containing a static member reference
        // exercises the default branch in AnalyzeLambdaBody via IConditionalOperation.
        await Verifier.VerifyAnalyzerAsync(
            """
            using Moq;

            public interface IService
            {
                string Name { get; }
            }

            public static class Defaults
            {
                public static string FallbackName => "fallback";
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var svc = Mock.Of<IService>(s => s.Name == (true ? "active" : Defaults.FallbackName));
                }
            }
            """,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(MoqReferenceAssemblyGroups))]
    public async Task ShouldNotFlagNullCoalescingWithExternalDefault(string referenceAssemblyGroup)
    {
        // Null-coalescing expression exercises the default branch in AnalyzeLambdaBody
        // via ICoalesceOperation. The external constant should not be flagged.
        await Verifier.VerifyAnalyzerAsync(
            """
            using Moq;

            public interface IRepository
            {
                string Name { get; }
            }

            public static class Constants
            {
                public const string DefaultName = "default";
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var repo = Mock.Of<IRepository>(r => (r.Name ?? Constants.DefaultName) == "test");
                }
            }
            """,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(MoqReferenceAssemblyGroups))]
    public async Task ShouldFlagNonVirtualMemberInsideConditionalExpression(string referenceAssemblyGroup)
    {
        // Non-virtual member inside a ternary expression should be flagged.
        // Virtual members and string literals in the same ternary should not be flagged.
        await Verifier.VerifyAnalyzerAsync(
            """
            using Moq;

            public class ConcreteClass
            {
                public string NonVirtualProperty { get; set; }
                public virtual bool IsEnabled { get; set; }
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var mock = Mock.Of<ConcreteClass>(c => (c.IsEnabled ? {|Moq1302:c.NonVirtualProperty|} : "none") == "test");
                }
            }
            """,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(MoqReferenceAssemblyGroups))]
    public async Task ShouldHandleChainedPropertyAccessOnLambdaParameter(string referenceAssemblyGroup)
    {
        // Exercises multiple hops in the IsRootedInLambdaParameter receiver chain walk.
        await Verifier.VerifyAnalyzerAsync(
            """
            using Moq;

            public interface IInner
            {
                string Value { get; }
            }

            public interface IOuter
            {
                IInner Inner { get; }
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var mock = Mock.Of<IOuter>(o => o.Inner.Value == "test");
                }
            }
            """,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(MoqReferenceAssemblyGroups))]
    public async Task ShouldFlagNonVirtualMemberInChainedAndComparison(string referenceAssemblyGroup)
    {
        // Regression test: chained && must not suppress non-virtual member diagnostics.
        // The inner == comparisons are IBinaryOperation nodes that must pass through
        // AnalyzeLambdaBody for decomposition, not be blocked by the guard.
        await Verifier.VerifyAnalyzerAsync(
            """
            using Moq;

            public class ConcreteClass
            {
                public string NonVirtualProperty { get; set; }
                public virtual int VirtualProperty { get; set; }
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var mock = Mock.Of<ConcreteClass>(c =>
                        {|Moq1302:c.NonVirtualProperty|} == "a" &&
                        c.VirtualProperty == 1);
                }
            }
            """,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(MoqReferenceAssemblyGroups))]
    public async Task ShouldFlagMultipleNonVirtualMembersInChainedComparison(string referenceAssemblyGroup)
    {
        // Both non-virtual members in a chained && should be flagged.
        await Verifier.VerifyAnalyzerAsync(
            """
            using Moq;

            public class ConcreteClass
            {
                public string Name { get; set; }
                public int Age { get; set; }
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var mock = Mock.Of<ConcreteClass>(c =>
                        {|Moq1302:c.Name|} == "test" &&
                        {|Moq1302:c.Age|} == 42);
                }
            }
            """,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(MoqReferenceAssemblyGroups))]
    public async Task ShouldNotFlagVirtualMembersInChainedComparisonWithStaticConstants(string referenceAssemblyGroup)
    {
        // No false positives: virtual members with static constants in chained &&.
        await Verifier.VerifyAnalyzerAsync(
            """
            using Moq;

            public interface IService
            {
                string Name { get; }
                int Priority { get; }
                bool IsEnabled { get; }
            }

            public static class Defaults
            {
                public const string ServiceName = "default";
                public const int DefaultPriority = 5;
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var svc = Mock.Of<IService>(s =>
                        s.Name == Defaults.ServiceName &&
                        s.Priority == Defaults.DefaultPriority &&
                        s.IsEnabled == true);
                }
            }
            """,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(MoqReferenceAssemblyGroups))]
    public async Task ShouldFlagNonVirtualMemberInOrComparison(string referenceAssemblyGroup)
    {
        // || is also an IBinaryOperation; non-virtual members must still be flagged.
        await Verifier.VerifyAnalyzerAsync(
            """
            using Moq;

            public class ConcreteClass
            {
                public string NonVirtualProperty { get; set; }
                public virtual bool IsActive { get; set; }
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var mock = Mock.Of<ConcreteClass>(c =>
                        {|Moq1302:c.NonVirtualProperty|} == "x" ||
                        c.IsActive == true);
                }
            }
            """,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(MoqReferenceAssemblyGroups))]
    public async Task ShouldFlagNonVirtualPropertyInNullCoalescing(string referenceAssemblyGroup)
    {
        // Non-virtual member rooted in lambda parameter inside null-coalescing
        // should still be flagged.
        await Verifier.VerifyAnalyzerAsync(
            """
            using Moq;

            public class ConcreteClass
            {
                public string NonVirtualProperty { get; set; }
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var mock = Mock.Of<ConcreteClass>(c =>
                        ({|Moq1302:c.NonVirtualProperty|} ?? "fallback") == "test");
                }
            }
            """,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(MoqReferenceAssemblyGroups))]
    public async Task ShouldNotFlagNestedMockOfExpression(string referenceAssemblyGroup)
    {
        // Nested Mock.Of calls have their own lambda parameters and should not
        // be recursively analyzed by the outer lambda's analysis.
        await Verifier.VerifyAnalyzerAsync(
            """
            using Moq;

            public interface IInner
            {
                string Value { get; }
            }

            public interface IOuter
            {
                IInner Inner { get; }
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var mock = Mock.Of<IOuter>(o => o.Inner == Mock.Of<IInner>(i => i.Value == "nested"));
                }
            }
            """,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(MoqReferenceAssemblyGroups))]
    public async Task ShouldNotFlagMemberAccessThroughExplicitCast(string referenceAssemblyGroup)
    {
        // Explicit cast on the lambda parameter inserts an IConversionOperation
        // in the receiver chain. IsRootedInLambdaParameter must walk through it.
        await Verifier.VerifyAnalyzerAsync(
            """
            using Moq;

            public interface IBase
            {
                string Name { get; }
            }

            public interface IDerived : IBase
            {
                string Extra { get; }
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var mock = Mock.Of<IDerived>(d => ((IBase)d).Name == "test");
                }
            }
            """,
            referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(MoqReferenceAssemblyGroups))]
    public async Task ShouldNotFlagVirtualInstanceMethodOnLambdaParameter(string referenceAssemblyGroup)
    {
        // Instance method call on the lambda parameter exercises the
        // IInvocationOperation branch with non-null Instance in IsRootedInLambdaParameter.
        await Verifier.VerifyAnalyzerAsync(
            """
            using Moq;

            public interface IFormatter
            {
                string Format(string input);
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var mock = Mock.Of<IFormatter>(f => f.Format("x") == "formatted");
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
}
