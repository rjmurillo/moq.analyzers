using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.ConstructorArgumentsShouldMatchAnalyzer>;

namespace Moq.Analyzers.Test;

public class ConstructorArgumentsShouldMatchAnalyzerTests
{
    public static IEnumerable<object[]> DelegateTestData()
    {
        return new object[][]
        {
            ["""new Mock<DelegateWithParam>();"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(DelegateTestData))]
    public async Task ShouldAnalyzeDelegate(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
              {{@namespace}}

              public delegate void DelegateWithParam(int a);

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
            ["""Mock.Of<Foo>{|Moq1002:()|};"""],

            // Repository versions
            ["""var repository = new MockRepository(MockBehavior.Default) { DefaultValue = DefaultValue.Empty }; var fooMock = repository.Create<Foo>{|Moq1002:(MockBehavior.Default)|}; repository.Verify();"""],
            ["""var repository = new MockRepository(MockBehavior.Default) { DefaultValue = DefaultValue.Empty }; var fooMock = repository.Create<Foo>{|Moq1002:()|}; repository.Verify();"""],
            ["""var repository = new MockRepository(MockBehavior.Default) { DefaultValue = DefaultValue.Empty }; var fooMock = repository.Create<Foo>(false, 42); repository.Verify();"""],
            ["""var repository = new MockRepository(MockBehavior.Default) { DefaultValue = DefaultValue.Empty }; var fooMock = repository.Create<Foo>(MockBehavior.Default, false, 42); repository.Verify();"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> InterfaceTestData()
    {
        IEnumerable<object[]> all = new object[][]
        {
            // Regular code (to make sure we bail out early)
            ["""IFoo foo;"""],

            // Regular
            ["""new Mock<IFoo>(MockBehavior.Default);"""],
            ["""new Mock<IFoo>();"""],
            ["""new Mock<IFoo>{|Moq1001:(MockBehavior.Default, 42)|};"""],
            ["""new Mock<IFoo>{|Moq1001:(42)|};"""],

            // LINQ
            ["""Mock.Of<IFoo>();"""],

            // Repository
            ["""var repository = new MockRepository(MockBehavior.Default) { DefaultValue = DefaultValue.Empty }; var fooMock = repository.Create<IFoo>(MockBehavior.Default); repository.Verify();"""],
            ["""var repository = new MockRepository(MockBehavior.Default) { DefaultValue = DefaultValue.Empty }; var fooMock = repository.Create<IFoo>(); repository.Verify();"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();

        IEnumerable<object[]> @new = new object[][]
        {
            // LINQ
            ["""Mock.Of<IFoo>(MockBehavior.Default);"""],   // This is only available in newer versions of
        }.WithNamespaces().WithNewMoqReferenceAssemblyGroups();

        return all.Union(@new);
    }

    public static IEnumerable<object[]> ClassWithDefaultCtorTestData()
    {
        return new object[][]
        {
            ["""new Mock<ClassDefaultCtor>(MockBehavior.Default);"""],
            ["""new Mock<ClassDefaultCtor>();"""],

            ["""Mock.Of<ClassDefaultCtor>(MockBehavior.Default);"""],
            ["""Mock.Of<ClassDefaultCtor>();"""],
            ["""Mock.Of<ClassDefaultCtor>(m => true);"""],
            ["""Mock.Of<ClassDefaultCtor>(m => true, MockBehavior.Default);"""],

            ["""var repository = new MockRepository(MockBehavior.Default) { DefaultValue = DefaultValue.Empty }; var fooMock = repository.Create<ClassDefaultCtor>(MockBehavior.Default); repository.Verify();"""],
            ["""var repository = new MockRepository(MockBehavior.Default) { DefaultValue = DefaultValue.Empty }; var fooMock = repository.Create<ClassDefaultCtor>(); repository.Verify();"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> ClassWithDefaultParamCtorTestData()
    {
        var all = new object[][]
        {
            // Regular
            ["""new Mock<ClassWithDefaultParamCtor>(MockBehavior.Default);"""],
            ["""new Mock<ClassWithDefaultParamCtor>();"""],
            ["""new Mock<ClassWithDefaultParamCtor>(MockBehavior.Default, 21);"""],
            ["""new Mock<ClassWithDefaultParamCtor>(21);"""],

            // LINQ
            ["""Mock.Of<ClassWithDefaultParamCtor>();"""],

            // Repository
            ["""var repository = new MockRepository(MockBehavior.Default) { DefaultValue = DefaultValue.Empty }; var fooMock = repository.Create<ClassWithDefaultParamCtor>(MockBehavior.Default); repository.Verify();"""],
            ["""var repository = new MockRepository(MockBehavior.Default) { DefaultValue = DefaultValue.Empty }; var fooMock = repository.Create<ClassWithDefaultParamCtor>(); repository.Verify();"""],
            ["""var repository = new MockRepository(MockBehavior.Default) { DefaultValue = DefaultValue.Empty }; var fooMock = repository.Create<ClassWithDefaultParamCtor>(MockBehavior.Default, 21); repository.Verify();"""],
            ["""var repository = new MockRepository(MockBehavior.Default) { DefaultValue = DefaultValue.Empty }; var fooMock = repository.Create<ClassWithDefaultParamCtor>(21); repository.Verify();"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();

        var @new = new object[][]
        {
            // LINQ
            ["""Mock.Of<ClassWithDefaultParamCtor>(MockBehavior.Default);"""],
        }.WithNamespaces().WithNewMoqReferenceAssemblyGroups();

        return all.Union(@new);
    }

    [Theory]
    [MemberData(nameof(ClassWithDefaultParamCtorTestData))]
    public async Task ShouldAnalyzeClassWithDefaultParamCtor(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
              {{@namespace}}

              internal class ClassWithDefaultParamCtor
              {
                  public ClassWithDefaultParamCtor(int a = 42) { }
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

    [Theory]
    [MemberData(nameof(ClassWithDefaultCtorTestData))]
    public async Task ShouldAnalyzeClassWithDefaultCtor(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
              {{@namespace}}

              public class ClassDefaultCtor
              {
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

    [Theory]
    [MemberData(nameof(InterfaceTestData))]
    public async Task ShouldAnalyzeInterface(string referenceAssemblyGroup, string @namespace, string mock)
    {
        await Verifier.VerifyAnalyzerAsync(
                $$"""
                {{@namespace}}

                public interface IFoo
                {
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
