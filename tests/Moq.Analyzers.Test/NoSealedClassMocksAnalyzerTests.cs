using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.NoSealedClassMocksAnalyzer>;

namespace Moq.Analyzers.Test;

public class NoSealedClassMocksAnalyzerTests
{
    public static IEnumerable<object[]> TestData()
    {
        return new object[][]
        {
            ["""new Mock<{|Moq1000:FooSealed|}>();"""],
            ["""new Mock<Foo>();"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShoulAnalyzeSealedClassMocks(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await Verifier.VerifyAnalyzerAsync(
                $$"""
                {{@namespace}}

                internal sealed class FooSealed { }

                internal class Foo { }

                internal class UnitTest
                {
                    private void Test()
                    {
                        {{mock}}
                    }
                }
                """,
                referenceAssemblyGroup);
    }

    public static IEnumerable<object[]> DelegateTestData()
    {
        return new object[][]
        {
            // Sealed delegates should be allowed (not trigger diagnostic)
            ["""new Mock<SealedDelegate>();"""],
            ["""new Mock<NonSealedDelegate>();"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(DelegateTestData))]
    public async Task ShouldAllowSealedDelegates(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await Verifier.VerifyAnalyzerAsync(
                $$"""
                {{@namespace}}

                internal sealed delegate void SealedDelegate();

                internal delegate void NonSealedDelegate();

                internal class UnitTest
                {
                    private void Test()
                    {
                        {{mock}}
                    }
                }
                """,
                referenceAssemblyGroup);
    }

    public static IEnumerable<object[]> EdgeCaseTestData()
    {
        return new object[][]
        {
            // Non-Mock object creation should not trigger diagnostic
            ["""new SealedClass();"""],
            ["""new List<int>();"""],

            // Complex generic types
            ["""new Mock<{|Moq1000:SealedGeneric<int>|}>();"""],
            ["""new Mock<NonSealedGeneric<int>>();"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(EdgeCaseTestData))]
    public async Task ShouldHandleEdgeCases(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await Verifier.VerifyAnalyzerAsync(
                $$"""
                {{@namespace}}

                using System.Collections.Generic;

                internal sealed class SealedClass { }

                internal sealed class SealedGeneric<T> { }

                internal class NonSealedGeneric<T> { }

                internal class UnitTest
                {
                    private void Test()
                    {
                        {{mock}}
                    }
                }
                """,
                referenceAssemblyGroup);
    }

    public static IEnumerable<object[]> BuiltInSealedTypesTestData()
    {
        return new object[][]
        {
            // Built-in sealed types should trigger diagnostic
            ["""new Mock<{|Moq1000:string|}>();"""],
            ["""new Mock<{|Moq1000:int|}>();"""],
            ["""new Mock<{|Moq1000:DateTime|}>();"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(BuiltInSealedTypesTestData))]
    public async Task ShouldDetectBuiltInSealedTypes(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await Verifier.VerifyAnalyzerAsync(
                $$"""
                {{@namespace}}

                using System;

                internal class UnitTest
                {
                    private void Test()
                    {
                        {{mock}}
                    }
                }
                """,
                referenceAssemblyGroup);
    }

    public static IEnumerable<object[]> VariousCreationPatternsTestData()
    {
        return new object[][]
        {
            // Various ways of creating Mock instances
            ["""var mock = new Mock<{|Moq1000:FooSealed|}>();"""],
            ["""Mock<{|Moq1000:FooSealed|}> mock = new();"""],
            ["""Mock<{|Moq1000:FooSealed|}> mock = new Mock<{|Moq1000:FooSealed|}>();"""],

            // Constructor with parameters
            ["""new Mock<{|Moq1000:FooSealed|}>(MockBehavior.Strict);"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(VariousCreationPatternsTestData))]
    public async Task ShouldDetectVariousCreationPatterns(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await Verifier.VerifyAnalyzerAsync(
                $$"""
                {{@namespace}}

                internal sealed class FooSealed { }

                internal class UnitTest
                {
                    private void Test()
                    {
                        {{mock}}
                    }
                }
                """,
                referenceAssemblyGroup);
    }

    public static IEnumerable<object[]> ComplexScenariosTestData()
    {
        return new object[][]
        {
            // Nested sealed classes
            ["""new Mock<{|Moq1000:OuterClass.NestedSealed|}>();"""],

            // Sealed struct
            ["""new Mock<{|Moq1000:SealedStruct|}>();"""],

            // Sealed enum
            ["""new Mock<{|Moq1000:SealedEnum|}>();"""],

            // Interface should not trigger
            ["""new Mock<ISampleInterface>();"""],

            // Abstract class should not trigger
            ["""new Mock<AbstractClass>();"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(ComplexScenariosTestData))]
    public async Task ShouldHandleComplexScenarios(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await Verifier.VerifyAnalyzerAsync(
                $$"""
                {{@namespace}}

                public interface ISampleInterface
                {
                    void Method();
                }

                public abstract class AbstractClass
                {
                    public abstract void Method();
                }

                internal sealed struct SealedStruct
                {
                    public int Value;
                }

                internal enum SealedEnum
                {
                    Value1,
                    Value2
                }

                internal class OuterClass
                {
                    internal sealed class NestedSealed
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
                """,
                referenceAssemblyGroup);
    }

    public static IEnumerable<object[]> NullableAndArrayTestData()
    {
        return new object[][]
        {
            // Nullable sealed type should still trigger
            ["""new Mock<{|Moq1000:string|}?>();"""],

            // Array of sealed type - arrays themselves are not sealed
            ["""new Mock<string[]>();"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(NullableAndArrayTestData))]
    public async Task ShouldHandleNullableAndArrayTypes(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await Verifier.VerifyAnalyzerAsync(
                $$"""
                {{@namespace}}

                internal class UnitTest
                {
                    private void Test()
                    {
                        {{mock}}
                    }
                }
                """,
                referenceAssemblyGroup);
    }
}
