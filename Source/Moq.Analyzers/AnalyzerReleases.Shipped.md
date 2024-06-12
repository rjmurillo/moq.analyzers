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

## Release 0.0.4.43043

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
Moq1100 | Moq | Warning | CallbackSignatureShouldMatchMockedMethodAnalyzer
Moq1000 | Moq | Warning | ConstructorArgumentsShouldMatchAnalyzer

## Release 0.0.6

### Removed Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
Moq1003 | Moq | Warning | ConstructorArgumentsShouldMatchAnalyzer

## Release 0.0.7

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
Moq1200 | Moq | Error | SetupShouldBeUsedOnlyForOverridableMembersAnalyzer

## Release 0.0.8

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
Moq1300 | Moq | Error | AsShouldBeUsedOnlyForInterfaceAnalyzer

## Release 0.0.9

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
Moq1201 | Moq | Error | SetupShouldNotIncludeAsyncResultAnalyzer
