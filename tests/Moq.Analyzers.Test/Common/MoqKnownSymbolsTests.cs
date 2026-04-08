using Moq.Analyzers.Common.WellKnown;
using Moq.Analyzers.Test.Helpers;

namespace Moq.Analyzers.Test.Common;

public partial class MoqKnownSymbolsTests
{
#pragma warning disable ECS1300 // Static field init is simpler than static constructor for single field
    private static readonly MetadataReference SystemThreadingTasksReference =
        MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location);
#pragma warning restore ECS1300

    private static MetadataReference[] CoreReferencesWithTasks =>
        [CompilationHelper.CorlibReference, CompilationHelper.SystemRuntimeReference, SystemThreadingTasksReference, CompilationHelper.SystemLinqReference];

    [Fact]
    public void Constructor_WithCompilation_CreatesInstance()
    {
        CSharpCompilation compilation = CreateMinimalCompilation();

        MoqKnownSymbols symbols = new MoqKnownSymbols(compilation);

        // Verify it does not throw and the object is usable.
        Assert.Null(symbols.Mock);
    }

    [Fact]
    public void Constructor_WithWellKnownTypeProvider_CreatesInstance()
    {
        CSharpCompilation compilation = CreateMinimalCompilation();
        Analyzer.Utilities.WellKnownTypeProvider typeProvider =
            Analyzer.Utilities.WellKnownTypeProvider.GetOrCreate(compilation);

        MoqKnownSymbols symbols = new MoqKnownSymbols(typeProvider);

        Assert.Null(symbols.Mock);
    }

    [Fact]
    public void Constructor_WithNullTypeProvider_ThrowsArgumentNullException()
    {
        Analyzer.Utilities.WellKnownTypeProvider? nullProvider = null;

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => new MoqKnownSymbols(nullProvider!));

        Assert.Equal("typeProvider", ex.ParamName);
    }

    [Fact]
    public async Task BothConstructors_ProduceSameResults_ForMockType()
    {
        CSharpCompilation compilation = await CreateMoqCompilationAsync();
        Analyzer.Utilities.WellKnownTypeProvider typeProvider =
            Analyzer.Utilities.WellKnownTypeProvider.GetOrCreate(compilation);

        MoqKnownSymbols fromCompilation = new MoqKnownSymbols(compilation);
        MoqKnownSymbols fromProvider = new MoqKnownSymbols(typeProvider);

        Assert.NotNull(fromCompilation.Mock);
        Assert.NotNull(fromProvider.Mock);
        Assert.True(SymbolEqualityComparer.Default.Equals(fromCompilation.Mock, fromProvider.Mock));
    }

    [Fact]
    public async Task BothConstructors_ProduceSameResults_ForMock1Type()
    {
        CSharpCompilation compilation = await CreateMoqCompilationAsync();
        Analyzer.Utilities.WellKnownTypeProvider typeProvider =
            Analyzer.Utilities.WellKnownTypeProvider.GetOrCreate(compilation);

        MoqKnownSymbols fromCompilation = new MoqKnownSymbols(compilation);
        MoqKnownSymbols fromProvider = new MoqKnownSymbols(typeProvider);

        Assert.NotNull(fromCompilation.Mock1);
        Assert.NotNull(fromProvider.Mock1);
        Assert.True(SymbolEqualityComparer.Default.Equals(fromCompilation.Mock1, fromProvider.Mock1));
    }

    private static CSharpCompilation CreateMinimalCompilation()
    {
        (SemanticModel model, _) = CompilationHelper.CreateCompilation("public class Empty { }", CoreReferencesWithTasks);
        return (CSharpCompilation)model.Compilation;
    }

    private static MoqKnownSymbols CreateSymbolsWithoutMoq()
    {
        return new MoqKnownSymbols(CreateMinimalCompilation());
    }

    private static async Task<MoqKnownSymbols> CreateSymbolsWithMoqAsync()
    {
        return new MoqKnownSymbols(await CreateMoqCompilationAsync().ConfigureAwait(false));
    }

    private static async Task<CSharpCompilation> CreateMoqCompilationAsync()
    {
        (SemanticModel model, _) = await CompilationHelper.CreateMoqCompilationAsync("public class Empty { }").ConfigureAwait(false);
        return (CSharpCompilation)model.Compilation;
    }
}
