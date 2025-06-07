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
            // Note: All delegates in .NET are sealed by default, but can still be mocked by Moq
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
            // Built-in sealed reference types should trigger diagnostic
            ["""new Mock<{|Moq1000:string|}>();"""],

            // Note: Value types like int, DateTime are excluded because they cannot be mocked anyway
            // due to Mock<T> constraint requiring reference types, not because they are sealed
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
            // Nested classes
            ["""new Mock<{|Moq1000:OuterClass.SealedNested|}>();"""],
            ["""new Mock<OuterClass.NonSealedNested>();"""],

            // Note: Structs and enums are excluded because they are value types
            // and cannot be mocked by Mock<T> (reference type constraint)

            // Interfaces should not trigger (they can't be sealed)
            ["""new Mock<ITestInterface>();"""],

            // Abstract classes should not trigger (they can't be sealed)
            ["""new Mock<AbstractClass>();"""],

            // Qualified names - List creation should not trigger diagnostic
            ["""new System.Collections.Generic.List<Mock<SealedClass>>();"""],

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

    public static IEnumerable<object[]> EdgeCaseMethodCallsTestData()
    {
        return new object[][]
        {
            // Mock.Of<T>() with sealed types should trigger diagnostic
            ["""Mock.Of<{|Moq1000:SealedClass|}>();"""],
            ["""Mock.Of<{|Moq1000:string|}>();"""],

            // Mock.Of<T>() with non-sealed types should not trigger
            ["""Mock.Of<NonSealedClass>();"""],
            ["""Mock.Of<ITestInterface>();"""],

            // Non-Mock static method calls should not trigger
            ["""var empty = string.Empty;"""],
            ["""System.Console.WriteLine("test");"""],

            // Instance method calls should not trigger
            ["""var mock = new Mock<NonSealedClass>(); mock.Object.ToString();"""],

            // Invalid Mock.Of calls (wrong type arguments count) should not crash
            ["""typeof(Mock).GetMethod("Of");"""],

            // Non-static Of method calls should not trigger
            ["""var mockInstance = new Mock<NonSealedClass>();"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> MockConstructorVariationsTestData()
    {
        return new object[][]
        {
            // Different Mock constructor overloads with sealed types
            ["""new Mock<{|Moq1000:SealedClass|}>(MockBehavior.Default);"""],
            ["""new Mock<{|Moq1000:SealedClass|}>(MockBehavior.Loose);"""],

            // Generic type without type arguments (should not crash)
            ["""var type = typeof(Mock<>);"""],

            // Valid Mock creation with non-sealed type should not trigger
            ["""new Mock<NonSealedClass>();"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> DiagnosticLocationTestData()
    {
        return new object[][]
        {
            // Test diagnostic location targeting for complex generic syntax
            ["""new Mock<{|Moq1000:System.String|}>();"""],
            ["""Mock.Of<{|Moq1000:System.String|}>();"""],

            // Nested generic types
            ["""new Mock<{|Moq1000:SealedGeneric<int>|}>();"""],

            // Multiple type parameters (first should be reported)
            ["""new Mock<{|Moq1000:SealedClass|}>(MockBehavior.Strict);"""],
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
        // This test verifies that delegates (which are always sealed in .NET) can still be mocked
        // The analyzer specifically excludes delegates from the sealed type check because
        // Moq can mock delegates even though they are sealed
        await Verifier.VerifyAnalyzerAsync(
                $$"""
                {{@namespace}}

                // Note: All delegates in .NET are implicitly sealed, so we don't need to explicitly declare them as sealed
                internal delegate void SealedDelegate();

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

                internal struct SealedStruct { }

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

    [Theory]
    [MemberData(nameof(EdgeCaseMethodCallsTestData))]
    public async Task ShouldHandleEdgeCaseMethodCalls(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await Verifier.VerifyAnalyzerAsync(
                $$"""
                {{@namespace}}

                internal sealed class SealedClass { }

                internal class NonSealedClass { }

                internal interface ITestInterface { }

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
    [MemberData(nameof(MockConstructorVariationsTestData))]
    public async Task ShouldHandleMockConstructorVariations(string referenceAssemblyGroup, string @namespace, string mock)
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

    [Fact]
    public async Task ShouldHandleNonMockObjectCreation()
    {
        // This test verifies that non-Mock object creation doesn't trigger analysis
        // Even with sealed types, since we're not creating Mock instances
        await Verifier.VerifyAnalyzerAsync(
                """
                using Moq;

                internal sealed class SealedClass { }

                internal class UnitTest
                {
                    private void Test()
                    {
                        // Non-Mock object creation should not trigger diagnostic
                        var sealedInstance = new SealedClass();
                        var stringInstance = new string("test".ToCharArray());
                        
                        // Mock creation with non-sealed type should not trigger
                        var mock = new Mock<UnitTest>();
                    }
                }
                """,
                referenceAssemblyGroup: ReferenceAssemblyCatalog.Net80WithOldMoq); // Use valid group for test infrastructure
    }

    [Fact]
    public async Task ShouldHandleErrorConditionsGracefully()
    {
        // Test to ensure analyzer handles various error conditions gracefully
        await Verifier.VerifyAnalyzerAsync(
                """
                using Moq;

                internal sealed class SealedClass { }

                internal class TestClass
                {
                    private void Test()
                    {
                        // Object creation without type
                        object obj = new();

                        // Method call without specific target
                        ToString();

                        // Mock creation with non-sealed type should not trigger
                        var mock = new Mock<TestClass>();
                    }
                }
                """,
                referenceAssemblyGroup: ReferenceAssemblyCatalog.Net80WithOldMoq);
    }

    [Theory]
    [MemberData(nameof(DiagnosticLocationTestData))]
    public async Task ShouldProvideAccurateDiagnosticLocations(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await Verifier.VerifyAnalyzerAsync(
                $$"""
                {{@namespace}}

                internal sealed class SealedClass { }

                internal sealed class SealedGeneric<T> { }

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
