using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Moq.Analyzers.Test.Helpers;

public abstract class DiagnosticVerifier<TAnalyzer> : CodeFixVerifier<TAnalyzer, EmptyCodeFixProvider>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    protected async Task VerifyCSharpDiagnostic(string source)
    {
        await VerifyCSharpFix(source, source);
    }
}
