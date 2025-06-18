; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules
Rule ID | Category | Severity | Notes
--------|----------|----------|-------
Moq1202 | Moq | Warning | RaiseEventArgumentsShouldMatchEventSignatureAnalyzer
Moq1301 | Moq      | Warning    | Mock.Get() should not take literals
Moq1302 | Moq | Warning | LinqToMocksExpressionShouldBeValidAnalyzer, [Documentation](https://github.com/rjmurillo/moq.analyzers/blob/4f5f9cad067390e9937cfb5cde6d4c93b96f0e3c/docs/rules/Moq1302.md)
SIMPLE001 | Moq | Info | SimpleInvocationAnalyzer
