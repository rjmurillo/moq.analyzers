using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace Moq.Analyzers.Test;

public class SetupShouldNotIncludeAsyncResultAnalyzerTests : DiagnosticVerifier
{
    [Fact]
    public Task ShouldPassWhenSetupWithoutReturn()
    {
        return Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/SetupShouldNotIncludeAsyncResult.TestOkForTask.cs")));
    }

    [Fact]
    public Task ShouldPassWhenSetupWithReturnsAsync()
    {
        return Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/SetupShouldNotIncludeAsyncResult.TestOkForGenericTaskProperSetup.cs")));
    }

    [Fact]
    public Task ShouldFailWhenSetupWithTaskResult()
    {
        return Verify(VerifyCSharpDiagnostic(File.ReadAllText("Data/SetupShouldNotIncludeAsyncResult.TestBadForGenericTask.cs")));
    }

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        return new SetupShouldNotIncludeAsyncResultAnalyzer();
    }
}
