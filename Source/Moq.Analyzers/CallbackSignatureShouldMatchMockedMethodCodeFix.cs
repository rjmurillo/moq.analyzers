﻿using System.Composition;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Moq.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CallbackSignatureShouldMatchMockedMethodCodeFix))]
[Shared]
public class CallbackSignatureShouldMatchMockedMethodCodeFix : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds
    {
        get { return ImmutableArray.Create(Diagnostics.CallbackSignatureShouldMatchMockedMethodId); }
    }

    public sealed override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        if (root == null)
        {
            return;
        }

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the type declaration identified by the diagnostic.
        ParameterListSyntax? badArgumentListSyntax = root.FindToken(diagnosticSpan.Start)
                                        .Parent?
                                        .AncestorsAndSelf()
                                        .OfType<ParameterListSyntax>()
                                        .First();

        // Register a code action that will invoke the fix.
        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Fix Moq callback signature",
                createChangedDocument: c => FixCallbackSignatureAsync(root, context.Document, badArgumentListSyntax, c),
                equivalenceKey: "Fix Moq callback signature"),
            diagnostic);
    }

    private async Task<Document> FixCallbackSignatureAsync(SyntaxNode root, Document document, ParameterListSyntax? oldParameters, CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        Debug.Assert(semanticModel != null, nameof(semanticModel) + " != null");

        if (semanticModel == null)
        {
            return document;
        }

        if (oldParameters?.Parent?.Parent?.Parent?.Parent is not InvocationExpressionSyntax callbackInvocation)
        {
            return document;
        }

        var setupMethodInvocation = Helpers.FindSetupMethodFromCallbackInvocation(semanticModel, callbackInvocation);
        Debug.Assert(setupMethodInvocation != null, nameof(setupMethodInvocation) + " != null");
        var matchingMockedMethods = Helpers.GetAllMatchingMockedMethodSymbolsFromSetupMethodInvocation(semanticModel, setupMethodInvocation).ToArray();

        if (matchingMockedMethods.Length != 1 || oldParameters == null)
        {
            return document;
        }

        var newParameters = SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(matchingMockedMethods[0].Parameters.Select(
            p =>
            {
                var type = SyntaxFactory.ParseTypeName(p.Type.ToMinimalDisplayString(semanticModel, oldParameters.SpanStart));
                return SyntaxFactory.Parameter(default, SyntaxFactory.TokenList(), type, SyntaxFactory.Identifier(p.Name), null);
            })));

        var newRoot = root.ReplaceNode(oldParameters, newParameters);
        return document.WithSyntaxRoot(newRoot);
    }
}
