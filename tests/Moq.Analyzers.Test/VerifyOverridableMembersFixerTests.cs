using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;

namespace Moq.Analyzers.Test;

/// <summary>
/// Direct unit tests for <see cref="Moq.CodeFixes.VerifyOverridableMembersFixer"/> node resolution.
/// The analyzer always reports the diagnostic on the whole invocation, so the higher-level
/// verifier tests never exercise the ancestor-walk branch that fires when the diagnostic span
/// resolves to a node nested inside the invocation. This test drives that branch with a synthetic
/// diagnostic positioned on the <c>Verify</c> identifier rather than the full invocation.
/// </summary>
public class VerifyOverridableMembersFixerTests
{
    private static readonly string Source = """
        using Moq;

        public class SampleClass
        {
            public int Property { get; set; }
        }

        internal class UnitTest
        {
            private void Test()
            {
                new Mock<SampleClass>().Verify(x => x.Property);
            }
        }
        """;

    [Fact]
    public async Task DiagnosticOnNestedNode_ResolvesToInvocation_AndRegistersFix()
    {
        (SemanticModel model, SyntaxTree tree) = await CompilationHelper.CreateMoqCompilationAsync(Source);
        SyntaxNode root = await tree.GetRootAsync();

        // Position the diagnostic on the `Verify` identifier so FindNode returns a non-invocation
        // node; the fixer must walk up to the enclosing invocation before offering the fix.
        IdentifierNameSyntax verifyName = root
            .DescendantNodes()
            .OfType<IdentifierNameSyntax>()
            .Single(name => string.Equals(name.Identifier.ValueText, "Verify", StringComparison.Ordinal));

        Diagnostic diagnostic = CreateSyntheticDiagnosticAtSpan(tree, verifyName.Span);
        using AdhocWorkspace workspace = new();
        Document document = CreateTestDocument(workspace, model, Source);
        List<CodeAction> actions = await InvokeFixerAsync(document, diagnostic);

        CodeAction action = Assert.Single(actions);
        Assert.Equal("Make member virtual", action.Title);
    }

    private static Diagnostic CreateSyntheticDiagnosticAtSpan(SyntaxTree tree, TextSpan span)
    {
        DiagnosticDescriptor descriptor = new(
            DiagnosticIds.VerifyOnlyUsedForOverridableMembers,
            "Verify only used for overridable members",
            "Verify only used for overridable members",
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
        Moq.CodeFixes.VerifyOverridableMembersFixer fixer = new();
        List<CodeAction> actions = [];

        CodeFixContext context = new(
            document,
            diagnostic,
            (action, _) => actions.Add(action),
            CancellationToken.None);

        await fixer.RegisterCodeFixesAsync(context).ConfigureAwait(false);
        return actions;
    }
}
