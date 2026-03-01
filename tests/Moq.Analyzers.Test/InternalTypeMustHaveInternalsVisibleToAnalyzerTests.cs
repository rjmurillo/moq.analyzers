using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.InternalTypeMustHaveInternalsVisibleToAnalyzer>;

namespace Moq.Analyzers.Test;

public class InternalTypeMustHaveInternalsVisibleToAnalyzerTests
{
    public static IEnumerable<object[]> InternalTypeWithoutAttributeTestData()
    {
        return new object[][]
        {
            ["""new Mock<{|Moq1003:InternalClass|}>()"""],
            ["""new Mock<{|Moq1003:InternalClass|}>(MockBehavior.Strict)"""],
            ["""Mock.Of<{|Moq1003:InternalClass|}>()"""],
            ["""var mock = new Mock<{|Moq1003:InternalClass|}>()"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> PublicTypeTestData()
    {
        return new object[][]
        {
            ["""new Mock<PublicClass>()"""],
            ["""new Mock<PublicClass>(MockBehavior.Strict)"""],
            ["""Mock.Of<PublicClass>()"""],
            ["""var mock = new Mock<PublicClass>()"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> InterfaceTestData()
    {
        return new object[][]
        {
            // Internal interfaces also need InternalsVisibleTo
            ["""new Mock<{|Moq1003:IInternalInterface|}>()"""],
            ["""Mock.Of<{|Moq1003:IInternalInterface|}>()"""],

            // Public interfaces should not trigger
            ["""new Mock<IPublicInterface>()"""],
            ["""Mock.Of<IPublicInterface>()"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> NestedTypeTestData()
    {
        return new object[][]
        {
            // Public type nested inside internal type is effectively internal
            ["""new Mock<{|Moq1003:InternalOuter.PublicNested|}>()"""],

            // Internal type nested inside public type
            ["""new Mock<{|Moq1003:PublicOuter.InternalNested|}>()"""],

            // Public nested in public should not trigger
            ["""new Mock<PublicOuter.PublicNested>()"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(InternalTypeWithoutAttributeTestData))]
    public async Task ShouldDetectInternalTypeWithoutInternalsVisibleTo(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await Verifier.VerifyAnalyzerAsync(
                $$"""
                {{@namespace}}

                internal class InternalClass { public virtual void DoWork() { } }

                public class PublicClass { public virtual void DoWork() { } }

                internal class UnitTest
                {
                    private void Test()
                    {
                        {{mock}};
                    }
                }
                """,
                referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(PublicTypeTestData))]
    public async Task ShouldNotFlagPublicType(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await Verifier.VerifyAnalyzerAsync(
                $$"""
                {{@namespace}}

                public class PublicClass { public virtual void DoWork() { } }

                internal class UnitTest
                {
                    private void Test()
                    {
                        {{mock}};
                    }
                }
                """,
                referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(InterfaceTestData))]
    public async Task ShouldHandleInterfaces(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await Verifier.VerifyAnalyzerAsync(
                $$"""
                {{@namespace}}

                internal interface IInternalInterface { void DoWork(); }

                public interface IPublicInterface { void DoWork(); }

                internal class UnitTest
                {
                    private void Test()
                    {
                        {{mock}};
                    }
                }
                """,
                referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(NestedTypeTestData))]
    public async Task ShouldHandleNestedTypes(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await Verifier.VerifyAnalyzerAsync(
                $$"""
                {{@namespace}}

                internal class InternalOuter
                {
                    public class PublicNested { public virtual void DoWork() { } }
                }

                public class PublicOuter
                {
                    internal class InternalNested { public virtual void DoWork() { } }
                    public class PublicNested { public virtual void DoWork() { } }
                }

                internal class UnitTest
                {
                    private void Test()
                    {
                        {{mock}};
                    }
                }
                """,
                referenceAssemblyGroup);
    }

    [Fact]
    public async Task ShouldNotFlagInternalTypeWithCorrectInternalsVisibleTo()
    {
        await Verifier.VerifyAnalyzerAsync(
                """
                using Moq;
                using System.Runtime.CompilerServices;

                [assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

                internal class InternalClass { public virtual void DoWork() { } }

                internal class UnitTest
                {
                    private void Test()
                    {
                        var mock = new Mock<InternalClass>();
                        var of = Mock.Of<InternalClass>();
                    }
                }
                """,
                referenceAssemblyGroup: ReferenceAssemblyCatalog.Net80WithOldMoq);
    }

    [Fact]
    public async Task ShouldFlagInternalTypeWithWrongAssemblyName()
    {
        await Verifier.VerifyAnalyzerAsync(
                """
                using Moq;
                using System.Runtime.CompilerServices;

                [assembly: InternalsVisibleTo("SomeOtherAssembly")]

                internal class InternalClass { public virtual void DoWork() { } }

                internal class UnitTest
                {
                    private void Test()
                    {
                        var mock = new Mock<{|Moq1003:InternalClass|}>();
                        var of = Mock.Of<{|Moq1003:InternalClass|}>();
                    }
                }
                """,
                referenceAssemblyGroup: ReferenceAssemblyCatalog.Net80WithOldMoq);
    }

    [Fact]
    public async Task ShouldNotFlagInternalTypeWithPublicKeyInAttribute()
    {
        await Verifier.VerifyAnalyzerAsync(
                """
                using Moq;
                using System.Runtime.CompilerServices;

                [assembly: InternalsVisibleTo("DynamicProxyGenAssembly2, PublicKey=0024000004800000940000000602000000240000525341310004000001000100c547cac37abd99c8db225ef2f6c8a3602f3b3606cc9891605d02baa56104f4cfc0734aa39b93bf7852f7d9266654753cc297e7d2edfe0bac1cdcf9f717241550e0a7b191195b7667bb4f64bcb8e2121380fd1d9d46ad2d92d2d15605093924cceaf74c4861eff62abf69b9291ed0a340e113be11e6a7d3113e92484cf7045cc7")]

                internal class InternalClass { public virtual void DoWork() { } }

                internal class UnitTest
                {
                    private void Test()
                    {
                        var mock = new Mock<InternalClass>();
                    }
                }
                """,
                referenceAssemblyGroup: ReferenceAssemblyCatalog.Net80WithOldMoq);
    }

    [Fact]
    public async Task ShouldNotAnalyzeWhenMoqNotReferenced()
    {
        await Verifier.VerifyAnalyzerAsync(
                """
                namespace Test
                {
                    internal class InternalClass { }

                    internal class UnitTest
                    {
                        private void Test()
                        {
                            var instance = new InternalClass();
                        }
                    }
                }
                """,
                referenceAssemblyGroup: ReferenceAssemblyCatalog.Net80WithOldMoq);
    }

    [Fact]
    public async Task ShouldNotFlagAbstractPublicType()
    {
        await Verifier.VerifyAnalyzerAsync(
                """
                using Moq;

                public abstract class PublicAbstractClass { public abstract void DoWork(); }

                internal class UnitTest
                {
                    private void Test()
                    {
                        var mock = new Mock<PublicAbstractClass>();
                    }
                }
                """,
                referenceAssemblyGroup: ReferenceAssemblyCatalog.Net80WithOldMoq);
    }
}
