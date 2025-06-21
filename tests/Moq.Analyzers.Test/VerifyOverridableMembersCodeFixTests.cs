using Verify = Moq.Analyzers.Test.Helpers.CodeFixVerifier<Moq.Analyzers.VerifyShouldBeUsedOnlyForOverridableMembersAnalyzer, Moq.Analyzers.VerifyOverridableMembersFixer>;

namespace Moq.Analyzers.Test;

public class VerifyOverridableMembersCodeFixTests(ITestOutputHelper output)
{
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

        string o = Template(@namespace, brokenCode);
        string f = Template(@namespace, fixedCode);

        output.WriteLine("Original:");
        output.WriteLine(o);
        output.WriteLine(string.Empty);
        output.WriteLine("Fixed:");
        output.WriteLine(f);

        await Verify.VerifyCodeFixAsync(o, f, referenceAssemblyGroup);
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

        string o = Template(@namespace, brokenCode);
        string f = Template(@namespace, fixedCode);

        output.WriteLine("Original:");
        output.WriteLine(o);
        output.WriteLine(string.Empty);
        output.WriteLine("Fixed:");
        output.WriteLine(f);

        await Verify.VerifyCodeFixAsync(o, f, referenceAssemblyGroup);
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
}
