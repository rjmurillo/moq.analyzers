; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### Changed Rules

Documentation links added for every rule.

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
Moq1000 | Moq | Warning | NoSealedClassMocksAnalyzer, [Documentation](https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/Moq1000.md)
Moq1001 | Moq | Warning | NoConstructorArgumentsForInterfaceMockAnalyzer, [Documentation](https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/Moq1001.md)
Moq1002 | Moq | Warning | ConstructorArgumentsShouldMatchAnalyzer, [Documentation](https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/Moq1002.md)
Moq1100 | Moq | Warning | CallbackSignatureShouldMatchMockedMethodAnalyzer, [Documentation](https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/Moq1100.md)
Moq1101 | Moq | Warning | NoMethodsInPropertySetupAnalyzer, [Documentation](https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/Moq1101.md)
Moq1200 | Moq | Error | SetupShouldBeUsedOnlyForOverridableMembersAnalyzer, [Documentation](https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/Moq1200.md)
Moq1201 | Moq | Error | SetupShouldNotIncludeAsyncResultAnalyzer, [Documentation](https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/Moq1201.md)
Moq1300 | Moq | Error | AsShouldBeUsedOnlyForInterfaceAnalyzer, [Documentation](https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/Moq1300.md)