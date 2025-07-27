using Verifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.VerifyShouldBeUsedOnlyForOverridableMembersAnalyzer>;
using Verify = Moq.Analyzers.Test.Helpers.CodeFixVerifier<Moq.Analyzers.VerifyShouldBeUsedOnlyForOverridableMembersAnalyzer, Moq.Analyzers.VerifyOverridableMembersFixer>;

namespace Moq.Analyzers.Test;

public partial class VerifyShouldBeUsedOnlyForOverridableMembersAnalyzerTests(ITestOutputHelper output)
{
    public static IEnumerable<object[]> TestData()
    {
        IEnumerable<object[]> both = new object[][]
        {
            // Valid in both versions, but flagged as error for non-virtual/invalid targets
            ["""{|Moq1210:new Mock<BaseSampleClass>().Verify(x => x.Calculate())|};"""],
            ["""{|Moq1210:new Mock<SampleClass>().Verify(x => x.Property)|};"""],
            ["""{|Moq1210:new Mock<SampleClass>().Verify(x => x.Calculate(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))|};"""],
            ["""new Mock<BaseSampleClass>().Verify(x => x.Calculate(It.IsAny<int>(), It.IsAny<int>()));"""],

            // VerifyGet tests
            ["""{|Moq1210:new Mock<SampleClass>().VerifyGet(x => x.Property)|};"""],
            ["""new Mock<ISampleInterface>().VerifyGet(x => x.TestProperty);"""],

            // VerifyNoOtherCalls should not trigger any diagnostics
            ["""new Mock<SampleClass>().VerifyNoOtherCalls();"""],
            ["""new Mock<ISampleInterface>().VerifyNoOtherCalls();"""],

            // Valid verifications should not trigger diagnostics
            ["""new Mock<SampleClass>().Verify(x => x.DoSth());"""],
            ["""new Mock<ISampleInterface>().Verify(x => x.TestProperty);"""],
            ["""{|Moq1210:new Mock<SampleClass>().Verify(x => x.Field)|};"""],
            ["""{|Moq1210:new Mock<SampleClassWithNonVirtualIndexer>().Verify(x => x[0])|};"""],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();

        IEnumerable<object[]> newMoqOnly = new object[][]
        {
            // VerifySet tests - only available in new Moq versions
            // VerifySet uses Action<T> syntax, not Expression<Func<T, ...>>
            ["""{|Moq1210:new Mock<SampleClass>().VerifySet(x => { x.Property = It.IsAny<int>(); })|};"""],
            ["""new Mock<ISampleInterface>().VerifySet(x => { x.TestProperty = It.IsAny<string>(); });"""],
        }.WithNamespaces().WithNewMoqReferenceAssemblyGroups();

        return both.Concat(newMoqOnly);
    }

    public static IEnumerable<object[]> MoqReferenceAssemblyGroups() =>
        new List<object[]>
        {
            new object[] { ReferenceAssemblyCatalog.Net80WithOldMoq },
            new object[] { ReferenceAssemblyCatalog.Net80WithNewMoq },
        };

    public static IEnumerable<object[]> MakesNonVirtualPropertyVirtualData()
    {
        return new object[][]
        {
            [
                """public int MyProperty { get; set; }""",
                """public virtual int MyProperty { get; set; }""",
            ],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> MakesNonVirtualMethodVirtualData()
    {
        return new object[][]
        {
            [
                """public int MyMethod() => 0;""",
                """public virtual int MyMethod() => 0;""",
            ],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> PreservesModifiersAttributesAndDocumentationData()
    {
        return new object[][]
        {
            [
                """
                /// <summary>Some documentation.</summary>
                [Obsolete]
                protected internal string MyProperty { get; set; }
                """,
                """
                /// <summary>Some documentation.</summary>
                [Obsolete]
                protected internal virtual string MyProperty { get; set; }
                """,
                "MyProperty",
            ],
            [
                """
                [Obsolete]
                public int MyMethod() => 0;
                """,
                """
                [Obsolete]
                public virtual int MyMethod() => 0;
                """,
                "MyMethod()",
            ],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> NoFixForSealedMembersData()
    {
        return new object[][]
        {
            [
                """public sealed override string ToString() => "";""",
                """public sealed override string ToString() => "";""",
            ],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldAnalyzeVerifyForOverridableMembers(string referenceAssemblyGroup, string @namespace, string mock)
    {
        string source = $$"""
                                {{@namespace}}

                                public interface ISampleInterface
                                {
                                    int Calculate(int a, int b);
                                    string TestProperty { get; set; }
                                }

                                public class SampleClassWithVirtualIndexer { public virtual int this[int i] { get => 0; set { } } }
                                public class SampleClassWithNonVirtualIndexer { public int this[int i] { get => 0; set { } } }
                                public interface IExplicitInterface { void ExplicitMethod(); }
                                public class SampleClassWithStaticMembers { public static int StaticField; public const int ConstField = 42; public static readonly int ReadonlyField = 42; public static void StaticMethod() { } }

                                public abstract class BaseSampleClass
                                {
                                    public int Calculate() => 0;
                                    public abstract int Calculate(int a, int b);
                                    public abstract int Calculate(int a, int b, int c);
                                }

                                public class SampleClass : BaseSampleClass
                                {
                                    public override int Calculate(int a, int b) => 0;
                                    public sealed override int Calculate(int a, int b, int c) => 0;
                                    public virtual int DoSth() => 0;
                                    public int Property { get; set; }
                                    public int Field;
                                    public event EventHandler? TestEvent;
                                }

                                public class SampleClassWithVirtualEvent
                                {
                                    public virtual event EventHandler? TestEvent;
                                }

                                internal class UnitTest
                                {
                                    private void Test()
                                    {
                                        {{mock}}
                                    }
                                }
                                """;

        output.WriteLine(source);

        await Verifier.VerifyAnalyzerAsync(
                source,
                referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(DoppelgangerTestHelper.GetAllCustomMockData), MemberType = typeof(DoppelgangerTestHelper))]
    public async Task ShouldPassIfCustomMockClassIsUsed(string mockCode)
    {
        await Verifier.VerifyAnalyzerAsync(
            DoppelgangerTestHelper.CreateTestCode(mockCode),
            ReferenceAssemblyCatalog.Net80WithNewMoq);
    }

    [Theory]
    [MemberData(nameof(MakesNonVirtualPropertyVirtualData))]
    public async Task MakesNonVirtualPropertyVirtual(string referenceAssemblyGroup, string @namespace, string brokenCode, string fixedCode)
    {
        static string Template(string ns, string code) =>
            $$"""
              {{ns}}

              public class MyClass
              {
                  {{code}}
              }

              public class MyTest : MyClass
              {
                  public void Test()
                  {
                      var mock = new Mock<MyClass>();
                      {|Moq1210:mock.Verify(x => x.MyProperty)|};
                  }
              }
              """;

        string originalSource = Template(@namespace, brokenCode);
        string fixedSource = Template(@namespace, fixedCode);

        output.WriteLine("Original:");
        output.WriteLine(originalSource);
        output.WriteLine(string.Empty);
        output.WriteLine("Fixed:");
        output.WriteLine(fixedSource);

        await Verify.VerifyCodeFixAsync(originalSource, fixedSource, referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(MakesNonVirtualMethodVirtualData))]
    public async Task MakesNonVirtualMethodVirtual(string referenceAssemblyGroup, string @namespace, string brokenCode, string fixedCode)
    {
        static string Template(string ns, string code) =>
        $$"""
        {{ns}}

        public class MyClass
        {
            {{code}}
        }

        public class MyTest : MyClass
        {
            public void Test()
            {
                var mock = new Mock<MyClass>();
                {|Moq1210:mock.Verify(x => x.MyMethod())|};
            }
        }
        """;

        string originalSource = Template(@namespace, brokenCode);
        string fixedSource = Template(@namespace, fixedCode);

        output.WriteLine("Original:");
        output.WriteLine(originalSource);
        output.WriteLine(string.Empty);
        output.WriteLine("Fixed:");
        output.WriteLine(fixedSource);

        await Verify.VerifyCodeFixAsync(originalSource, fixedSource, referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(PreservesModifiersAttributesAndDocumentationData))]
    public async Task PreservesModifiersAttributesAndDocumentation(string referenceAssemblyGroup, string @namespace, string brokenCode, string fixedCode, string invocation)
    {
        static string Template(string ns, string code, string invocation) =>
            $$"""
            {{ns}}

            public class MyClass
            {
                {{code}}
            }

            public class MyTest : MyClass
            {
                public void Test()
                {
                    var mock = new Mock<MyClass>();
                    {|Moq1210:mock.Verify(x => x.{{invocation}})|};
                }
            }
            """;

        string originalSource = Template(@namespace, brokenCode, invocation);
        string fixedSource = Template(@namespace, fixedCode, invocation);

        output.WriteLine("Original:");
        output.WriteLine(originalSource);
        output.WriteLine(string.Empty);
        output.WriteLine("Fixed:");
        output.WriteLine(fixedSource);

        await Verify.VerifyCodeFixAsync(originalSource, fixedSource, referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(NoFixForSealedMembersData))]
    public async Task NoFixForSealedMembers(string referenceAssemblyGroup, string @namespace, string brokenCode, string fixedCode)
    {
        static string Template(string ns, string code) =>
            $$"""
              {{ns}}

              public class MyClass
              {
                  {{code}}
              }

              public class MyTest : MyClass
              {
                  public void Test()
                  {
                      var mock = new Mock<MyClass>();
                      {|Moq1210:mock.Verify(x => x.ToString())|};
                  }
              }
              """;

        string originalSource = Template(@namespace, brokenCode);
        string fixedSource = Template(@namespace, fixedCode);

        output.WriteLine("Original:");
        output.WriteLine(originalSource);
        output.WriteLine(string.Empty);
        output.WriteLine("Fixed:");
        output.WriteLine(fixedSource);

        await Verify.VerifyCodeFixAsync(originalSource, fixedSource, referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(MoqReferenceAssemblyGroups))]
    public async Task NoFixForInterfaceMembers(string referenceAssemblyGroup)
    {
        string test = """
        using Moq;

        public interface IMyInterface
        {
            string MyProperty { get; set; }
        }

        public class MyTest
        {
            public void Test()
            {
                var mock = new Mock<IMyInterface>();
                mock.Verify(x => x.MyProperty);
            }
        }
        """;

        await Verify.VerifyCodeFixAsync(test, test, referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(MoqReferenceAssemblyGroups))]
    public async Task NoFixForAbstractMembers(string referenceAssemblyGroup)
    {
        string test = """
        using Moq;

        public abstract class MyClass
        {
            public abstract string MyProperty { get; set; }
        }

        public class MyTest
        {
            public void Test()
            {
                var mock = new Mock<MyClass>();
                mock.Verify(x => x.MyProperty);
            }
        }
        """;

        await Verify.VerifyCodeFixAsync(test, test, referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(MoqReferenceAssemblyGroups))]
    public async Task NoFixForVirtualMembers(string referenceAssemblyGroup)
    {
        string test = """
        using Moq;

        public class MyClass
        {
            public virtual string MyProperty { get; set; }
        }

        public class MyTest
        {
            public void Test()
            {
                var mock = new Mock<MyClass>();
                mock.Verify(x => x.MyProperty);
            }
        }
        """;

        await Verify.VerifyCodeFixAsync(test, test, referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(MoqReferenceAssemblyGroups))]
    public async Task NoFixForExplicitInterfaceImplementation(string referenceAssemblyGroup)
    {
        string test = """

        public interface IMyInterface
        {
            void MyMethod();
        }

        public class MyClass : IMyInterface
        {
            void IMyInterface.MyMethod() { }
        }

        public class MyTest
        {
            public void Test()
            {
                var mock = new Mock<MyClass>();
                // This is not a valid Moq pattern, but ensure analyzer/fixer does not crash or offer a fix
                mock.Verify(x => ((IMyInterface)x).MyMethod());
            }
        }
        """;

        await Verify.VerifyCodeFixAsync(test, test, referenceAssemblyGroup);
    }
}
