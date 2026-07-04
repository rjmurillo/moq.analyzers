using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.ConstructorArgumentsShouldMatchAnalyzer>;

namespace Moq.Analyzers.Test;

public partial class ConstructorArgumentsShouldMatchAnalyzerTests
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0051:Method is too long", Justification = "All test cases")]
    public static IEnumerable<object[]> TestData()
    {
        return new object[][]
        {
            // This is allowed because there's a ctor with a single params parameter
            ["""new Mock<ClassWithParams>(MockBehavior.Default);"""],
            ["""new Mock<ClassWithParams>();"""],
            ["""new Mock<ClassWithParams>(MockBehavior.Default, DateTime.Now, DateTime.Now);"""],
            ["""new Mock<ClassWithParams>(DateTime.Now, DateTime.Now);"""],
            ["""new Mock<ClassWithParams>(MockBehavior.Default, "42", DateTime.Now, DateTime.Now);"""],
            ["""new Mock<ClassWithParams>("42", DateTime.Now, DateTime.Now);"""],

            ["""new Mock<Foo>(false, 0);"""],
            ["""new Mock<Foo>(MockBehavior.Default, true, 1);"""],

            ["""new Mock<Foo>(MockBehavior.Default, new List<string>());"""],
            ["""new Mock<Foo>(new List<string>());"""],
            ["""new Mock<Foo>(MockBehavior.Default, new List<string>(), "8");"""],
            ["""new Mock<Foo>(new List<string>(), "7");"""],
            ["""new Mock<Foo>{|Moq1002:(1, true)|};"""],
            ["""new Mock<Foo>{|Moq1002:(MockBehavior.Default, 2, true)|};"""],
            ["""new Mock<Foo>{|Moq1002:("1", 3)|};"""],
            ["""new Mock<Foo>{|Moq1002:(MockBehavior.Default, "2", 6)|};"""],
            ["""new Mock<Foo>{|Moq1002:(new int[] { 1, 2, 3 })|};"""],
            ["""new Mock<Foo>{|Moq1002:(MockBehavior.Default, 4, true)|};"""],

            ["""new Mock<ClassWithDefaultParamCtor>(MockBehavior.Default);"""],
            ["""new Mock<ClassWithDefaultParamCtor>();"""],

            ["""new Mock<ClassWithRequiredParamCtor>{|Moq1002:(MockBehavior.Default)|};"""],
            ["""new Mock<ClassWithRequiredParamCtor>{|Moq1002:()|};"""],

            ["""new Mock<AbstractGenericClassDefaultCtor<object>>(MockBehavior.Default);"""],
            ["""new Mock<AbstractGenericClassDefaultCtor<object>>();"""],
            ["""new Mock<AbstractGenericClassDefaultCtor<object>>{|Moq1002:(42)|};"""],

            ["""new Mock<AbstractClassDefaultCtor>(MockBehavior.Default);"""],
            ["""new Mock<AbstractClassDefaultCtor>();"""],
            ["""new Mock<AbstractClassDefaultCtor>{|Moq1002:(MockBehavior.Default, 42)|};"""],
            ["""new Mock<AbstractClassDefaultCtor>{|Moq1002:(42)|};"""],

            ["""new Mock<AbstractClassWithDefaultParamCtor>(MockBehavior.Default);"""],
            ["""new Mock<AbstractClassWithDefaultParamCtor>();"""],
            ["""new Mock<AbstractClassWithDefaultParamCtor>(MockBehavior.Default, 42);"""],
            ["""new Mock<AbstractClassWithDefaultParamCtor>(42);"""],
            ["""new Mock<AbstractClassWithDefaultParamCtor>{|Moq1002:(MockBehavior.Default, "42")|};"""],
            ["""new Mock<AbstractClassWithDefaultParamCtor>{|Moq1002:("42")|};"""],

            ["""new Mock<AbstractClassWithCtor>(MockBehavior.Default, 42);"""],
            ["""new Mock<AbstractClassWithCtor>(42);"""],
            ["""new Mock<AbstractClassWithCtor>(MockBehavior.Default, 42, "42");"""],
            ["""new Mock<AbstractClassWithCtor>(42, "42");"""],
            ["""new Mock<AbstractClassWithCtor>{|Moq1002:("42")|};"""],
            ["""new Mock<AbstractClassWithCtor>{|Moq1002:("42", 42)|};"""],
            ["""new Mock<AbstractClassWithCtor>{|Moq1002:(MockBehavior.Default)|};"""],
            ["""new Mock<AbstractClassWithCtor>{|Moq1002:()|};"""],

            ["""new Mock<AbstractGenericClassWithCtor<object>>(MockBehavior.Default, 42);"""],
            ["""new Mock<AbstractGenericClassWithCtor<object>>(42);"""],
            ["""new Mock<AbstractGenericClassWithCtor<object>>{|Moq1002:("42")|};"""],
            ["""new Mock<AbstractGenericClassWithCtor<object>>{|Moq1002:("42", 42)|};"""],
            ["""new Mock<AbstractGenericClassWithCtor<object>>{|Moq1002:()|};"""],
            ["""new Mock<AbstractGenericClassWithCtor<object>>{|Moq1002:(MockBehavior.Default)|};"""],

            // LINQ versions don't have capacity to specify ctors, so we can't use
            // types that don't have a default ctor
            ["""Mock.Of<ClassDefaultCtor>();"""],
            ["""Mock.Of<ClassWithDefaultParamCtor>();"""],

            // Squiggle moves out further because there are no arguments to scope
            ["""{|Moq1002:Mock.Of<Foo>()|};"""],

            // Repository versions
            ["""var repository = new MockRepository(MockBehavior.Default) { DefaultValue = DefaultValue.Empty }; var fooMock = repository.Create<Foo>{|Moq1002:(MockBehavior.Default)|}; repository.Verify();"""],
            ["""var repository = new MockRepository(MockBehavior.Default) { DefaultValue = DefaultValue.Empty }; var fooMock = repository.Create<Foo>{|Moq1002:()|}; repository.Verify();"""],
            ["""var repository = new MockRepository(MockBehavior.Default) { DefaultValue = DefaultValue.Empty }; var fooMock = repository.Create<Foo>(false, 42); repository.Verify();"""],
            ["""var repository = new MockRepository(MockBehavior.Default) { DefaultValue = DefaultValue.Empty }; var fooMock = repository.Create<Foo>(MockBehavior.Default, false, 42); repository.Verify();"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0051:Method is too long", Justification = "All test cases")]
    public static IEnumerable<object[]> ParamsCollectionTestData()
    {
        return new object[][]
        {
            ["List<int>", """new Mock<ClassWithCollectionParams>(1, 2, 3);"""],
            ["List<int>", """new Mock<ClassWithCollectionParams>{|Moq1002:("a", "b")|};"""],
            ["List<int>", """new Mock<ClassWithCollectionParams>();"""],

            ["IEnumerable<int>", """new Mock<ClassWithCollectionParams>(1, 2, 3);"""],
            ["IEnumerable<int>", """new Mock<ClassWithCollectionParams>{|Moq1002:("a", "b")|};"""],

            ["IReadOnlyList<int>", """new Mock<ClassWithCollectionParams>(1, 2, 3);"""],
            ["IReadOnlyList<int>", """new Mock<ClassWithCollectionParams>{|Moq1002:("a", "b")|};"""],

            ["Span<int>", """new Mock<ClassWithCollectionParams>(1, 2, 3);"""],
            ["Span<int>", """new Mock<ClassWithCollectionParams>{|Moq1002:("a", "b")|};"""],
        }.WithNamespaces();
    }

    // A constructor whose params parameter is a C# 13 params collection (e.g. `params ReadOnlySpan<T>`)
    // must not crash the analyzer (AD0001/InvalidCastException) and must still correctly validate
    // argument types against the collection's element type.
    [Fact]
    public async Task ShouldNotCrashOnParamsReadOnlySpanConstructor()
    {
        await Verifier.VerifyAnalyzerAsync(
                """
                using System;
                using Moq;

                internal class ClassWithReadOnlySpanParams
                {
                    public ClassWithReadOnlySpanParams(params ReadOnlySpan<string> values) { }
                }

                internal class UnitTest
                {
                    private void Test()
                    {
                        var mockOk = new Mock<ClassWithReadOnlySpanParams>("a", "b");
                        var mockBad = new Mock<ClassWithReadOnlySpanParams>{|Moq1002:(1, 2)|};
                    }
                }
                """,
                ReferenceAssemblyCatalog.Net80WithNewMoq);
    }

    // A constructor whose params parameter uses any of the C# 13 params collection types
    // (List<T>, IEnumerable<T>, IReadOnlyList<T>, Span<T>) must be analyzed the same way as a
    // `params T[]` constructor: matching argument types are accepted, mismatched ones raise Moq1002.
    [Theory]
    [MemberData(nameof(ParamsCollectionTestData))]
    public async Task ShouldAnalyzeParamsCollectionConstructorArguments(string @namespace, string collectionType, string mock)
    {
        await Verifier.VerifyAnalyzerAsync(
                $$"""
                {{@namespace}}

                internal class ClassWithCollectionParams
                {
                    public ClassWithCollectionParams(params {{collectionType}} items) { }
                }

                internal class UnitTest
                {
                    private void Test()
                    {
                        {{mock}}
                    }
                }
                """,
                ReferenceAssemblyCatalog.Net80WithNewMoq);
    }

    // A custom params collection type resolves its element type from the IEnumerable<T> it
    // implements — including a non-generic collection (MyIntCollection : IEnumerable<int>) and a
    // generic wrapper whose own type argument is unrelated to the element type
    // (MyTaggedCollection<TTag> : IEnumerable<string>).
    [Theory]
    [InlineData("MyIntCollection", """new Mock<ClassWithCustomParams>(1, 2, 3);""")]
    [InlineData("MyIntCollection", """new Mock<ClassWithCustomParams>{|Moq1002:("a", "b")|};""")]
    [InlineData("MyTaggedCollection<int>", """new Mock<ClassWithCustomParams>("a", "b");""")]
    [InlineData("MyTaggedCollection<int>", """new Mock<ClassWithCustomParams>{|Moq1002:(1, 2)|};""")]
    public async Task ShouldAnalyzeCustomParamsCollectionConstructorArguments(string collectionType, string mock)
    {
        await Verifier.VerifyAnalyzerAsync(
                $$"""
                using System.Collections;
                using System.Collections.Generic;
                using Moq;

                internal class MyIntCollection : IEnumerable<int>
                {
                    private readonly List<int> _items = new();
                    public void Add(int item) => _items.Add(item);
                    public IEnumerator<int> GetEnumerator() => _items.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
                }

                internal class MyTaggedCollection<TTag> : IEnumerable<string>
                {
                    private readonly List<string> _items = new();
                    public void Add(string item) => _items.Add(item);
                    public IEnumerator<string> GetEnumerator() => _items.GetEnumerator();
                    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
                }

                internal class ClassWithCustomParams
                {
                    public ClassWithCustomParams(params {{collectionType}} items) { }
                }

                internal class UnitTest
                {
                    private void Test()
                    {
                        {{mock}}
                    }
                }
                """,
                ReferenceAssemblyCatalog.Net80WithNewMoq);
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldAnalyzeConstructorArguments(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await Verifier.VerifyAnalyzerAsync(
                $$"""
                {{@namespace}}

                internal interface IFoo
                {
                }

                internal class Foo
                {
                    public Foo(bool b, int i) { }
                    public Foo(List<string> l, string s = "A") { }
                }

                internal class ClassDefaultCtor
                {
                }

                internal class ClassWithDefaultParamCtor
                {
                    public ClassWithDefaultParamCtor(int a = 42) { }
                }

                internal class ClassWithRequiredParamCtor
                {
                    public ClassWithRequiredParamCtor(int a) { }
                }

                internal class ClassWithParams
                {
                    public ClassWithParams(params DateTime[] dates) { }
                    public ClassWithParams(string s, params DateTime[] dates) { }
                }

                internal abstract class AbstractClassDefaultCtor
                {
                }

                internal abstract class AbstractGenericClassDefaultCtor<T>
                {
                }

                internal abstract class AbstractClassWithDefaultParamCtor
                {
                    protected AbstractClassWithDefaultParamCtor(int a = 42) { }
                }

                internal abstract class AbstractClassWithCtor
                {
                    protected AbstractClassWithCtor(int a) { }
                    protected AbstractClassWithCtor(int a, string b) { }
                }

                internal abstract class AbstractGenericClassWithCtor<T>
                {
                    protected AbstractGenericClassWithCtor(int a) { }
                    protected AbstractGenericClassWithCtor(int a, string b) { }
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
}
