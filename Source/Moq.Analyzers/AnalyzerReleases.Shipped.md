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

## Release 0.0.4.43043

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
Moq1100 | Moq | Warning | CallbackSignatureShouldMatchMockedMethodAnalyzer
Moq1000 | Moq | Warning | ConstructorArgumentsShouldMatchAnalyzer

### Changed Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
Moq1001 | Moq | Warning | NoConstructorArgumentsForInterfaceMockAnalyzer
Moq1101 | Moq | Warning | NoMethodsInPropertySetupAnalyzer
Moq1000 | Moq | Warning | NoSealedClassMocksAnalyzer

## Release 0.0.6

### Changed Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
Moq1002 | Moq | Warning | ConstructorArgumentsShouldMatchAnalyzer
