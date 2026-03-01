; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules
Rule ID | Category | Severity | Notes
--------|----------|----------|-------
Moq1000 | Usage | Warning | NoSealedClassMocksAnalyzer (updated category from Moq to Usage)
Moq1001 | Usage | Warning | NoConstructorArgumentsForInterfaceMockRuleId (updated category from Moq to Usage)
Moq1002 | Usage | Warning | NoMatchingConstructorRuleId (updated category from Moq to Usage)
Moq1003 | Usage | Warning | InternalTypeMustHaveInternalsVisibleToAnalyzer
Moq1100 | Usage | Warning | CallbackSignatureShouldMatchMockedMethodAnalyzer (updated category from Moq to Usage)
Moq1101 | Usage | Warning | NoMethodsInPropertySetupAnalyzer (updated category from Moq to Usage)
Moq1200 | Usage | Error | SetupShouldBeUsedOnlyForOverridableMembersAnalyzer (updated category from Moq to Usage)
Moq1201 | Usage | Error | SetupShouldNotIncludeAsyncResultAnalyzer (updated category from Moq to Usage)
Moq1202 | Usage | Warning | RaiseEventArgumentsShouldMatchEventSignatureAnalyzer (updated category from Moq to Usage)
Moq1203 | Usage | Warning | MethodSetupShouldSpecifyReturnValueAnalyzer (updated category from Moq to Usage)
Moq1204 | Usage | Warning | RaisesEventArgumentsShouldMatchEventSignatureAnalyzer (updated category from Moq to Usage)
Moq1205 | Usage | Warning | EventSetupHandlerShouldMatchEventTypeAnalyzer (updated category from Moq to Usage)
Moq1206 | Usage | Warning | ReturnsAsyncShouldBeUsedForAsyncMethodsAnalyzer (updated category from Moq to Usage)
Moq1207 | Usage | Error | SetupSequenceShouldBeUsedOnlyForOverridableMembersAnalyzer (updated category from Moq to Usage)
Moq1210 | Usage | Error | VerifyShouldBeUsedOnlyForOverridableMembersAnalyzer (updated category from Moq to Usage)
Moq1300 | Usage | Error | AsShouldBeUsedOnlyForInterfaceAnalyzer (updated category from Moq to Usage)
Moq1301 | Usage | Warning | Mock.Get() should not take literals (updated category from Moq to Usage)
Moq1302 | Usage | Warning | LINQ to Mocks expression should be valid (updated category from Moq to Usage)
Moq1400 | Usage | Warning | SetExplicitMockBehaviorAnalyzer (updated category from Moq to Usage)
Moq1410 | Usage | Info | SetStrictMockBehaviorAnalyzer (updated category from Moq to Usage)
Moq1420 | Usage | Info | RedundantTimesSpecificationAnalyzer (updated category from Moq to Usage)
Moq1500 | Usage | Warning | MockRepository.Verify() should be called (updated category from Moq to Usage)
