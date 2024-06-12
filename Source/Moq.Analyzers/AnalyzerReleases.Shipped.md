; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 0.0.1.22865

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
Moq1101 | Moq | Warning | CallbackSignatureAnalyzer
Moq1002 | Moq | Warning | ShouldNotAllowParametersForMockedInterfaceAnalyzer
Moq1001 | Moq | Warning | ShouldNotMockSealedClassesAnalyzer

## Release 0.0.3.40797

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
Moq1003 | Moq | Warning | MatchingConstructorParametersAnalyzer

### Changed Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
Moq1001 | Moq | Warning | NoMocksForSealedClassesAnalyzer
Moq1002 | Moq | Warning | NoParametersForMockedInterfacesAnalyzer
