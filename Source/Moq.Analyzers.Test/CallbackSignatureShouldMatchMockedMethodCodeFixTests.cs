using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace Moq.Analyzers.Test;

public class CallbackSignatureShouldMatchMockedMethodCodeFixTests : CodeFixVerifier
{
    [Fact]
    public Task ShouldPassWhenCorrectSetupAndReturns()
    {
        return Verify(VerifyCSharpFix(File.ReadAllText("Data/CallbackSignatureShouldMatchMockedMethod.MyGoodSetupAndReturns.cs")));
    }

    [Fact]
    public Task ShouldSuggestQuickFixWhenIncorrectCallbacks()
    {
        return Verify(VerifyCSharpFix(File.ReadAllText("Data/CallbackSignatureShouldMatchMockedMethod.TestBadCallbacks.cs")));
    }

    [Fact]
    public Task ShouldPassWhenCorrectSetupAndCallbacks()
    {
        return Verify(VerifyCSharpFix(File.ReadAllText("Data/CallbackSignatureShouldMatchMockedMethod.TestGoodSetupAndCallback.cs")));
    }

    [Fact]
    public Task ShouldPassWhenCorrectSetupAndParameterlessCallbacks()
    {
        return Verify(VerifyCSharpFix(File.ReadAllText("Data/CallbackSignatureShouldMatchMockedMethod.TestGoodSetupAndParameterlessCallback.cs")));
    }

    [Fact]
    public Task ShouldPassWhenCorrectSetupAndReturnsAndCallbacks()
    {
        return Verify(VerifyCSharpFix(File.ReadAllText("Data/CallbackSignatureShouldMatchMockedMethod.TestGoodSetupAndReturnsAndCallback.cs")));
    }

    protected override CodeFixProvider GetCSharpCodeFixProvider()
    {
        return new CallbackSignatureShouldMatchMockedMethodCodeFix();
    }

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        return new CallbackSignatureShouldMatchMockedMethodAnalyzer();
    }
}
