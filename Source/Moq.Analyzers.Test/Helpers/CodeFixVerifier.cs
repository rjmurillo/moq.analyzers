using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Moq.Analyzers.Test.Helpers;

public abstract class CodeFixVerifier<TAnalyzer, TCodeFixProvider>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFixProvider : CodeFixProvider, new()
{
    protected async Task VerifyCSharpFix(string originalSource, string fixedSource)
    {
        CSharpCodeFixTest<TAnalyzer, TCodeFixProvider, DefaultVerifier> context = new()
        {
            // TODO: Refactor this out
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80.AddPackages([new PackageIdentity("Moq", "4.8.2")]), // TODO: See https://github.com/Litee/moq.analyzers/issues/58
            TestBehaviors = TestBehaviors.SkipGeneratedCodeCheck, // TODO: We should enable the generated code check
            TestCode = originalSource,
            FixedCode = fixedSource,
        };

        await context.RunAsync().ConfigureAwait(false);
    }
}
