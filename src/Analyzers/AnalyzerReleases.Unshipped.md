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
Moq1200 | Usage | Error | Moq | Error | SetupShouldBeUsedOnlyForOverridableMembersAnalyzer, [Documentation](https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/Moq1200.md)
Moq1300 | Usage | Error | Moq | Error | AsShouldBeUsedOnlyForInterfaceAnalyzer, [Documentation](https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/Moq1300.md)
