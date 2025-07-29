; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules
Rule ID | Category | Severity | Notes
--------|----------|----------|-------
Moq1001 | Usage | Warning | NoConstructorArgumentsForInterfaceMockRuleId (updated category from Moq to Usage)
Moq1002 | Usage | Warning | NoMatchingConstructorRuleId (updated category from Moq to Usage)
Moq1200 | Usage | Error | SetupShouldBeUsedOnlyForOverridableMembersAnalyzer (updated category from Moq to Usage)
Moq1202 | Moq | Warning | RaiseEventArgumentsShouldMatchEventSignatureAnalyzer
Moq1203 | Moq | Warning | MethodSetupShouldSpecifyReturnValueAnalyzer
Moq1204 | Usage | Warning | RaisesEventArgumentsShouldMatchEventSignatureAnalyzer (updated category from Moq to Usage)
Moq1205 | Moq | Warning | EventSetupHandlerShouldMatchEventTypeAnalyzer
Moq1206 | Moq | Warning | ReturnsAsyncShouldBeUsedForAsyncMethodsAnalyzer
Moq1207 | Usage | Error | SetupSequenceShouldBeUsedOnlyForOverridableMembersAnalyzer (updated category from Moq to Usage)
Moq1210 | Moq | Error | VerifyShouldBeUsedOnlyForOverridableMembersAnalyzer
Moq1300 | Usage | Error | AsShouldBeUsedOnlyForInterfaceAnalyzer (updated category from Moq to Usage)
Moq1301 | Moq | Warning | Mock.Get() should not take literals
Moq1302 | Moq | Warning | LINQ to Mocks expression should be valid (flags non-virtual members including fields, events, nested and chained accesses)
Moq1420 | Moq | Info | RedundantTimesSpecificationAnalyzer
Moq1500 | Moq | Warning | MockRepository.Verify() should be called
