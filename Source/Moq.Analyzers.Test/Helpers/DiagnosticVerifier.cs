using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Moq.Analyzers.Test.Helpers;

public abstract class DiagnosticVerifier<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    protected async Task VerifyCSharpDiagnostic(string source)
    {
        CSharpAnalyzerTest<TAnalyzer, DefaultVerifier> context = new()
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80.AddPackages([new PackageIdentity("Moq", "4.8.2")]), // TODO: See https://github.com/Litee/moq.analyzers/issues/58
            TestCode = source,
            TestBehaviors = TestBehaviors.SkipGeneratedCodeCheck, // TODO: We should enable the generated code check
        };

        context.SolutionTransforms.Add((solution, projectId) =>
        {
            return solution.WithProjectParseOptions(projectId, new CSharpParseOptions(
                languageVersion: LanguageVersion.Latest));
        });

        await context.RunAsync().ConfigureAwait(false);
    }
}
