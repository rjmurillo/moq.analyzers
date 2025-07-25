; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules
Rule ID | Category | Severity | Notes
--------|----------|----------|-------
Moq1202 | Moq | Warning | RaiseEventArgumentsShouldMatchEventSignatureAnalyzer
Moq1203 | Moq | Warning | MethodSetupShouldSpecifyReturnValueAnalyzer
Moq1204 | Moq | Warning | RaisesEventArgumentsShouldMatchEventSignatureAnalyzer
Moq1205 | Moq | Warning | EventSetupHandlerShouldMatchEventTypeAnalyzer
Moq1206 | Moq | Warning | ReturnsAsyncShouldBeUsedForAsyncMethodsAnalyzer
Moq1210 | Moq | Error | VerifyShouldBeUsedOnlyForOverridableMembersAnalyzer
Moq1301 | Moq | Warning | Mock.Get() should not take literals
Moq1302 | Moq | Warning | LINQ to Mocks expression should be valid (flags non-virtual members including fields, events, nested and chained accesses)
Moq1420 | Moq | Info | RedundantTimesSpecificationAnalyzer
Moq1500 | Moq | Warning | MockRepository.Verify() should be called
