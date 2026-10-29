using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.LinqToMocksExpressionShouldBeValidAnalyzer>;

namespace Moq.Analyzers.Test;

/// <summary>
/// Moq1302 tests for members reached through the lambda parameter. Non-virtual members are flagged; virtual members, nested Mock.Of, and casts are not.
/// </summary>
public class LinqToMocksExpressionLambdaMemberAnalyzerTests
{
    [Theory]
    [MemberData(nameof(LinqToMocksExpressionShouldBeValidAnalyzerTests.MoqReferenceAssemblyGroups), MemberType = typeof(LinqToMocksExpressionShouldBeValidAnalyzerTests))]
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
    [MemberData(nameof(LinqToMocksExpressionShouldBeValidAnalyzerTests.MoqReferenceAssemblyGroups), MemberType = typeof(LinqToMocksExpressionShouldBeValidAnalyzerTests))]
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
    [MemberData(nameof(LinqToMocksExpressionShouldBeValidAnalyzerTests.MoqReferenceAssemblyGroups), MemberType = typeof(LinqToMocksExpressionShouldBeValidAnalyzerTests))]
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
    [MemberData(nameof(LinqToMocksExpressionShouldBeValidAnalyzerTests.MoqReferenceAssemblyGroups), MemberType = typeof(LinqToMocksExpressionShouldBeValidAnalyzerTests))]
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
    [MemberData(nameof(LinqToMocksExpressionShouldBeValidAnalyzerTests.MoqReferenceAssemblyGroups), MemberType = typeof(LinqToMocksExpressionShouldBeValidAnalyzerTests))]
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
    [MemberData(nameof(LinqToMocksExpressionShouldBeValidAnalyzerTests.MoqReferenceAssemblyGroups), MemberType = typeof(LinqToMocksExpressionShouldBeValidAnalyzerTests))]
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
    [MemberData(nameof(LinqToMocksExpressionShouldBeValidAnalyzerTests.MoqReferenceAssemblyGroups), MemberType = typeof(LinqToMocksExpressionShouldBeValidAnalyzerTests))]
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
    [MemberData(nameof(LinqToMocksExpressionShouldBeValidAnalyzerTests.MoqReferenceAssemblyGroups), MemberType = typeof(LinqToMocksExpressionShouldBeValidAnalyzerTests))]
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
    [MemberData(nameof(LinqToMocksExpressionShouldBeValidAnalyzerTests.MoqReferenceAssemblyGroups), MemberType = typeof(LinqToMocksExpressionShouldBeValidAnalyzerTests))]
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
    [MemberData(nameof(LinqToMocksExpressionShouldBeValidAnalyzerTests.MoqReferenceAssemblyGroups), MemberType = typeof(LinqToMocksExpressionShouldBeValidAnalyzerTests))]
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
    [MemberData(nameof(LinqToMocksExpressionShouldBeValidAnalyzerTests.MoqReferenceAssemblyGroups), MemberType = typeof(LinqToMocksExpressionShouldBeValidAnalyzerTests))]
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
}
