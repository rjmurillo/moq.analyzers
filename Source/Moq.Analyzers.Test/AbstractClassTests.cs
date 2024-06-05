using System.IO;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace Moq.Analyzers.Test;

public class AbstractClassTests : DiagnosticVerifier
{
    [Fact]
    public Task ShouldFailOnGenericTypesWithMismatchArgs()
    {
        return Verify(VerifyCSharpDiagnostic(
            [
                File.ReadAllText("Data/AbstractClass.GenericMismatchArgs.cs")
            ]));
    }

    [Fact]
    public Task ShouldPassOnGenericTypesWithNoArgs()
    {
        return Verify(VerifyCSharpDiagnostic(
            [
                File.ReadAllText("Data/AbstractClass.GenericNoArgs.cs")
            ]));
    }

    [Fact]
    public Task ShouldFailOnMismatchArgs()
    {
        return Verify(VerifyCSharpDiagnostic(
            [
                File.ReadAllText("Data/AbstractClass.MismatchArgs.cs")
            ]));
    }

    [Fact]
    public Task ShouldPassWithNoArgs()
    {
        return Verify(VerifyCSharpDiagnostic(
            [
                File.ReadAllText("Data/AbstractClass.NoArgs.cs")
            ]));
    }

    [Fact(Skip = "I think this _should_ fail, but currently passes. Tracked by #55.")]
    public Task ShouldFailWithArgsNonePassed()
    {
        return Verify(VerifyCSharpDiagnostic(
            [
                File.ReadAllText("Data/AbstractClass.WithArgsNonePassed.cs")
            ]));
    }

    [Fact]
    public Task ShouldPassWithArgsPassed()
    {
        return Verify(VerifyCSharpDiagnostic(
            [
                File.ReadAllText("Data/AbstractClass.WithArgsPassed.cs")
            ]));
    }

    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        return new ConstructorArgumentsShouldMatchAnalyzer();
    }
}
