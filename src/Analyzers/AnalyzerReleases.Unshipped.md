; Unshipped analyzer release
; <https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md>

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
Moq1003 | Usage | Warning | InternalTypeMustHaveInternalsVisibleToAnalyzer
Moq1004 | Usage | Warning | NoMockOfLoggerAnalyzer
Moq1208 | Correctness | Warning | ReturnsDelegateShouldReturnTaskAnalyzer

### Changed Rules

Rule ID | New Category | New Severity | Old Category | Old Severity | Notes
--------|--------------|--------------|--------------|--------------|-------
Moq1100 | Correctness | Warning | Usage | Warning | CallbackSignatureShouldMatchMockedMethodAnalyzer
Moq1101 | Correctness | Warning | Usage | Warning | NoMethodsInPropertySetupAnalyzer
Moq1200 | Correctness | Error | Usage | Error | SetupShouldBeUsedOnlyForOverridableMembersAnalyzer
Moq1201 | Correctness | Error | Usage | Error | SetupShouldNotIncludeAsyncResultAnalyzer
Moq1202 | Correctness | Warning | Usage | Warning | RaiseEventArgumentsShouldMatchEventSignatureAnalyzer
Moq1203 | Correctness | Warning | Usage | Warning | MethodSetupShouldSpecifyReturnValueAnalyzer
Moq1204 | Correctness | Warning | Usage | Warning | RaisesEventArgumentsShouldMatchEventSignatureAnalyzer
Moq1205 | Correctness | Warning | Usage | Warning | EventSetupHandlerShouldMatchEventTypeAnalyzer
Moq1206 | Correctness | Warning | Usage | Warning | ReturnsAsyncShouldBeUsedForAsyncMethodsAnalyzer
Moq1207 | Correctness | Error | Usage | Error | SetupSequenceShouldBeUsedOnlyForOverridableMembersAnalyzer
Moq1210 | Correctness | Error | Usage | Error | VerifyShouldBeUsedOnlyForOverridableMembersAnalyzer
Moq1400 | Best Practice | Warning | Usage | Warning | SetExplicitMockBehaviorAnalyzer
Moq1410 | Best Practice | Info | Usage | Info | SetStrictMockBehaviorAnalyzer
Moq1500 | Best Practice | Warning | Usage | Warning | MockRepositoryVerifyAnalyzer
