; Unshipped analyzer release
; <https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md>

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
Moq1003 | Usage | Warning | InternalTypeMustHaveInternalsVisibleToAnalyzer
Moq1004 | Usage | Warning | NoMockOfLoggerAnalyzer
Moq1202 | Usage | Warning | RaiseEventArgumentsShouldMatchEventSignatureAnalyzer
Moq1203 | Usage | Warning | MethodSetupShouldSpecifyReturnValueAnalyzer
Moq1204 | Usage | Warning | RaisesEventArgumentsShouldMatchEventSignatureAnalyzer
Moq1205 | Usage | Warning | EventSetupHandlerShouldMatchEventTypeAnalyzer
Moq1206 | Usage | Warning | ReturnsAsyncShouldBeUsedForAsyncMethodsAnalyzer
Moq1207 | Usage | Error | SetupSequenceShouldBeUsedOnlyForOverridableMembersAnalyzer
Moq1208 | Usage | Warning | ReturnsDelegateShouldReturnTaskAnalyzer
Moq1210 | Usage | Error | VerifyShouldBeUsedOnlyForOverridableMembersAnalyzer
Moq1301 | Usage | Warning | Mock.Get() should not take literals
Moq1302 | Usage | Warning | LINQ to Mocks expression should be valid
Moq1420 | Usage | Info | RedundantTimesSpecificationAnalyzer
Moq1500 | Usage | Warning | MockRepository.Verify() should be called

### Changed Rules

Rule ID | New Category | New Severity | Old Category | Old Severity | Notes
--------|--------------|--------------|--------------|--------------|------
Moq1000 | Usage | Warning | Moq | Warning | NoSealedClassMocksAnalyzer (updated category from Moq to Usage)
Moq1001 | Usage | Warning | Moq | Warning | ConstructorArgumentsShouldMatchAnalyzer (updated category from Moq to Usage)
Moq1002 | Usage | Warning | Moq | Warning | ConstructorArgumentsShouldMatchAnalyzer (updated category from Moq to Usage)
Moq1100 | Usage | Warning | Moq | Warning | CallbackSignatureShouldMatchMockedMethodAnalyzer (updated category from Moq to Usage)
Moq1101 | Usage | Warning | Moq | Warning | NoMethodsInPropertySetupAnalyzer (updated category from Moq to Usage)
Moq1200 | Usage | Error | Moq | Error | SetupShouldBeUsedOnlyForOverridableMembersAnalyzer (updated category from Moq to Usage)
Moq1201 | Usage | Error | Moq | Error | SetupShouldNotIncludeAsyncResultAnalyzer (updated category from Moq to Usage)
Moq1300 | Usage | Error | Moq | Error | AsShouldBeUsedOnlyForInterfaceAnalyzer (updated category from Moq to Usage)
Moq1400 | Usage | Warning | Moq | Warning | SetExplicitMockBehaviorAnalyzer (updated category from Moq to Usage)
Moq1410 | Usage | Info | Moq | Info | SetStrictMockBehaviorAnalyzer (updated category from Moq to Usage)
