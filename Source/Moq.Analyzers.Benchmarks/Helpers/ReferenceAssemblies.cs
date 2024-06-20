//using System.Collections.Immutable;
//using Microsoft.CodeAnalysis;

//namespace Moq.Analyzers.Benchmarks.Helpers;

//// Originally from https://github.com/dotnet/roslyn-analyzers/blob/f1115edce8633ebe03a86191bc05c6969ed9a821/src/PerformanceTests/Utilities/Common/ReferenceAssemblies.cs
//internal sealed class ReferenceAssemblies
//{
//    private readonly ImmutableArray<PortableExecutableReference> _assemblies;

//    public ReferenceAssemblies(string targetFramework, ImmutableArray<PortableExecutableReference> assemblies)
//    {
//        _assemblies = assemblies;
//        TargetFramework = targetFramework ?? throw new ArgumentNullException(nameof(targetFramework));
//        AssemblyIdentityComparer = AssemblyIdentityComparer.Default;
//        ReferenceAssemblyPath = null;
//        Assemblies = ImmutableArray<string>.Empty;
//        FacadeAssemblies = ImmutableArray<string>.Empty;
//        LanguageSpecificAssemblies = ImmutableDictionary<string, ImmutableArray<string>>.Empty;
//    }

//    public static ReferenceAssemblies Default => Net.Net60;

//    public string TargetFramework { get; }

//    public AssemblyIdentityComparer AssemblyIdentityComparer { get; }

//    public string? ReferenceAssemblyPath { get; }

//    public ImmutableArray<string> Assemblies { get; }

//    public ImmutableArray<string> FacadeAssemblies { get; }

//    public ImmutableDictionary<string, ImmutableArray<string>> LanguageSpecificAssemblies { get; }

//    public string? NuGetConfigFilePath { get; }

//    public Task<ImmutableArray<MetadataReference>> ResolveAsync(string? language)
//    {
//        ImmutableArray<MetadataReference>.Builder references = ImmutableArray.CreateBuilder<MetadataReference>();
//        if (string.Equals(language, LanguageNames.CSharp, StringComparison.Ordinal))
//        {
//            foreach (PortableExecutableReference assembly in _assemblies)
//            {
//                if (assembly.FilePath?.Contains("VisualBasic") == false)
//                {
//                    references.Add(assembly);
//                }
//            }

//            return Task.FromResult(references.ToImmutable());
//        }

//        return Task.FromResult(_assemblies.CastArray<MetadataReference>());
//    }

//    public static class Net
//    {
//        private static readonly Lazy<ReferenceAssemblies> _lazyNet60 =
//            new(() => new ReferenceAssemblies("net6.0", Microsoft.CodeAnalysis.Testing.ReferenceAssemblies.Net60.All.ToImmutableArray()));

//        public static ReferenceAssemblies Net60 => _lazyNet60.Value;
//    }
//}
