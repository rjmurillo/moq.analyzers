using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Moq.Analyzers.Test;

public class CallbackSignatureShouldMatchMockedMethodAnalyzerTests : CallbackSignatureShouldMatchMockedMethodBase
{
    [Fact]
    public Task ShouldPassWhenCorrectSetupAndReturns()
    {
        return Verify(VerifyCSharpDiagnostic(GoodSetupAndCallback));
    }

    [Fact]
    public Task ShouldFailWhenIncorrectCallbacks()
    {
        return Verify(VerifyCSharpDiagnostic(BadCallbacks));
    }

    [Fact]
    public Task ShouldPassWhenCorrectSetupAndCallbacks()
    {
        return Verify(VerifyCSharpDiagnostic(GoodSetupAndCallback));
    }

    [Fact]
    public Task ShouldPassWhenCorrectSetupAndParameterlessCallbacks()
    {
        return Verify(VerifyCSharpDiagnostic(GoodSetupAndParameterlessCallback));
    }

    [Fact]
    public Task ShouldPassWhenCorrectSetupAndReturnsAndCallbacks()
    {
        return Verify(VerifyCSharpDiagnostic(GoodSetupAndReturnsAndCallback));
    }
}
