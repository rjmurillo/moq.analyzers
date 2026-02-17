using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Testing;

namespace Moq.Analyzers.Test.Helpers;

/// <summary>
/// Shared compilation boilerplate for unit tests that need Roslyn
/// <see cref="SemanticModel"/> and <see cref="SyntaxTree"/> instances.
/// Eliminates per-class duplication of reference resolution and compilation creation.
/// </summary>
internal static class CompilationHelper
{
    internal static readonly MetadataReference CorlibReference;
    internal static readonly MetadataReference SystemRuntimeReference;
    internal static readonly MetadataReference SystemLinqReference;
    internal static readonly MetadataReference SystemLinqExpressionsReference;

#pragma warning disable S3963 // "static fields" should be initialized inline - multiple fields depend on runtimeDir
    static CompilationHelper()
    {
        CorlibReference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        string runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        SystemRuntimeReference = MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Runtime.dll"));
        SystemLinqReference = MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location);
        SystemLinqExpressionsReference = MetadataReference.CreateFromFile(typeof(System.Linq.Expressions.Expression).Assembly.Location);
    }
#pragma warning restore S3963

    /// <summary>
    /// Gets the minimal set of references needed for most compilations: corlib + System.Runtime.
    /// </summary>
    internal static MetadataReference[] CoreReferences => [CorlibReference, SystemRuntimeReference];

    /// <summary>
    /// Gets core references plus System.Linq and System.Linq.Expressions.
    /// Used by tests that compile code containing LINQ or expression trees.
    /// </summary>
    internal static MetadataReference[] CoreReferencesWithLinq =>
        [CorlibReference, SystemRuntimeReference, SystemLinqReference, SystemLinqExpressionsReference];

    /// <summary>
    /// Creates a <see cref="CSharpCompilation"/> from source code and returns the semantic model and syntax tree.
    /// </summary>
    /// <param name="code">C# source code to compile.</param>
    /// <param name="references">
    /// Metadata references to use. Defaults to <see cref="CoreReferences"/> when null.
    /// </param>
    /// <returns>A tuple of the <see cref="SemanticModel"/> and <see cref="SyntaxTree"/>.</returns>
    internal static (SemanticModel Model, SyntaxTree Tree) CreateCompilation(
        string code,
        MetadataReference[]? references = null)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        CSharpCompilation compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { tree },
            references ?? CoreReferences,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        SemanticModel model = compilation.GetSemanticModel(tree);
        return (model, tree);
    }

    /// <summary>
    /// Creates a compilation that includes Moq package references resolved via NuGet.
    /// </summary>
    /// <param name="code">C# source code to compile.</param>
    /// <returns>A tuple of the <see cref="SemanticModel"/> and <see cref="SyntaxTree"/>.</returns>
    internal static async Task<(SemanticModel Model, SyntaxTree Tree)> CreateMoqCompilationAsync(string code)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        MetadataReference[] references = await GetMoqReferencesAsync().ConfigureAwait(false);
        CSharpCompilation compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { tree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        SemanticModel model = compilation.GetSemanticModel(tree);
        return (model, tree);
    }

    /// <summary>
    /// Resolves Moq NuGet package references for use in test compilations.
    /// </summary>
    /// <returns>An array of <see cref="MetadataReference"/> including Moq and its dependencies.</returns>
    internal static async Task<MetadataReference[]> GetMoqReferencesAsync()
    {
        ReferenceAssemblies referenceAssemblies = ReferenceAssemblyCatalog.Catalog[ReferenceAssemblyCatalog.Net80WithNewMoq];
        ImmutableArray<MetadataReference> resolved = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None).ConfigureAwait(false);
        return [.. resolved];
    }
}
