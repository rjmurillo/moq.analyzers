using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace Moq.Analyzers.Test;

// TODO: These tests should be broken down further
public class CallbackSignatureShouldMatchMockedMethodAnalyzerTests : DiagnosticVerifier
{
    [Fact]
    public Task ShouldPassWhenCorrectSetupAndReturns()
    {
        return Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/CallbackSignatureShouldMatchMockedMethod.MyGoodSetupAndReturns.cs")));
    }

    [Fact]
    public Task ShouldFailWhenIncorrectCallbacks()
    {
        return Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/CallbackSignatureShouldMatchMockedMethod.TestBadCallbacks.cs")));
    }

    [Fact]
    public Task ShouldPassWhenCorrectSetupAndCallbacks()
    {
        return Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/CallbackSignatureShouldMatchMockedMethod.TestGoodSetupAndCallback.cs")));
    }

    [Fact]
    public Task ShouldPassWhenCorrectSetupAndParameterlessCallbacks()
    {
        return Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/CallbackSignatureShouldMatchMockedMethod.TestGoodSetupAndParameterlessCallback.cs")));
    }

    [Fact]
    public Task ShouldPassWhenCorrectSetupAndReturnsAndCallbacks()
    {
        return Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/CallbackSignatureShouldMatchMockedMethod.TestGoodSetupAndReturnsAndCallback.cs")));
    }

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        return new CallbackSignatureShouldMatchMockedMethodAnalyzer();
    }
}
