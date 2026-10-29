using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.LinqToMocksExpressionShouldBeValidAnalyzer>;

namespace Moq.Analyzers.Test;

/// <summary>
/// Moq1302 tests for members outside the lambda parameter, such as static, enum, and external members. The analyzer must not flag them.
/// </summary>
public class LinqToMocksExpressionExternalMemberAnalyzerTests
{
    [Theory]
    [MemberData(nameof(LinqToMocksExpressionShouldBeValidAnalyzerTests.MoqReferenceAssemblyGroups), MemberType = typeof(LinqToMocksExpressionShouldBeValidAnalyzerTests))]
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
    [MemberData(nameof(LinqToMocksExpressionShouldBeValidAnalyzerTests.MoqReferenceAssemblyGroups), MemberType = typeof(LinqToMocksExpressionShouldBeValidAnalyzerTests))]
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
    [MemberData(nameof(LinqToMocksExpressionShouldBeValidAnalyzerTests.MoqReferenceAssemblyGroups), MemberType = typeof(LinqToMocksExpressionShouldBeValidAnalyzerTests))]
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
    [MemberData(nameof(LinqToMocksExpressionShouldBeValidAnalyzerTests.MoqReferenceAssemblyGroups), MemberType = typeof(LinqToMocksExpressionShouldBeValidAnalyzerTests))]
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
    [MemberData(nameof(LinqToMocksExpressionShouldBeValidAnalyzerTests.MoqReferenceAssemblyGroups), MemberType = typeof(LinqToMocksExpressionShouldBeValidAnalyzerTests))]
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
    [MemberData(nameof(LinqToMocksExpressionShouldBeValidAnalyzerTests.MoqReferenceAssemblyGroups), MemberType = typeof(LinqToMocksExpressionShouldBeValidAnalyzerTests))]
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
    [MemberData(nameof(LinqToMocksExpressionShouldBeValidAnalyzerTests.MoqReferenceAssemblyGroups), MemberType = typeof(LinqToMocksExpressionShouldBeValidAnalyzerTests))]
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
    [MemberData(nameof(LinqToMocksExpressionShouldBeValidAnalyzerTests.MoqReferenceAssemblyGroups), MemberType = typeof(LinqToMocksExpressionShouldBeValidAnalyzerTests))]
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
    [MemberData(nameof(LinqToMocksExpressionShouldBeValidAnalyzerTests.MoqReferenceAssemblyGroups), MemberType = typeof(LinqToMocksExpressionShouldBeValidAnalyzerTests))]
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
    [MemberData(nameof(LinqToMocksExpressionShouldBeValidAnalyzerTests.MoqReferenceAssemblyGroups), MemberType = typeof(LinqToMocksExpressionShouldBeValidAnalyzerTests))]
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
}
