using Microsoft.CodeAnalysis.Text;

namespace Moq.Analyzers.Test.Common;

public class DiagnosticExtensionsTests
{
#pragma warning disable RS2008 // Enable analyzer release tracking (test-only descriptor)
#pragma warning disable ECS1300 // Test-only descriptor; inline init is simpler than static constructor
    private static readonly DiagnosticDescriptor TestRule = new(
        "TEST0001",
        "Test",
        "Test message {0}",
        "Test",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
#pragma warning restore ECS1300
#pragma warning restore RS2008

    // Overload #1: SyntaxNode + rule + messageArgs
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

    // Overload #2: SyntaxNode + rule + properties + messageArgs
    [Fact]
    public void CreateDiagnostic_FromSyntaxNode_WithProperties_SetsDiagnosticProperties()
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText("class C { void M() {} }");
        SyntaxNode node = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();
        ImmutableDictionary<string, string?> properties = ImmutableDictionary<string, string?>.Empty.Add("PropKey", "PropVal");

#pragma warning disable ECS0900 // Minimize boxing and unboxing (messageArgs is params object[])
        Diagnostic diag = node.CreateDiagnostic(TestRule, properties, "arg1");
#pragma warning restore ECS0900

        Assert.Equal("TEST0001", diag.Id);
        Assert.Equal("PropVal", diag.Properties["PropKey"]);
        Assert.True(diag.Location.IsInSource);
    }

    // Overload #3: SyntaxNode + rule + additionalLocations + properties + messageArgs
    [Fact]
    public void CreateDiagnostic_FromSyntaxNode_WithAdditionalLocationsAndProperties_PassesBothThrough()
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText("class C { void M() {} int X; }");
        SyntaxNode root = tree.GetRoot();
        MethodDeclarationSyntax method = root.DescendantNodes().OfType<MethodDeclarationSyntax>().First();
        FieldDeclarationSyntax field = root.DescendantNodes().OfType<FieldDeclarationSyntax>().First();

        Location additionalLocation = field.GetLocation();
        ImmutableDictionary<string, string?> properties = ImmutableDictionary<string, string?>.Empty.Add("Scope", "Class");

#pragma warning disable ECS0900 // Minimize boxing and unboxing (messageArgs is params object[])
        Diagnostic diag = method.CreateDiagnostic(TestRule, new[] { additionalLocation }, properties, "arg1");
#pragma warning restore ECS0900

        Assert.Equal("TEST0001", diag.Id);
        Assert.Equal("Class", diag.Properties["Scope"]);
        Assert.True(diag.Location.IsInSource);
        Assert.Single(diag.AdditionalLocations);
        Assert.Equal(additionalLocation, diag.AdditionalLocations[0]);
    }

    // Overload #4: Location + rule + messageArgs (basic)
    [Fact]
    public void CreateDiagnostic_FromLocation_Basic_CreatesDiagnosticWithLocation()
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText("class C { void M() {} }");
        Location location = tree.GetRoot().GetLocation();

        Diagnostic diag = location.CreateDiagnostic(TestRule);

        Assert.Equal("TEST0001", diag.Id);
        Assert.True(diag.Location.IsInSource);
        Assert.Equal(location.SourceSpan, diag.Location.SourceSpan);
    }

    // Overload #5: Location + rule + properties + messageArgs (existing test)
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

    // Overload #6: Location + rule + additionalLocations + properties + messageArgs (CORE)
    // Branch: location.IsInSource == true
    [Fact]
    public void CreateDiagnostic_FromLocation_WithAllParameters_WhenInSource_PreservesLocation()
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText("class C { void M() {} int X; }");
        SyntaxNode root = tree.GetRoot();
        Location primaryLocation = root.DescendantNodes().OfType<MethodDeclarationSyntax>().First().GetLocation();
        Location additionalLocation = root.DescendantNodes().OfType<FieldDeclarationSyntax>().First().GetLocation();
        ImmutableDictionary<string, string?> properties = ImmutableDictionary<string, string?>.Empty.Add("Detail", "Full");

#pragma warning disable ECS0900 // Minimize boxing and unboxing (messageArgs is params object[])
        Diagnostic diag = primaryLocation.CreateDiagnostic(
            TestRule,
            new[] { additionalLocation },
            properties,
            "formatted");
#pragma warning restore ECS0900

        Assert.Equal("TEST0001", diag.Id);
        Assert.True(diag.Location.IsInSource);
        Assert.Equal(primaryLocation.SourceSpan, diag.Location.SourceSpan);
        Assert.Single(diag.AdditionalLocations);
        Assert.Equal(additionalLocation, diag.AdditionalLocations[0]);
        Assert.Equal("Full", diag.Properties["Detail"]);
    }

    // Overload #6: Branch: location.IsInSource == false
    [Fact]
    public void CreateDiagnostic_FromLocation_WithAllParameters_WhenNotInSource_ReplacesWithLocationNone()
    {
        // Location.Create with a file path (not from a SyntaxTree) produces a non-source location
        Location nonSourceLocation = Location.Create(
            "test.cs",
            TextSpan.FromBounds(0, 0),
            default(LinePositionSpan));
        ImmutableDictionary<string, string?> properties = ImmutableDictionary<string, string?>.Empty.Add("K", "V");

#pragma warning disable ECS0900 // Minimize boxing and unboxing (messageArgs is params object[])
        Diagnostic diag = nonSourceLocation.CreateDiagnostic(
            TestRule,
            additionalLocations: null,
            properties,
            "arg");
#pragma warning restore ECS0900

        Assert.Equal("TEST0001", diag.Id);
        Assert.Equal(Location.None, diag.Location);
        Assert.Equal("V", diag.Properties["K"]);
    }

    // Overload #7: IOperation + rule + messageArgs (existing test)
    [Fact]
    public void CreateDiagnostic_FromOperation_DelegatesToSyntax()
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText("class C { void M() {} }");
        SyntaxNode root = tree.GetRoot();
        CSharpCompilation compilation = CSharpCompilation.Create(
            "Test",
            new[] { tree },
            CompilationHelper.CoreReferences,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        SemanticModel model = compilation.GetSemanticModel(tree);
        MethodDeclarationSyntax methodDecl = root.DescendantNodes().OfType<MethodDeclarationSyntax>().First();
        Microsoft.CodeAnalysis.IOperation? operation = model.GetOperation(methodDecl);
        DiagnosticDescriptor rule = new DiagnosticDescriptor("TEST0003", "Test3", "Test message 3", "Test", DiagnosticSeverity.Error, true);
        Diagnostic diag = operation!.CreateDiagnostic(rule);
        Assert.Equal("TEST0003", diag.Id);
        Assert.Equal(DiagnosticSeverity.Error, diag.Severity);
    }

    // Overload #8: IOperation + rule + properties + messageArgs (existing test)
    [Fact]
    public void CreateDiagnostic_FromOperation_WithProperties()
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText("class C { void M() {} }");
        SyntaxNode root = tree.GetRoot();
        CSharpCompilation compilation = CSharpCompilation.Create(
            "Test",
            new[] { tree },
            CompilationHelper.CoreReferences,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        SemanticModel model = compilation.GetSemanticModel(tree);
        MethodDeclarationSyntax methodDecl = root.DescendantNodes().OfType<MethodDeclarationSyntax>().First();
        Microsoft.CodeAnalysis.IOperation? operation = model.GetOperation(methodDecl);
        DiagnosticDescriptor rule = new DiagnosticDescriptor("TEST0004", "Test4", "Test message 4", "Test", DiagnosticSeverity.Warning, true);
        ImmutableDictionary<string, string?> properties = ImmutableDictionary<string, string?>.Empty.Add("Key2", "Value2");
        Diagnostic diag = operation!.CreateDiagnostic(rule, properties);
        Assert.Equal("TEST0004", diag.Id);
        Assert.Equal("Value2", diag.Properties["Key2"]);
    }

    // Overload #9: IOperation + rule + additionalLocations + properties + messageArgs
    [Fact]
    public void CreateDiagnostic_FromOperation_WithAdditionalLocationsAndProperties_DelegatesToSyntax()
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText("class C { void M() {} int X; }");
        SyntaxNode root = tree.GetRoot();
        CSharpCompilation compilation = CSharpCompilation.Create(
            "Test",
            new[] { tree },
            CompilationHelper.CoreReferences,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        SemanticModel model = compilation.GetSemanticModel(tree);
        MethodDeclarationSyntax methodDecl = root.DescendantNodes().OfType<MethodDeclarationSyntax>().First();
        Microsoft.CodeAnalysis.IOperation? operation = model.GetOperation(methodDecl);
        Location additionalLocation = root.DescendantNodes().OfType<FieldDeclarationSyntax>().First().GetLocation();
        ImmutableDictionary<string, string?> properties = ImmutableDictionary<string, string?>.Empty.Add("OpKey", "OpVal");

#pragma warning disable ECS0900 // Minimize boxing and unboxing (messageArgs is params object[])
        Diagnostic diag = operation!.CreateDiagnostic(
            TestRule,
            new[] { additionalLocation },
            properties,
            "opArg");
#pragma warning restore ECS0900

        Assert.Equal("TEST0001", diag.Id);
        Assert.True(diag.Location.IsInSource);
        Assert.Single(diag.AdditionalLocations);
        Assert.Equal(additionalLocation, diag.AdditionalLocations[0]);
        Assert.Equal("OpVal", diag.Properties["OpKey"]);
    }
}
