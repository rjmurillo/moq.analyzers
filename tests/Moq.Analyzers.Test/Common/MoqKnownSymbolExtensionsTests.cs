using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Testing;
using Moq.Analyzers.Common.WellKnown;
using Moq.Analyzers.Test.Helpers;

namespace Moq.Analyzers.Test.Common;

public class MoqKnownSymbolExtensionsTests
{
    private static readonly MetadataReference CorlibReference;
    private static readonly MetadataReference SystemRuntimeReference;

#pragma warning disable S3963 // "static fields" should be initialized inline - conflicts with ECS1300
    static MoqKnownSymbolExtensionsTests()
    {
        CorlibReference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        string runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        SystemRuntimeReference = MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Runtime.dll"));
    }
#pragma warning restore S3963

    private static MetadataReference[] CoreReferences =>
        [CorlibReference, SystemRuntimeReference];

    [Fact]
    public async Task IsMockReferenced_WithMoqCompilation_ReturnsTrue()
    {
        MoqKnownSymbols symbols = await CreateSymbolsWithMoqAsync();

        Assert.True(symbols.IsMockReferenced());
    }

    [Fact]
    public void IsMockReferenced_WithoutMoqCompilation_ReturnsFalse()
    {
        MoqKnownSymbols symbols = CreateSymbolsWithoutMoq();

        Assert.False(symbols.IsMockReferenced());
    }

    private static MoqKnownSymbols CreateSymbolsWithoutMoq()
    {
        return new MoqKnownSymbols(CreateMinimalCompilation());
    }

    private static CSharpCompilation CreateMinimalCompilation()
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText("public class Empty { }");
        return CSharpCompilation.Create(
            "TestAssembly",
            new[] { tree },
            CoreReferences,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static async Task<MoqKnownSymbols> CreateSymbolsWithMoqAsync()
    {
        return new MoqKnownSymbols(await CreateMoqCompilationAsync().ConfigureAwait(false));
    }

    private static async Task<CSharpCompilation> CreateMoqCompilationAsync()
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText("public class Empty { }");
        MetadataReference[] references = await GetMoqReferencesAsync().ConfigureAwait(false);
        return CSharpCompilation.Create(
            "TestAssembly",
            new[] { tree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static async Task<MetadataReference[]> GetMoqReferencesAsync()
    {
        ReferenceAssemblies referenceAssemblies = ReferenceAssemblyCatalog.Catalog[ReferenceAssemblyCatalog.Net80WithNewMoq];
        ImmutableArray<MetadataReference> resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None).ConfigureAwait(false);
        return [.. resolved];
    }
}
