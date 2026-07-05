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
        // A left-associative `||` chain nests the leftmost clause deepest in the operation tree
        // (one level per `||`). With 39 clauses the deepest clause sits well under
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
            ReferenceAssemblyCatalog.Net80WithNewMoq);
    }

    [Fact]
    public async Task ShouldReportAtExactDepthCap()
    {
        // Exact-boundary case for https://github.com/rjmurillo/moq.analyzers/issues/1261.
        // A left-associative `||` chain nests the leftmost clause deepest (one level per `||`);
        // expression lambdas add a synthesized block+return wrapper, so the deepest member of an
        // N-clause chain lands at depth N+2. With 62 clauses that is exactly MaxAnalysisDepth (64);
        // the guard is `depth > cap`, so depth 64 is still analyzed and the non-virtual member MUST
        // be flagged. Verified empirically: the deepest clause is the last one reported at N=62 and
        // is suppressed at N=63. A `>=` regression would suppress N=62 and fail this test.
        // Only the leftmost clause references a non-virtual member, so its diagnostic location is
        // unambiguous (the analyzer resolves member locations by symbol and returns the first match).
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
            ReferenceAssemblyCatalog.Net80WithNewMoq);
    }

    [Fact]
    public async Task ShouldSuppressJustBeyondDepthCapButStillReportShallowMembers()
    {
        // Just-over-boundary case for https://github.com/rjmurillo/moq.analyzers/issues/1261.
        // With 63 clauses the leftmost (deepest) member sits one past the cap, so it is suppressed
        // (no markup on `NonVirtualDeep`). The rightmost clause is shallow and its non-virtual member
        // (`NonVirtualShallow`) MUST still be flagged. This proves the cap suppresses only deep
        // members, not the whole expression, and would fail if the guard regressed to `>=`.
        // The deep and shallow members use distinct names so their diagnostic locations do not
        // collide (the analyzer resolves member locations by symbol and returns the first match).
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
            ReferenceAssemblyCatalog.Net80WithNewMoq);
    }

    [Fact]
    public async Task ShouldBailSilentlyBeyondDepthCapWithoutCrashing()
    {
        // Regression for the stack-overflow crash in https://github.com/rjmurillo/moq.analyzers/issues/1261.
        // A 499-clause `||` chain nests the leftmost clause ~499 levels deep, far beyond
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
            ReferenceAssemblyCatalog.Net80WithNewMoq);
    }
}
