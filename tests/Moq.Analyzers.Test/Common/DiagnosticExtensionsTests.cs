using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Moq.Analyzers.Common;
using Xunit;

namespace Moq.Analyzers.Test.Common;

public class DiagnosticExtensionsTests
{
    [Fact]
    public void CreateDiagnostic_FromSyntaxNode_Basic()
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText("class C { void M() {} }");
        SyntaxNode root = tree.GetRoot();
        DiagnosticDescriptor rule = new DiagnosticDescriptor("TEST0001", "Test", "Test message", "Test", DiagnosticSeverity.Warning, true);
        Diagnostic diag = root.CreateDiagnostic(rule);
        Assert.Equal("TEST0001", diag.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diag.Severity);
    }

    [Fact]
    public void CreateDiagnostic_FromLocation_WithProperties()
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText("class C { void M() {} }");
        Location loc = tree.GetRoot().GetLocation();
        DiagnosticDescriptor rule = new DiagnosticDescriptor("TEST0002", "Test2", "Test message 2", "Test", DiagnosticSeverity.Info, true);
        ImmutableDictionary<string, string?> properties = ImmutableDictionary<string, string?>.Empty.Add("Key", "Value");
        Diagnostic diag = loc.CreateDiagnostic(rule, properties);
        Assert.Equal("TEST0002", diag.Id);
        Assert.Equal("Value", diag.Properties["Key"]);
    }

    [Fact]
    public void CreateDiagnostic_FromOperation_DelegatesToSyntax()
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText("class C { void M() {} }");
        SyntaxNode root = tree.GetRoot();
        CSharpCompilation compilation = CSharpCompilation.Create("Test", new[] { tree });
        SemanticModel model = compilation.GetSemanticModel(tree);
        MethodDeclarationSyntax methodDecl = root.DescendantNodes().OfType<MethodDeclarationSyntax>().First();
        Microsoft.CodeAnalysis.IOperation? operation = model.GetOperation(methodDecl);
        DiagnosticDescriptor rule = new DiagnosticDescriptor("TEST0003", "Test3", "Test message 3", "Test", DiagnosticSeverity.Error, true);
        Diagnostic diag = operation!.CreateDiagnostic(rule);
        Assert.Equal("TEST0003", diag.Id);
        Assert.Equal(DiagnosticSeverity.Error, diag.Severity);
    }
}
