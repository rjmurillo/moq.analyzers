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

    public static IEnumerable<object[]> DelegateTestData()
    {
        return new object[][]
        {
            // Sealed delegates should be allowed (not trigger diagnostic)
            ["""new Mock<SealedDelegate>();"""],
            ["""new Mock<NonSealedDelegate>();"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
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

    public static IEnumerable<object[]> VariousCreationPatternsTestData()
    {
        return new object[][]
        {
            // Various ways to create Mock instances with sealed types
            ["""new Mock<{|Moq1000:SealedClass|}>();"""],
            ["""new Mock<{|Moq1000:SealedClass|}>(MockBehavior.Strict);"""],
            ["""Mock.Of<{|Moq1000:SealedClass|}>();"""],
            ["""var mock = new Mock<{|Moq1000:SealedClass|}>();"""],

            // Non-sealed should not trigger
            ["""new Mock<NonSealedClass>();"""],
            ["""new Mock<NonSealedClass>(MockBehavior.Strict);"""],
            ["""Mock.Of<NonSealedClass>();"""],
            ["""var mock = new Mock<NonSealedClass>();"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> ComplexScenariosTestData()
    {
        return new object[][]
        {
            // Nested classes and structs
            ["""new Mock<{|Moq1000:OuterClass.SealedNested|}>();"""],
            ["""new Mock<OuterClass.NonSealedNested>();"""],

            // Sealed structs (structs are always sealed but should trigger diagnostic)
            ["""new Mock<{|Moq1000:SealedStruct|}>();"""],

            // Enums (enums are sealed but should trigger diagnostic)
            ["""new Mock<{|Moq1000:TestEnum|}>();"""],

            // Interfaces should not trigger (they can't be sealed)
            ["""new Mock<ITestInterface>();"""],

            // Abstract classes should not trigger (they can't be sealed)
            ["""new Mock<AbstractClass>();"""],

            // Qualified names
            ["""new System.Collections.Generic.List<Mock<{|Moq1000:SealedClass|}>>();"""],

            // Complex generics
            ["""new Mock<{|Moq1000:SealedGeneric<int, string>|}>();"""],
            ["""new Mock<NonSealedGeneric<int, string>>();"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> NullableAndArrayTestData()
    {
        return new object[][]
        {
            // Nullable sealed types - nullable reference types are not sealed themselves
            ["""new Mock<SealedClass?>();"""],

            // Array types should not trigger (arrays are not the sealed type being mocked)
            ["""new Mock<SealedClass[]>();"""],
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

    [Theory]
    [MemberData(nameof(VariousCreationPatternsTestData))]
    public async Task ShouldDetectVariousCreationPatterns(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await Verifier.VerifyAnalyzerAsync(
                $$"""
                {{@namespace}}

                internal sealed class SealedClass { }

                internal class NonSealedClass { }

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

    [Theory]
    [MemberData(nameof(ComplexScenariosTestData))]
    public async Task ShouldHandleComplexScenarios(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await Verifier.VerifyAnalyzerAsync(
                $$"""
                {{@namespace}}

                internal class OuterClass
                {
                    internal sealed class SealedNested { }
                    internal class NonSealedNested { }
                }

                internal sealed struct SealedStruct { }

                internal enum TestEnum { Value1, Value2 }

                internal interface ITestInterface { }

                internal abstract class AbstractClass { }

                internal sealed class SealedClass { }

                internal sealed class SealedGeneric<T1, T2> { }

                internal class NonSealedGeneric<T1, T2> { }

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

    [Theory]
    [MemberData(nameof(NullableAndArrayTestData))]
    public async Task ShouldHandleNullableAndArrayTypes(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await Verifier.VerifyAnalyzerAsync(
                $$"""
                {{@namespace}}

                #nullable enable

                internal sealed class SealedClass { }

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
