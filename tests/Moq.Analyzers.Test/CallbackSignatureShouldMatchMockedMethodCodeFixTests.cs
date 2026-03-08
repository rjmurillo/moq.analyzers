using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using AnalyzerVerifier = Moq.Analyzers.Test.Helpers.AnalyzerVerifier<Moq.Analyzers.CallbackSignatureShouldMatchMockedMethodAnalyzer>;
using Verifier = Moq.Analyzers.Test.Helpers.CodeFixVerifier<Moq.Analyzers.CallbackSignatureShouldMatchMockedMethodAnalyzer, Moq.CodeFixes.CallbackSignatureShouldMatchMockedMethodFixer>;

namespace Moq.Analyzers.Test;

#pragma warning disable SA1204 // Static members should appear before non-static members

public class CallbackSignatureShouldMatchMockedMethodCodeFixTests
{
    private readonly ITestOutputHelper _output;

    public CallbackSignatureShouldMatchMockedMethodCodeFixTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0051:Method is too long", Justification = "Contains test data")]
    public static IEnumerable<object[]> TestData()
    {
        return new object[][]
        {
            [
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<string>())).Returns((string s) => { return 0; });""",
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<string>())).Returns((string s) => { return 0; });""",
            ],
            [
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>())).Returns((int i, string s, DateTime dt) => { return 0; });""",
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>())).Returns((int i, string s, DateTime dt) => { return 0; });""",
            ],
            [
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<List<string>>())).Returns((List<string> l) => { return 0; });""",
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<List<string>>())).Returns((List<string> l) => { return 0; });""",
            ],
            [
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<string>())).Callback(({|Moq1100:int i|}) => { });""",
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<string>())).Callback((string i) => { });""",
            ],
            [
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<string>())).Callback({|Moq1100:(string s1, string s2)|} => { });""",
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<string>())).Callback((string s1) => { });""",
            ],
            [
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>())).Callback({|Moq1100:(string s1, int i1)|} => { });""",
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>())).Callback((int s1, string i1, DateTime dt) => { });""",
            ],
            [
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<List<string>>())).Callback(({|Moq1100:int i|}) => { });""",
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<List<string>>())).Callback((List<string> i) => { });""",
            ],
            [
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<string>())).Callback((string s) => { });""",
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<string>())).Callback((string s) => { });""",
            ],
            [
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>())).Callback((int i, string s, DateTime dt) => { });""",
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>())).Callback((int i, string s, DateTime dt) => { });""",
            ],
            [
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<List<string>>())).Callback((List<string> l) => { });""",
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<List<string>>())).Callback((List<string> l) => { });""",
            ],
            [
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<string>())).Callback(() => { });""",
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<string>())).Callback(() => { });""",
            ],
            [
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>())).Callback(() => { });""",
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>())).Callback(() => { });""",
            ],
            [
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<List<string>>())).Callback(() => { });""",
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<List<string>>())).Callback(() => { });""",
            ],
            [
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<string>())).Returns(0).Callback((string s) => { });""",
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<string>())).Returns(0).Callback((string s) => { });""",
            ],
            [
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>())).Returns(0).Callback((int i, string s, DateTime dt) => { });""",
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>())).Returns(0).Callback((int i, string s, DateTime dt) => { });""",
            ],
            [
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<List<string>>())).Returns(0).Callback((List<string> l) => { });""",
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<List<string>>())).Returns(0).Callback((List<string> l) => { });""",
            ],
            [ // Repros for https://github.com/rjmurillo/moq.analyzers/issues/172
                """new Mock<IFoo>().Setup(m => m.Do(It.IsAny<object?>())).Returns((object? bar) => true);""",
                """new Mock<IFoo>().Setup(m => m.Do(It.IsAny<object?>())).Returns((object? bar) => true);""",
            ],
            [
                """new Mock<IFoo>().Setup(m => m.Do(42)).Returns((long bar) => true);""",
                """new Mock<IFoo>().Setup(m => m.Do(42)).Returns((long bar) => true);""",
            ],
            [
                """new Mock<IFoo>().Setup(m => m.Do((long)42)).Returns((long bar) => true);""",
                """new Mock<IFoo>().Setup(m => m.Do((long)42)).Returns((long bar) => true);""",
            ],
            [
                """new Mock<IFoo>().Setup(m => m.Do(42)).Returns((object? bar) => true);""",
                """new Mock<IFoo>().Setup(m => m.Do(42)).Returns((object? bar) => true);""",
            ],
            [ // This was also reported as part of 172, but is a different error
                """new Mock<IFoo>().Setup(m => m.Do(42)).Returns((int bar) => true);""",
                """new Mock<IFoo>().Setup(m => m.Do(42)).Returns((int bar) => true);""",
            ],
            [ // Test delegate construction callbacks - these should work the same as lambdas
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<string>())).Callback(new StringDelegate((string s) => { }));""",
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<string>())).Callback(new StringDelegate((string s) => { }));""",
            ],
            [ // Parenthesized Setup with wrong callback type
                """(new Mock<IFoo>().Setup(x => x.Do(It.IsAny<string>()))).Callback(({|Moq1100:int i|}) => { });""",
                """(new Mock<IFoo>().Setup(x => x.Do(It.IsAny<string>()))).Callback((string i) => { });""",
            ],
            [ // Double-parenthesized Setup with wrong callback type
                """((new Mock<IFoo>().Setup(x => x.Do(It.IsAny<string>())))).Callback(({|Moq1100:int i|}) => { });""",
                """((new Mock<IFoo>().Setup(x => x.Do(It.IsAny<string>())))).Callback((string i) => { });""",
            ],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task ShouldSuggestQuickFixWhenIncorrectCallbacks(string referenceAssemblyGroup, string @namespace, string original, string quickFix)
    {
        string o = CallbackTemplate(@namespace, original);
        string f = CallbackTemplate(@namespace, quickFix);

        _output.WriteLine("Original:");
        _output.WriteLine(o);
        _output.WriteLine(string.Empty);
        _output.WriteLine("Fixed:");
        _output.WriteLine(f);

        await Verifier.VerifyCodeFixAsync(o, f, referenceAssemblyGroup);
    }

    public static IEnumerable<object[]> ConversionTestData()
    {
        return new object[][]
            {
                [ // This should be allowed because of the implicit conversion from int to CustomType
                    """new Mock<IFoo>().Setup(x => x.Do(42)).Returns((CustomType i) => true);""",
                    """new Mock<IFoo>().Setup(x => x.Do(42)).Returns((CustomType i) => true);""",
                ],
                [ // This should be allowed because of identity
                    """new Mock<IFoo>().Setup(x => x.Do(new CustomType(42))).Returns((CustomType i) => true);""",
                    """new Mock<IFoo>().Setup(x => x.Do(new CustomType(42))).Returns((CustomType i) => true);""",
                ],
                [ // This should be allowed because of the explicit conversion from string to CustomType
                    """new Mock<IFoo>().Setup(x => x.Do((CustomType)"42")).Returns((CustomType i) => true);""",
                    """new Mock<IFoo>().Setup(x => x.Do((CustomType)"42")).Returns((CustomType i) => true);""",
                ],
                [ // This should be allowed because of numeric conversion (explicit)
                    """new Mock<IFoo>().Setup(x => x.Do((int)42L)).Returns((CustomType i) => true);""",
                    """new Mock<IFoo>().Setup(x => x.Do((int)42L)).Returns((CustomType i) => true);""",
                ],
                [ // This should be allowed because of numeric conversion (explicit)
                    """new Mock<IFoo>().Setup(x => x.Do((int)42.0)).Returns((CustomType i) => true);""",
                    """new Mock<IFoo>().Setup(x => x.Do((int)42.0)).Returns((CustomType i) => true);""",
                ],
                [
                    """new Mock<IFoo>().Setup(m => m.Do(It.IsAny<int>())).Returns((CustomType i) => true);""",
                    """new Mock<IFoo>().Setup(m => m.Do(It.IsAny<int>())).Returns((CustomType i) => true);""",
                ],
                [
                    """new Mock<IFoo>().Setup(m => m.Do(It.IsAny<CustomType>())).Returns((CustomType i) => true);""",
                    """new Mock<IFoo>().Setup(m => m.Do(It.IsAny<CustomType>())).Returns((CustomType i) => true);""",
                ],
            }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    [Theory]
    [MemberData(nameof(ConversionTestData))]
    public async Task ConversionTests(string referenceAssemblyGroup, string @namespace, string original, string quickFix)
    {
        static string Template(string ns, string mock) =>
            $$"""
            {{ns}}

            internal interface IFoo
            {
                bool Do(CustomType custom);
            }

            public class CustomType
            {
                public int Value { get; }

                public CustomType(int value)
                {
                    Value = value;
                }

                // User-defined conversions
                public static implicit operator CustomType(int value)
                {
                    return new CustomType(value);
                }

                public static explicit operator CustomType(string str)
                {
                    return new CustomType(int.Parse(str));
                }
            }

            internal class UnitTest
            {
                private void Test()
                {
                    {{mock}}
                }
            }
            """;

        string o = Template(@namespace, original);
        string f = Template(@namespace, quickFix);

        _output.WriteLine("Original:");
        _output.WriteLine(o);
        _output.WriteLine(string.Empty);
        _output.WriteLine("Fixed:");
        _output.WriteLine(f);

        await Verifier.VerifyCodeFixAsync(o, f, referenceAssemblyGroup);
    }

    public static IEnumerable<object[]> SimpleLambdaTestData()
    {
        return new object[][]
        {
            [ // Simple lambda in delegate constructor with wrong type bails out (issue #1012)
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<string>())).Callback(new Action<int>({|Moq1100:x|} => { }));""",
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<string>())).Callback(new Action<int>({|Moq1100:x|} => { }));""",
            ],
            [ // Simple lambda in delegate constructor with wrong parameter count bails out (issue #1012)
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>())).Callback(new Action<int>({|Moq1100:x|} => { }));""",
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>())).Callback(new Action<int>({|Moq1100:x|} => { }));""",
            ],
            [ // Expression-bodied simple lambda in delegate constructor bails out
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<string>())).Callback(new Action<int>({|Moq1100:x|} => x.ToString()));""",
                """new Mock<IFoo>().Setup(x => x.Do(It.IsAny<string>())).Callback(new Action<int>({|Moq1100:x|} => x.ToString()));""",
            ],
        }.WithNamespaces().WithMoqReferenceAssemblyGroups();
    }

    // Direct simple lambda fixer tests (FixerConvertsDirectSimpleLambda*ToParenthesized)
    // verify the happy path. They use synthetic diagnostics because the compiler cannot
    // infer the delegate type for untyped simple lambdas, preventing the analyzer from
    // reporting Moq1100. The compilable source provides a valid semantic model while
    // the fixer input uses the simple lambda form.
    [Theory]
    [MemberData(nameof(SimpleLambdaTestData))]
    public async Task ShouldFixSimpleLambdaCallbackSignature(string referenceAssemblyGroup, string @namespace, string original, string quickFix)
    {
        string o = CallbackTemplate(@namespace, original);
        string f = CallbackTemplate(@namespace, quickFix);

        _output.WriteLine("Original:");
        _output.WriteLine(o);
        _output.WriteLine(string.Empty);
        _output.WriteLine("Fixed:");
        _output.WriteLine(f);

        // The fixer bails out for simple lambdas inside delegate constructors,
        // so no fix iterations occur. Compiler diagnostics are suppressed because
        // the delegate constructor type intentionally mismatches the lambda signature.
        await Verifier.VerifyCodeFixAsync(o, f, referenceAssemblyGroup, numberOfIncrementalIterations: 0, numberOfFixAllIterations: 0, CompilerDiagnostics.None);
    }

    [Fact]
    public async Task FixerConvertsDirectSimpleLambdaBlockToParenthesized()
    {
        const string compilableSource =
            """
            using System;
            using System.Collections.Generic;
            using Moq;

            internal interface IFoo
            {
                int Do(string s);
            }

            internal class UnitTest
            {
                private void Test()
                {
                    new Mock<IFoo>().Setup(x => x.Do(It.IsAny<string>())).Callback((string x) => { });
                }
            }
            """;

        await VerifySimpleLambdaConversionAsync(compilableSource, "(string x) => { }", "Callback(x => { })");
    }

    [Fact]
    public async Task FixerConvertsDirectSimpleLambdaExpressionToParenthesized()
    {
        const string compilableSource =
            """
            using System;
            using System.Collections.Generic;
            using Moq;

            internal interface IFoo
            {
                int Do(string s);
            }

            internal class UnitTest
            {
                private void Test()
                {
                    new Mock<IFoo>().Setup(x => x.Do(It.IsAny<string>())).Callback((string x) => x.ToString());
                }
            }
            """;

        await VerifySimpleLambdaConversionAsync(compilableSource, "(string x) => x.ToString()", "Callback(x => x.ToString())");
    }

    [Fact]
    public async Task FixerSkipsSimpleLambdaInsideDelegateConstructor()
    {
        // When a simple lambda is inside a delegate constructor, the fixer
        // should NOT register a code action (it bails out).
        const string source =
            """
            using System;
            using System.Collections.Generic;
            using Moq;

            internal interface IFoo
            {
                int Do(string s);
            }

            internal class UnitTest
            {
                private void Test()
                {
                    new Mock<IFoo>().Setup(x => x.Do(It.IsAny<string>())).Callback(new Action<int>(x => { }));
                }
            }
            """;

        (SemanticModel model, SyntaxTree tree) = await CompilationHelper.CreateMoqCompilationAsync(source);
        SyntaxNode root = await tree.GetRootAsync();

        SimpleLambdaExpressionSyntax simpleLambda = root
            .DescendantNodes()
            .OfType<SimpleLambdaExpressionSyntax>()
            .Last();

        Diagnostic diagnostic = CreateSyntheticDiagnosticAtSpan(tree, simpleLambda.Parameter.Span);
        using AdhocWorkspace workspace = new();
        Document document = CreateTestDocument(workspace, model, source);
        List<CodeAction> actions = await InvokeFixerAsync(document, diagnostic);

        // Fixer should NOT register a code action for simple lambdas in delegate constructors.
        Assert.Empty(actions);
    }

    [Fact]
    public async Task FixerSkipsParenthesizedLambdaInsideDelegateConstructor()
    {
        const string source =
            """
            using System;
            using System.Collections.Generic;
            using Moq;

            internal interface IFoo
            {
                int Do(string s);
            }

            internal class UnitTest
            {
                private void Test()
                {
                    new Mock<IFoo>().Setup(x => x.Do(It.IsAny<string>())).Callback(new Action<int>((int x) => { }));
                }
            }
            """;

        (SemanticModel model, SyntaxTree tree) = await CompilationHelper.CreateMoqCompilationAsync(source);
        SyntaxNode root = await tree.GetRootAsync();

        ParenthesizedLambdaExpressionSyntax parenthesizedLambda = root
            .DescendantNodes()
            .OfType<ParenthesizedLambdaExpressionSyntax>()
            .Last();

        Diagnostic diagnostic = CreateSyntheticDiagnosticAtSpan(tree, parenthesizedLambda.ParameterList.Span);
        using AdhocWorkspace workspace = new();
        Document document = CreateTestDocument(workspace, model, source);
        List<CodeAction> actions = await InvokeFixerAsync(document, diagnostic);

        Assert.Single(actions);
        await AssertDocumentUnchangedAsync(actions[0], document);
    }

    [Fact]
    public async Task FixerConvertsDirectSimpleLambdaForReturns()
    {
        const string compilableSource =
            """
            using System;
            using System.Collections.Generic;
            using Moq;

            internal interface IFoo
            {
                int Do(string s);
            }

            internal class UnitTest
            {
                private void Test()
                {
                    new Mock<IFoo>().Setup(x => x.Do(It.IsAny<string>())).Returns((string x) => 42);
                }
            }
            """;

        await VerifySimpleLambdaConversionAsync(compilableSource, "(string x) => 42", "Returns(x => 42)");
    }

    [Theory]
    [MemberData(nameof(DoppelgangerTestHelper.GetAllCustomMockData), MemberType = typeof(DoppelgangerTestHelper))]
    public async Task ShouldPassIfCustomMockClassIsUsed(string mockCode)
    {
        await AnalyzerVerifier.VerifyAnalyzerAsync(
            DoppelgangerTestHelper.CreateTestCode(mockCode),
            ReferenceAssemblyCatalog.Net80WithNewMoq);
    }

    [Fact]
    public async Task FixerReturnsUnchangedDocumentWhenSetupMethodNotFound()
    {
        // This test exercises the defensive null guard on setupMethodInvocation.
        // The guard is unreachable through normal analyzer-fixer pipelines because
        // the analyzer only reports Moq1100 when it finds a Setup method. We test
        // it by injecting a synthetic Moq1100 diagnostic on a non-chained Callback.
        const string source =
            """
            using System;
            using Moq;

            internal interface IFoo
            {
                int Do(string s);
            }

            internal class UnitTest
            {
                private void Test()
                {
                    var setup = new Mock<IFoo>().Setup(x => x.Do(It.IsAny<string>()));
                    setup.Callback((int i) => { });
                }
            }
            """;

        (SemanticModel model, SyntaxTree tree) = await CompilationHelper.CreateMoqCompilationAsync(source);
        SyntaxNode root = await tree.GetRootAsync();

        Diagnostic diagnostic = CreateSyntheticDiagnostic(root, tree);
        using AdhocWorkspace workspace = new();
        Document document = CreateTestDocument(workspace, model, source);
        List<CodeAction> actions = await InvokeFixerAsync(document, diagnostic);

        Assert.Single(actions);
        await AssertDocumentUnchangedAsync(actions[0], document);
    }

    private static string CallbackTemplate(string ns, string mock) =>
        $$"""
        {{ns}}

        internal interface IFoo
        {
            int Do(string s);

            int Do(int i, string s, DateTime dt);

            int Do(List<string> l);

            bool Do(object? bar);

            bool Do(long bar);
        }

        internal delegate void StringDelegate(string s);

        internal class UnitTest
        {
            private void Test()
            {
                {{mock}}
            }
        }
        """;

    /// <summary>
    /// Verifies that the fixer converts a simple lambda to a parenthesized lambda
    /// with correct types from the mocked method signature.
    /// </summary>
    private async Task VerifySimpleLambdaConversionAsync(string compilableSource, string expectedFragment, string unexpectedFragment)
    {
        (SemanticModel model, SyntaxTree _) = await CompilationHelper.CreateMoqCompilationAsync(compilableSource);
        using AdhocWorkspace workspace = new();
        Document compilableDoc = CreateTestDocument(workspace, model, compilableSource);

        SyntaxNode compilableRoot = (await compilableDoc.GetSyntaxRootAsync())!;
        ParenthesizedLambdaExpressionSyntax parenthesizedLambda = compilableRoot
            .DescendantNodes()
            .OfType<ParenthesizedLambdaExpressionSyntax>()
            .Last();

        SimpleLambdaExpressionSyntax simpleLambda = SyntaxFactory.SimpleLambdaExpression(
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("x")),
            parenthesizedLambda.Block,
            parenthesizedLambda.ExpressionBody)
            .WithArrowToken(parenthesizedLambda.ArrowToken)
            .WithTriviaFrom(parenthesizedLambda);

        SyntaxNode modifiedRoot = compilableRoot.ReplaceNode(parenthesizedLambda, simpleLambda);
        Document modifiedDoc = compilableDoc.WithSyntaxRoot(modifiedRoot);

        SyntaxNode modifiedSyntaxRoot = (await modifiedDoc.GetSyntaxRootAsync())!;
        SimpleLambdaExpressionSyntax targetLambda = modifiedSyntaxRoot
            .DescendantNodes()
            .OfType<SimpleLambdaExpressionSyntax>()
            .Last();

        Diagnostic diagnostic = CreateSyntheticDiagnosticAtSpan(modifiedSyntaxRoot.SyntaxTree, targetLambda.Parameter.Span);
        List<CodeAction> actions = await InvokeFixerAsync(modifiedDoc, diagnostic);

        Assert.Single(actions);

        ImmutableArray<CodeActionOperation> operations = await actions[0].GetOperationsAsync(CancellationToken.None).ConfigureAwait(false);
        ApplyChangesOperation applyChanges = Assert.Single(operations.OfType<ApplyChangesOperation>());
        Document changedDocument = applyChanges.ChangedSolution.GetDocument(modifiedDoc.Id)!;
        string changedText = (await changedDocument.GetTextAsync(CancellationToken.None).ConfigureAwait(false)).ToString();

        Assert.Contains(expectedFragment, changedText, StringComparison.Ordinal);
        Assert.DoesNotContain(unexpectedFragment, changedText, StringComparison.Ordinal);
    }

    private static Diagnostic CreateSyntheticDiagnostic(SyntaxNode root, SyntaxTree tree)
    {
        ParameterListSyntax parameterList = root
            .DescendantNodes()
            .OfType<ParenthesizedLambdaExpressionSyntax>()
            .Last()
            .ParameterList;

        return CreateSyntheticDiagnosticAtSpan(tree, parameterList.Span);
    }

    private static Diagnostic CreateSyntheticDiagnosticAtSpan(SyntaxTree tree, TextSpan span)
    {
        DiagnosticDescriptor descriptor = new(
            DiagnosticIds.BadCallbackParameters,
            "Bad callback parameters",
            "Callback signature must match the signature of the mocked method",
            "Usage",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        return Diagnostic.Create(descriptor, Location.Create(tree, span));
    }

    private static Document CreateTestDocument(AdhocWorkspace workspace, SemanticModel model, string source)
    {
        Project project = workspace.AddProject("TestProject", LanguageNames.CSharp);
        project = project.AddMetadataReferences(model.Compilation.References);
        return project.AddDocument("Test.cs", SourceText.From(source));
    }

    private static async Task<List<CodeAction>> InvokeFixerAsync(Document document, Diagnostic diagnostic)
    {
        Moq.CodeFixes.CallbackSignatureShouldMatchMockedMethodFixer fixer = new();
        List<CodeAction> actions = [];

        CodeFixContext context = new(
            document,
            diagnostic,
            (action, _) => actions.Add(action),
            CancellationToken.None);

        await fixer.RegisterCodeFixesAsync(context).ConfigureAwait(false);
        return actions;
    }

    private static async Task AssertDocumentUnchangedAsync(CodeAction action, Document document)
    {
        ImmutableArray<CodeActionOperation> operations = await action.GetOperationsAsync(CancellationToken.None).ConfigureAwait(false);
        ApplyChangesOperation? applyChanges = operations.OfType<ApplyChangesOperation>().FirstOrDefault();

        if (applyChanges is null)
        {
            return;
        }

        Document changedDocument = applyChanges.ChangedSolution.GetDocument(document.Id)!;
        string originalText = (await document.GetTextAsync(CancellationToken.None).ConfigureAwait(false)).ToString();
        string changedText = (await changedDocument.GetTextAsync(CancellationToken.None).ConfigureAwait(false)).ToString();
        Assert.Equal(originalText, changedText);
    }
}
