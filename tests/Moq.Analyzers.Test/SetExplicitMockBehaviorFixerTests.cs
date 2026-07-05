using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;

namespace Moq.Analyzers.Test;

/// <summary>
/// Direct unit tests for the stale-diagnostic guard in the MockBehavior code-fix pipeline.
/// The standard analyzer/code-fix harness cannot reach this path because the analyzer always
/// emits edit properties that match the node it analyzed; these tests simulate a diagnostic
/// whose recorded edit shape no longer matches the document.
/// </summary>
public class SetExplicitMockBehaviorFixerTests
{
    private static readonly string Source = """
        using Moq;

        public interface ISample
        {
            void Method();
        }

        internal class UnitTest
        {
            private void Test()
            {
                var mock = new Mock<ISample>();
            }
        }
        """;

    private static readonly string SourceWithExplicitBehavior = """
        using Moq;

        public interface ISample
        {
            void Method();
        }

        internal class UnitTest
        {
            private void Test()
            {
                var mock = new Mock<ISample>(MockBehavior.Strict);
            }
        }
        """;

    private static readonly string SourceWithImplicitBehavior = """
        using Moq;

        public interface ISample
        {
            void Method();
        }

        internal class UnitTest
        {
            private void Test()
            {
                var mock = new Mock<ISample>(0);
            }
        }
        """;

    public static IEnumerable<object[]> StaleEditShapeData()
    {
        // EditType / EditPosition pairs that do NOT match `new Mock<ISample>()` (zero arguments):
        // Replace/0 exercises the empty-argument-list case; Replace/5 and Insert/5 exercise the
        // out-of-range position checks. All must produce a graceful no-op instead of throwing.
        return
        [
            ["Replace", "0"],
            ["Replace", "5"],
            ["Insert", "5"],
        ];
    }

    [Theory]
    [MemberData(nameof(StaleEditShapeData))]
    public async Task StaleEditShape_DoesNotThrow_AndLeavesDocumentUnchanged(string editType, string editPosition)
    {
        (SemanticModel model, SyntaxTree tree) = await CompilationHelper.CreateMoqCompilationAsync(Source);
        SyntaxNode root = await tree.GetRootAsync();

        // The diagnostic points at the object-creation node the analyzer would have flagged.
        ObjectCreationExpressionSyntax creation = root
            .DescendantNodes()
            .OfType<ObjectCreationExpressionSyntax>()
            .Single();

        await AssertFixerNoOpAsync(model, tree, creation.Span, Source, editType, editPosition);
    }

    [Fact]
    public async Task StaleNodeKind_NonInvocationOrCreation_DoesNotThrow_AndLeavesDocumentUnchanged()
    {
        (SemanticModel model, SyntaxTree tree) = await CompilationHelper.CreateMoqCompilationAsync(Source);
        SyntaxNode root = await tree.GetRootAsync();

        // Stale scenario where the diagnostic span now resolves to a node the argument rewriters
        // do not understand (the local declaration statement, not an invocation or object creation).
        // The edit type and position are individually valid, so only the node-kind check can reject
        // the edit; the fixer must no-op rather than attempt a rewrite.
        LocalDeclarationStatementSyntax statement = root
            .DescendantNodes()
            .OfType<LocalDeclarationStatementSyntax>()
            .Single();

        await AssertFixerNoOpAsync(model, tree, statement.Span, Source, "Insert", "0");
    }

    [Fact]
    public async Task StaleInsert_WhenMockBehaviorAlreadyPresent_DoesNotDuplicate()
    {
        (SemanticModel model, SyntaxTree tree) = await CompilationHelper.CreateMoqCompilationAsync(SourceWithExplicitBehavior);
        SyntaxNode root = await tree.GetRootAsync();

        // Stale scenario where the diagnostic recorded Insert at position 0, but the user has since
        // added an explicit MockBehavior argument. The node shape still admits an insert (position 0
        // is in range), so only the semantic check can reject it; the fixer must no-op rather than
        // insert a second MockBehavior argument.
        ObjectCreationExpressionSyntax creation = root
            .DescendantNodes()
            .OfType<ObjectCreationExpressionSyntax>()
            .Single();

        await AssertFixerNoOpAsync(model, tree, creation.Span, SourceWithExplicitBehavior, "Insert", "0");
    }

    [Fact]
    public async Task StaleInsert_WhenMockBehaviorSuppliedViaConversion_DoesNotDuplicate()
    {
        (SemanticModel model, SyntaxTree tree) = await CompilationHelper.CreateMoqCompilationAsync(SourceWithImplicitBehavior);
        SyntaxNode root = await tree.GetRootAsync();

        // The user supplied the behavior as the literal 0, which binds to the MockBehavior parameter
        // through an implicit conversion. The argument's expression type is int, but its converted
        // type is MockBehavior, so the semantic check must still recognize it and no-op.
        ObjectCreationExpressionSyntax creation = root
            .DescendantNodes()
            .OfType<ObjectCreationExpressionSyntax>()
            .Single();

        await AssertFixerNoOpAsync(model, tree, creation.Span, SourceWithImplicitBehavior, "Insert", "0");
    }

    private static async Task AssertFixerNoOpAsync(SemanticModel model, SyntaxTree tree, TextSpan span, string sourceText, string editType, string editPosition)
    {
        DiagnosticDescriptor descriptor = new(
            DiagnosticIds.SetExplicitMockBehavior,
            "Test",
            "Test message",
            "Test",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        ImmutableDictionary<string, string?> properties = ImmutableDictionary<string, string?>.Empty
            .Add(DiagnosticEditProperties.EditTypeKey, editType)
            .Add(DiagnosticEditProperties.EditPositionKey, editPosition);

        Diagnostic diagnostic = Diagnostic.Create(descriptor, Location.Create(tree, span), properties);

        using AdhocWorkspace workspace = new();
        Project project = workspace.AddProject("TestProject", LanguageNames.CSharp)
            .AddMetadataReferences(model.Compilation.References);
        Document document = project.AddDocument("Test.cs", SourceText.From(sourceText));

        List<CodeAction> actions = [];
        CodeFixContext context = new(
            document,
            diagnostic,
            (action, _) => actions.Add(action),
            CancellationToken.None);

        Moq.CodeFixes.SetExplicitMockBehaviorFixer fixer = new();
        await fixer.RegisterCodeFixesAsync(context).ConfigureAwait(false);

        // The fixer registers two actions (Loose and Strict). Applying either must not throw and
        // must leave the document unchanged, because the recorded edit shape does not match the node.
        Assert.Equal(2, actions.Count);

        string originalText = (await document.GetTextAsync(CancellationToken.None).ConfigureAwait(false)).ToString();
        foreach (CodeAction action in actions)
        {
            ImmutableArray<CodeActionOperation> operations = await action.GetOperationsAsync(CancellationToken.None).ConfigureAwait(false);
            ApplyChangesOperation applyChanges = Assert.Single(operations.OfType<ApplyChangesOperation>());
            Document changedDocument = applyChanges.ChangedSolution.GetDocument(document.Id)!;
            string changedText = (await changedDocument.GetTextAsync(CancellationToken.None).ConfigureAwait(false)).ToString();
            Assert.Equal(originalText, changedText);
        }
    }
}
