using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Moq.Analyzers.Test.Helpers;

namespace Moq.Analyzers.CSharp14.Test;

internal static class CSharp14AnalyzerVerifier<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    public static async Task VerifyAnalyzerAsync(string source)
    {
        CSharpCodeFixTest<TAnalyzer, EmptyCodeFixProvider, DefaultVerifier> test = CreateTest(source);

        await test.RunAsync().ConfigureAwait(false);
    }

    public static async Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
    {
        CSharpCodeFixTest<TAnalyzer, EmptyCodeFixProvider, DefaultVerifier> test = CreateTest(source);

        test.ExpectedDiagnostics.AddRange(expected);

        await test.RunAsync().ConfigureAwait(false);
    }

    private static CSharpCodeFixTest<TAnalyzer, EmptyCodeFixProvider, DefaultVerifier> CreateTest(string source)
    {
        const string globalUsings =
            """
            global using System;
            global using System.Collections.Generic;
            global using System.Threading.Tasks;
            global using Moq;
            """;

        CSharpCodeFixTest<TAnalyzer, EmptyCodeFixProvider, DefaultVerifier> test = new()
        {
            TestCode = source,
            FixedCode = source,
            ReferenceAssemblies = ReferenceAssemblyCatalog.Catalog[ReferenceAssemblyCatalog.Net90WithNewMoq],
        };

        test.TestState.Sources.Add(globalUsings);
        test.FixedState.Sources.Add(globalUsings);
        test.SolutionTransforms.Add((solution, projectId) =>
        {
            Project project = solution.GetProject(projectId) ?? throw new InvalidOperationException("The C# 14 test project was not found.");
            CSharpParseOptions parseOptions = (CSharpParseOptions?)project.ParseOptions ?? CSharpParseOptions.Default;

            return solution.WithProjectParseOptions(projectId, parseOptions.WithLanguageVersion(LanguageVersion.Preview));
        });

        return test;
    }
}
