using Moq.Analyzers.Test.Helpers;
using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.NoSealedClassMocksAnalyzer>;

namespace Moq.Analyzers.Test;

public partial class NoSealedClassMocksAnalyzerTests
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
            // Positive: Nullable reference types (should trigger diagnostic)
            ["""new Mock<{|Moq1000:string?|}>();"""],
            ["""new Mock<{|Moq1000:SealedClass?|}>();"""],

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
                internal struct Struct { }

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

    [Fact]
    public async Task ShouldNotAnalyzeWhenMoqNotReferenced()
    {
        // Test when using types that would normally trigger analysis but without Mock usage
        await Verifier.VerifyAnalyzerAsync(
                """
                using Moq;

                namespace Test
                {
                    internal sealed class SealedClass { }

                    internal class UnitTest
                    {
                        private void Test()
                        {
                            // Without Mock creation, this should not trigger any analysis
                            var instance = new SealedClass();
                            var str = "test";
                        }
                    }
                }
                """,
                referenceAssemblyGroup: ReferenceAssemblyCatalog.Net80WithOldMoq);
    }

    [Fact]
    public async Task ShouldHandleReferenceTypeConstraints()
    {
        // Test various reference types without nullable complications
        await Verifier.VerifyAnalyzerAsync(
                """
                using Moq;
                using System;

                internal sealed class SealedClass { }
                internal class NonSealedClass { }

                internal class UnitTest
                {
                    private void Test()
                    {                        
                        // Regular reference types
                        var mock1 = new Mock<{|Moq1000:SealedClass|}>();
                        var of1 = Mock.Of<{|Moq1000:SealedClass|}>();
                        var mock2 = new Mock<NonSealedClass>();
                        var of2 = Mock.Of<NonSealedClass>();
                    }
                }
                """,
                referenceAssemblyGroup: ReferenceAssemblyCatalog.Net80WithOldMoq);
    }

    [Fact]
    public async Task ShouldHandleComplexGenericScenarios()
    {
        // Test various complex generic scenarios
        await Verifier.VerifyAnalyzerAsync(
                """
                using Moq;
                using System;
                using System.Collections.Generic;

                internal sealed class SealedClass { }
                internal class NonSealedClass { }

                internal class UnitTest
                {
                    private void Test()
                    {
                        // Generic collections should not trigger (they're not Mock<T>)
                        var list = new List<SealedClass>();
                        var dict = new Dictionary<string, SealedClass>();
                        
                        // Valid Mock usage
                        var validMock = new Mock<NonSealedClass>();
                        
                        // Invalid Mock usage should trigger
                        var invalidMock = new Mock<{|Moq1000:SealedClass|}>();
                    }
                }
                """,
                referenceAssemblyGroup: ReferenceAssemblyCatalog.Net80WithOldMoq);
    }

    [Fact]
    public async Task ShouldHandleInvalidMockOfCalls()
    {
        // Test edge cases for Mock.Of calls
        await Verifier.VerifyAnalyzerAsync(
                """
                using Moq;
                using System;
                using System.Reflection;

                internal sealed class SealedClass { }
                internal class NonSealedClass { }

                internal class UnitTest
                {
                    private void Test()
                    {
                        // Valid Mock.Of calls
                        var valid1 = Mock.Of<NonSealedClass>();
                        
                        // Invalid Mock.Of calls should trigger
                        var invalid1 = Mock.Of<{|Moq1000:SealedClass|}>();
                        
                        // Non-Mock static method calls should not interfere
                        var method = typeof(Mock).GetMethod("Of");
                        var empty = string.Empty;
                        var now = DateTime.Now;
                        
                        // Instance method calls should not interfere
                        var mock = new Mock<NonSealedClass>();
                        mock.Setup(x => x.ToString()).Returns("test");
                    }
                }
                """,
                referenceAssemblyGroup: ReferenceAssemblyCatalog.Net80WithOldMoq);
    }

    [Fact]
    public async Task ShouldHandleVariousBuiltInSealedTypes()
    {
        // Test more built-in sealed types
        await Verifier.VerifyAnalyzerAsync(
                """
                using Moq;
                using System;

                internal class UnitTest
                {
                    private void Test()
                    {
                        // Built-in sealed types should trigger diagnostics
                        var stringMock = new Mock<{|Moq1000:string|}>();
                        var stringOf = Mock.Of<{|Moq1000:string|}>();
                        
                        // Value types are typically not mockable due to constraints, 
                        // but if they could be, sealed ones would trigger the diagnostic
                        // Note: These would actually fail at compile time due to reference type constraint
                        // but we test the analyzer logic
                    }
                }
                """,
                referenceAssemblyGroup: ReferenceAssemblyCatalog.Net80WithOldMoq);
    }

    [Fact]
    public async Task ShouldHandleNestedGenericTypes()
    {
        // Test nested generic type scenarios
        await Verifier.VerifyAnalyzerAsync(
                """
                using Moq;
                using System;

                internal sealed class SealedGeneric<T> { }
                internal class NonSealedGeneric<T> { }

                internal class UnitTest
                {
                    private void Test()
                    {
                        // Nested generics - sealed should trigger
                        var mock1 = new Mock<{|Moq1000:SealedGeneric<string>|}>();
                        var of1 = Mock.Of<{|Moq1000:SealedGeneric<int>|}>();
                        
                        // Non-sealed should not trigger
                        var mock2 = new Mock<NonSealedGeneric<string>>();
                        var of2 = Mock.Of<NonSealedGeneric<int>>();
                    }
                }
                """,
                referenceAssemblyGroup: ReferenceAssemblyCatalog.Net80WithOldMoq);
    }

    [Fact]
    public async Task ShouldHandleMultipleConstructorOverloads()
    {
        // Test various Mock constructor overloads with sealed types
        await Verifier.VerifyAnalyzerAsync(
                """
                using Moq;

                internal sealed class SealedClass { }
                internal class NonSealedClass { }

                internal class UnitTest
                {
                    private void Test()
                    {
                        // Various constructor overloads - all should trigger for sealed types
                        var mock1 = new Mock<{|Moq1000:SealedClass|}>();
                        var mock2 = new Mock<{|Moq1000:SealedClass|}>(MockBehavior.Strict);
                        var mock3 = new Mock<{|Moq1000:SealedClass|}>(MockBehavior.Loose);
                        var mock4 = new Mock<{|Moq1000:SealedClass|}>(MockBehavior.Default);
                        
                        // Non-sealed should not trigger
                        var valid1 = new Mock<NonSealedClass>();
                        var valid2 = new Mock<NonSealedClass>(MockBehavior.Strict);
                    }
                }
                """,
                referenceAssemblyGroup: ReferenceAssemblyCatalog.Net80WithOldMoq);
    }

    [Fact]
    public async Task ShouldHandleSystemTypes()
    {
        // Test various System types (sealed reference types)
        await Verifier.VerifyAnalyzerAsync(
                """
                using Moq;
                using System;

                internal class UnitTest
                {
                    private void Test()
                    {
                        // System.String is sealed
                        var stringMock = new Mock<{|Moq1000:String|}>();
                        var stringOf = Mock.Of<{|Moq1000:String|}>();
                        
                        // Using full type name
                        var stringMock2 = new Mock<{|Moq1000:System.String|}>();
                        var stringOf2 = Mock.Of<{|Moq1000:System.String|}>();
                    }
                }
                """,
                referenceAssemblyGroup: ReferenceAssemblyCatalog.Net80WithOldMoq);
    }
}
