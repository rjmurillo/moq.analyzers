using Moq.Analyzers.Common.WellKnown;
using Moq.Analyzers.Test.Helpers;

namespace Moq.Analyzers.Test.Common;

public class MoqKnownSymbolExtensionsTests
{
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
        (SemanticModel model, _) = CompilationHelper.CreateCompilation("public class Empty { }");
        return new MoqKnownSymbols(model.Compilation);
    }

    private static async Task<MoqKnownSymbols> CreateSymbolsWithMoqAsync()
    {
        (SemanticModel model, _) = await CompilationHelper.CreateMoqCompilationAsync("public class Empty { }").ConfigureAwait(false);
        return new MoqKnownSymbols(model.Compilation);
    }
}
