using Microsoft.CodeAnalysis.Testing;

namespace Moq.Analyzers.Test.Helpers;

public class ReferenceAssemblyCatalogTests
{
    [Fact]
    public async Task Net90WithNewMoq_CompilesMoqSnippet()
    {
        ReferenceAssemblies referenceAssemblies = ReferenceAssemblyCatalog.Catalog[ReferenceAssemblyCatalog.Net90WithNewMoq];
        ImmutableArray<MetadataReference> references = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
        MetadataReference[] metadataReferences = [.. references];
        SyntaxTree tree = CSharpSyntaxTree.ParseText(
            """
            using Moq;

            internal class UnitTest
            {
                private readonly Mock<object> mock = new();
            }
            """);
        CSharpCompilation compilation = CSharpCompilation.Create(
            "Net90WithNewMoqSmokeTest",
            [tree],
            metadataReferences,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        Diagnostic[] errors = [.. compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)];
        Assert.Empty(errors);
    }
}
