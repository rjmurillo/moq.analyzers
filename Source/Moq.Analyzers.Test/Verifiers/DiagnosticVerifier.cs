using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace TestHelper
{
    /// <summary>
    /// Superclass of all Unit Tests for DiagnosticAnalyzers
    /// </summary>
    public abstract partial class DiagnosticVerifier
    {
        /// <summary>
        /// Get the CSharp analyzer being tested - to be implemented in non-abstract class
        /// </summary>
        protected virtual DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return null;
        }

        /// <summary>
        /// Called to test a C# DiagnosticAnalyzer when applied on the single inputted string as a source
        /// Note: input a DiagnosticResult for each Diagnostic expected
        /// </summary>
        /// <param name="source">A class in the form of a string to run the analyzer on</param>
        protected string VerifyCSharpDiagnostic(string source)
        {
            return VerifyDiagnostics(new[] { source }, LanguageNames.CSharp, GetCSharpDiagnosticAnalyzer());
        }

        /// <summary>
        /// Called to test a C# DiagnosticAnalyzer when applied on the inputted strings as a source
        /// Note: input a DiagnosticResult for each Diagnostic expected
        /// </summary>
        /// <param name="sources">An array of strings to create source documents from to run the analyzers on</param>
        protected string VerifyCSharpDiagnostic(string[] sources)
        {
            return VerifyDiagnostics(sources, LanguageNames.CSharp, GetCSharpDiagnosticAnalyzer());
        }

        /// <summary>
        /// General method that gets a collection of actual diagnostics found in the source after the analyzer is run, 
        /// then verifies each of them.
        /// </summary>
        /// <param name="sources">An array of strings to create source documents from to run the analyzers on</param>
        /// <param name="language">The language of the classes represented by the source strings</param>
        /// <param name="analyzer">The analyzer to be run on the source code</param>
        private string VerifyDiagnostics(string[] sources, string language, DiagnosticAnalyzer analyzer)
        {
            var diagnostics = GetSortedDiagnostics(sources, language, analyzer);
            return VerifyDiagnosticResults(diagnostics, analyzer);
        }

        /// <summary>
        /// Checks each of the actual Diagnostics found and compares them with the corresponding DiagnosticResult in the array of expected results.
        /// Diagnostics are considered equal only if the DiagnosticResultLocation, Id, Severity, and Message of the DiagnosticResult match the actual diagnostic.
        /// </summary>
        /// <param name="actualResults">The Diagnostics found by the compiler after running the analyzer on the source code</param>
        /// <param name="analyzer">The analyzer that was being run on the sources</param>
        private static string VerifyDiagnosticResults(IEnumerable<Diagnostic> actualResults, DiagnosticAnalyzer analyzer)
        {
            StringBuilder result = new StringBuilder();
            int i = 1;
            foreach (var diagnostic in actualResults)
            {
                result.AppendLine("Diagnostic " + i);
                result.AppendLine("\tId: " + diagnostic.Id);
                result.AppendLine("\tLocation: " + diagnostic.Location);
                result.AppendLine("\tSeverity: " + diagnostic.Severity);
                result.AppendLine("\tMessage: " + diagnostic.GetMessage());
                result.AppendLine();

                i += 1;
            }
            return result.ToString();
        }
      
    }
}
