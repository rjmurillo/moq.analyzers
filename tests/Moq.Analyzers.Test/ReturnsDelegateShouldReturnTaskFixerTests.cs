using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Verifier = Moq.Analyzers.Test.Helpers.CodeFixVerifier<Moq.Analyzers.ReturnsDelegateShouldReturnTaskAnalyzer, Moq.CodeFixes.ReturnsDelegateShouldReturnTaskFixer>;

namespace Moq.Analyzers.Test;

public class ReturnsDelegateShouldReturnTaskFixerTests(ITestOutputHelper output)
{
    public static IEnumerable<object[]> TestData()
    {
        return new object[][]
        {
            // Task<int> with parameterless lambda returning int
            [
                """new Mock<AsyncService>().Setup(s => s.GetValueAsync()).{|Moq1208:Returns(() => 42)|};""",
                """new Mock<AsyncService>().Setup(s => s.GetValueAsync()).ReturnsAsync(() => 42);""",
            ],

            // Task<string> with parameterless lambda returning string
            [
                """new Mock<AsyncService>().Setup(s => s.GetNameAsync()).{|Moq1208:Returns(() => "hello")|};""",
                """new Mock<AsyncService>().Setup(s => s.GetNameAsync()).ReturnsAsync(() => "hello");""",
            ],

            // ValueTask<int> with parameterless lambda returning int
            [
                """new Mock<AsyncService>().Setup(s => s.GetValueTaskAsync()).{|Moq1208:Returns(() => 42)|};""",
                """new Mock<AsyncService>().Setup(s => s.GetValueTaskAsync()).ReturnsAsync(() => 42);""",
            ],

            // Delegate with parameter
            [
                """new Mock<AsyncService>().Setup(s => s.ProcessAsync(It.IsAny<string>())).{|Moq1208:Returns((string x) => x.Length)|};""",
                """new Mock<AsyncService>().Setup(s => s.ProcessAsync(It.IsAny<string>())).ReturnsAsync((string x) => x.Length);""",
            ],

            // Parenthesized Setup expression
            [
                """(new Mock<AsyncService>().Setup(s => s.GetValueAsync())).{|Moq1208:Returns(() => 42)|};""",
                """(new Mock<AsyncService>().Setup(s => s.GetValueAsync())).ReturnsAsync(() => 42);""",
            ],

            // Block-bodied lambda returning wrong type
            [
                """new Mock<AsyncService>().Setup(s => s.GetValueAsync()).{|Moq1208:Returns(() => { return 42; })|};""",
                """new Mock<AsyncService>().Setup(s => s.GetValueAsync()).ReturnsAsync(() => { return 42; });""",
            ],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    /// <summary>
    /// Anonymous methods and method groups with type mismatches produce compiler errors
    /// (CS0029/CS1662), unlike lambdas. We suppress compiler diagnostics to isolate the fixer.
    /// </summary>
    /// <returns>Test data with compiler diagnostic suppression for anonymous delegate and method group cases.</returns>
    public static IEnumerable<object[]> AnonymousDelegateAndMethodGroupTestData()
    {
        return new object[][]
        {
            // Anonymous method returning int on Task<int> method
            [
                """new Mock<AsyncService>().Setup(s => s.GetValueAsync()).{|Moq1208:Returns(delegate { return 42; })|};""",
                """new Mock<AsyncService>().Setup(s => s.GetValueAsync()).ReturnsAsync(delegate { return 42; });""",
            ],

            // Anonymous method with parameter returning wrong type on Task<int> method
            [
                """new Mock<AsyncService>().Setup(s => s.ProcessAsync(It.IsAny<string>())).{|Moq1208:Returns(delegate (string x) { return x.Length; })|};""",
                """new Mock<AsyncService>().Setup(s => s.ProcessAsync(It.IsAny<string>())).ReturnsAsync(delegate (string x) { return x.Length; });""",
            ],

            // Method group returning int on Task<int> method
            [
                """new Mock<AsyncService>().Setup(s => s.GetValueAsync()).{|Moq1208:Returns(GetInt)|};""",
                """new Mock<AsyncService>().Setup(s => s.GetValueAsync()).ReturnsAsync(GetInt);""",
            ],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    public static IEnumerable<object[]> ExplicitTypeArgumentFixData()
    {
        return new object[][]
        {
            [
                """Returns<string>((string x) => default)""",
                """ReturnsAsync((string x) => default)""",
            ],
            [
                """Returns<string>(delegate (string x) { return default; })""",
                """ReturnsAsync(delegate (string x) { return default; })""",
            ],
        };
    }

    public static IEnumerable<object[]> ExplicitTypeArgumentNoFixData()
    {
        return new object[][]
        {
            ["""Returns<string>(x => default)"""],
            ["""Returns<string>((x) => default)"""],
            ["""Returns<string>(GetLengthAsync)"""],
        };
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldReplaceReturnsWithReturnsAsync(string referenceAssemblyGroup, string @namespace, string original, string quickFix)
    {
        await VerifyAsync(referenceAssemblyGroup, @namespace, original, quickFix);
    }

    [Theory]
    [MemberData(nameof(AnonymousDelegateAndMethodGroupTestData))]
    public async Task ShouldReplaceReturnsWithReturnsAsyncForAnonymousDelegateAndMethodGroup(string referenceAssemblyGroup, string @namespace, string original, string quickFix)
    {
        await VerifyAsync(referenceAssemblyGroup, @namespace, original, quickFix, CompilerDiagnostics.None);
    }

    [Theory]
    [MemberData(nameof(ExplicitTypeArgumentFixData))]
    public async Task ShouldDropTypeArgumentsWhenExplicitParameterTypesAllowInference(string original, string expected)
    {
        string source = Template("using Moq;\nusing System.Threading.Tasks;", $"new Mock<AsyncService>().Setup(s => s.ProcessAsync(It.IsAny<string>())).{original};");
        (AdhocWorkspace workspace, Document document, List<CodeAction> actions) = await GetCodeActionsAsync(source);
        using (workspace)
        {
            CodeAction action = Assert.Single(actions);
            string changedText = await GetChangedTextAsync(action, document);

            Assert.Contains(expected, changedText, StringComparison.Ordinal);
            Assert.DoesNotContain("ReturnsAsync<string>", changedText, StringComparison.Ordinal);
        }
    }

    [Theory]
    [MemberData(nameof(ExplicitTypeArgumentNoFixData))]
    public async Task ShouldNotOfferFixWhenDroppedTypeArgumentsCannotBeInferred(string original)
    {
        string source = Template("using Moq;\nusing System.Threading.Tasks;", $"new Mock<AsyncService>().Setup(s => s.ProcessAsync(It.IsAny<string>())).{original};");
        (AdhocWorkspace workspace, Document _, List<CodeAction> actions) = await GetCodeActionsAsync(source);
        using (workspace)
        {
            Assert.Empty(actions);
        }
    }

    private static string Template(string ns, string mock) =>
        $$"""
        {{ns}}

        public class AsyncService
        {
            public virtual Task<int> GetValueAsync() => Task.FromResult(0);
            public virtual Task<string> GetNameAsync() => Task.FromResult(string.Empty);
            public virtual ValueTask<int> GetValueTaskAsync() => new ValueTask<int>(0);
            public virtual Task<int> ProcessAsync(string input) => Task.FromResult(input.Length);
        }

        internal class UnitTest
        {
            private static int GetInt() => 42;
            private static int GetLength(string input) => input.Length;
            private static Task<int> GetLengthAsync(string input) => Task.FromResult(input.Length);

            private void Test()
            {
                {{mock}}
            }
        }
        """;

    private static async Task<(AdhocWorkspace Workspace, Document Document, List<CodeAction> Actions)> GetCodeActionsAsync(string source)
    {
        (SemanticModel model, SyntaxTree tree) = await CompilationHelper.CreateMoqCompilationAsync(source).ConfigureAwait(false);
        SyntaxNode root = await tree.GetRootAsync().ConfigureAwait(false);
        GenericNameSyntax returnsName = root
            .DescendantNodes()
            .OfType<GenericNameSyntax>()
            .Single(name => string.Equals(name.Identifier.ValueText, "Returns", StringComparison.Ordinal));

        Diagnostic diagnostic = CreateSyntheticDiagnosticAtSpan(tree, returnsName.Span);
        AdhocWorkspace workspace = new();
        Document document = CreateTestDocument(workspace, model, source);
        List<CodeAction> actions = await InvokeFixerAsync(document, diagnostic).ConfigureAwait(false);
        return (workspace, document, actions);
    }

    private static Diagnostic CreateSyntheticDiagnosticAtSpan(SyntaxTree tree, TextSpan span)
    {
        DiagnosticDescriptor descriptor = new(
            DiagnosticIds.ReturnsDelegateMismatchOnAsyncMethod,
            "Returns delegate mismatch",
            "Returns delegate mismatch",
            "Correctness",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        return Diagnostic.Create(descriptor, Location.Create(tree, span));
    }

    private static Document CreateTestDocument(AdhocWorkspace workspace, SemanticModel model, string source)
    {
        Project project = workspace.AddProject("TestProject", LanguageNames.CSharp)
            .AddMetadataReferences(model.Compilation.References);
        return project.AddDocument("Test.cs", SourceText.From(source));
    }

    private static async Task<List<CodeAction>> InvokeFixerAsync(Document document, Diagnostic diagnostic)
    {
        Moq.CodeFixes.ReturnsDelegateShouldReturnTaskFixer fixer = new();
        List<CodeAction> actions = [];

        CodeFixContext context = new(
            document,
            diagnostic,
            (action, _) => actions.Add(action),
            CancellationToken.None);

        await fixer.RegisterCodeFixesAsync(context).ConfigureAwait(false);
        return actions;
    }

    private static async Task<string> GetChangedTextAsync(CodeAction action, Document document)
    {
        ImmutableArray<CodeActionOperation> operations = await action.GetOperationsAsync(CancellationToken.None).ConfigureAwait(false);
        ApplyChangesOperation applyChanges = Assert.Single(operations.OfType<ApplyChangesOperation>());
        Document changedDocument = applyChanges.ChangedSolution.GetDocument(document.Id)!;
        return (await changedDocument.GetTextAsync(CancellationToken.None).ConfigureAwait(false)).ToString();
    }

    private async Task VerifyAsync(string referenceAssemblyGroup, string @namespace, string original, string quickFix, CompilerDiagnostics? compilerDiagnostics = null)
    {
        string o = Template(@namespace, original);
        string f = Template(@namespace, quickFix);

        output.WriteLine("Original:");
        output.WriteLine(o);
        output.WriteLine(string.Empty);
        output.WriteLine("Fixed:");
        output.WriteLine(f);

        await Verifier.VerifyCodeFixAsync(o, f, referenceAssemblyGroup, compilerDiagnostics: compilerDiagnostics).ConfigureAwait(false);
    }
}
