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

    [Theory]
    [MemberData(nameof(MoqReferenceAssemblyGroups))]
    public async Task MakesNonVirtualPropertyVirtual(string referenceAssemblyGroup)
    {
        const string test = """
        using Moq;

        public class MyClass
        {
            public string MyProperty { get; set; }
        }

        public class MyTest
        {
            public void Test()
            {
                var mock = new Mock<MyClass>();
                mock.Verify(x => {|Moq1210:x.MyProperty|});
            }
        }
        """;

        const string fixtest = """
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

        await Verify.VerifyCodeFixAsync(test, fixtest, referenceAssemblyGroup);
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

        public class MyTest
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
    [MemberData(nameof(MoqReferenceAssemblyGroups))]
    public async Task PreservesModifiersAttributesAndDocumentation(string referenceAssemblyGroup)
    {
        const string test = """
        using Moq;
        using System;

        public class MyClass
        {
            /// <summary>Some documentation.</summary>
            [Obsolete]
            protected internal string MyProperty { get; set; }
        }

        public class MyTest
        {
            public void Test()
            {
                var mock = new Mock<MyClass>();
                mock.Verify(x => {|Moq1210:x.MyProperty|});
            }
        }
        """;

        const string fixtest = """
        using Moq;
        using System;

        public class MyClass
        {
            /// <summary>Some documentation.</summary>
            [Obsolete]
            protected internal virtual string MyProperty { get; set; }
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

        await Verify.VerifyCodeFixAsync(test, fixtest, referenceAssemblyGroup);
    }

    [Theory]
    [MemberData(nameof(MoqReferenceAssemblyGroups))]
    public async Task NoFixForSealedMembers(string referenceAssemblyGroup)
    {
        const string test = """
        using Moq;

        public class MyClass
        {
            public sealed override string ToString() => "";
        }

        public class MyTest
        {
            public void Test()
            {
                var mock = new Mock<MyClass>();
                mock.Verify(x => {|Moq1210:x.ToString()|});
            }
        }
        """;

        const string fixtest = """
        using Moq;

        public class MyClass
        {
            public sealed override string ToString() => "";
        }

        public class MyTest
        {
            public void Test()
            {
                var mock = new Mock<MyClass>();
                mock.Verify(x => x.ToString());
            }
        }
        """;

        await Verify.VerifyCodeFixAsync(test, fixtest, referenceAssemblyGroup);
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
