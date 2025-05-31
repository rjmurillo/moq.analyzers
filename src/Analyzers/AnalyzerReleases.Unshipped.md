; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 0.4.0

### Changed Rules

Rule ID | New Category | New Severity | Old Category | Old Severity | Notes
--------|--------------|--------------|--------------|--------------|-------
Moq1000 | Usage | Warning | Moq | Warning | NoSealedClassMocksAnalyzer, [Documentation](https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/Moq1000.md)
Moq1001 | Usage | Warning | Moq | Warning | ConstructorArgumentsShouldMatchAnalyzer, [Documentation](https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/Moq1001.md)
Moq1002 | Usage | Warning | Moq | Warning | ConstructorArgumentsShouldMatchAnalyzer, [Documentation](https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/Moq1002.md)
Moq1100 | Usage | Warning | Moq | Warning | CallbackSignatureShouldMatchMockedMethodAnalyzer, [Documentation](https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/Moq1100.md)
Moq1101 | Usage | Warning | Moq | Warning | NoMethodsInPropertySetupAnalyzer, [Documentation](https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/Moq1101.md)
Moq1200 | Usage | Error | Moq | Error | SetupShouldBeUsedOnlyForOverridableMembersAnalyzer, [Documentation](https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/Moq1200.md)
Moq1201 | Usage | Error | Moq | Error | SetupShouldNotIncludeAsyncResultAnalyzer, [Documentation](https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/Moq1201.md)
Moq1300 | Usage | Error | Moq | Error | AsShouldBeUsedOnlyForInterfaceAnalyzer, [Documentation](https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/Moq1300.md)
Moq1400 | Usage | Warning | Moq | Warning | SetExplicitMockBehaviorAnalyzer, [Documentation](https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/Moq1400.md)
Moq1410 | Usage | Info | Moq | Info | SetStrictMockBehaviorAnalyzer, [Documentation](https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/Moq1410.md)
