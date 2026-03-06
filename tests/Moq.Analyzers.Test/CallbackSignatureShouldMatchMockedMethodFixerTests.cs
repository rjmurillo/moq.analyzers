using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Moq.CodeFixes;

namespace Moq.Analyzers.Test;

/// <summary>
/// Direct unit tests for <see cref="CallbackSignatureShouldMatchMockedMethodFixer"/> guard clauses
/// that cannot be exercised through the standard analyzer/code fix test harness.
/// </summary>
public class CallbackSignatureShouldMatchMockedMethodFixerTests
{
    [Fact]
    public async Task RegisterCodeFixesAsync_WhenParameterListNotFound_DoesNotRegisterCodeFix()
    {
        // Arrange: source where the diagnostic span points to a token
        // that does NOT have a ParameterListSyntax ancestor.
        const string source = """
            namespace TestNamespace
            {
                class MyClass
                {
                    int field = 42;
                }
            }
            """;

        using AdhocWorkspace workspace = new();
        Project project = workspace.AddProject("TestProject", LanguageNames.CSharp)
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddMetadataReferences(CompilationHelper.CoreReferences);

        Document document = project.AddDocument("Test.cs", SourceText.From(source));

        SyntaxTree? tree = await document.GetSyntaxTreeAsync();
        Assert.NotNull(tree);

        SyntaxNode root = await tree.GetRootAsync();

        // Find the "field" token. It has no ParameterListSyntax ancestor.
        SyntaxToken fieldToken = root.DescendantTokens()
            .First(t => string.Equals(t.Text, "field", StringComparison.Ordinal));

        DiagnosticDescriptor descriptor = new(
            "Moq1100",
            "Test",
            "Test message",
            "Test",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        Diagnostic diagnostic = Diagnostic.Create(
            descriptor,
            Location.Create(tree, fieldToken.Span));

        List<CodeAction> registeredActions = [];

        CodeFixContext context = new(
            document,
            diagnostic.Location.SourceSpan,
            ImmutableArray.Create(diagnostic),
            (action, _) => registeredActions.Add(action),
            CancellationToken.None);

        CallbackSignatureShouldMatchMockedMethodFixer fixer = new();

        // Act
        await fixer.RegisterCodeFixesAsync(context);

        // Assert: no code fix registered because the diagnostic location
        // has no ParameterListSyntax ancestor.
        Assert.Empty(registeredActions);
    }
}
