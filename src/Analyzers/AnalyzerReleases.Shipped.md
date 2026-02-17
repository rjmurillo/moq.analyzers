; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 0.0.1.22865

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
Moq1101 | Moq | Warning | CallbackSignatureAnalyzer, [Documentation](https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/Moq1101.md)
Moq1002 | Moq | Warning | ShouldNotAllowParametersForMockedInterfaceAnalyzer, [Documentation](https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/Moq1002.md)
Moq1001 | Moq | Warning | ShouldNotMockSealedClassesAnalyzer, [Documentation](https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/Moq1001.md)

## Release 0.0.3.40797

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
Moq1003 | Moq | Warning | MatchingConstructorParametersAnalyzer

## Release 0.0.4.43043

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
Moq1100 | Moq | Warning | CallbackSignatureShouldMatchMockedMethodAnalyzer, [Documentation](https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/Moq1100.md)
Moq1000 | Moq | Warning | ConstructorArgumentsShouldMatchAnalyzer, [Documentation](https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/Moq1000.md)

## Release 0.0.6

### Removed Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
Moq1003 | Moq | Warning | ConstructorArgumentsShouldMatchAnalyzer, [Documentation](https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/Moq1003.md)

## Release 0.0.7

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
Moq1200 | Moq | Error | SetupShouldBeUsedOnlyForOverridableMembersAnalyzer, [Documentation](https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/Moq1200.md)

## Release 0.0.8

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
Moq1300 | Moq | Error | AsShouldBeUsedOnlyForInterfaceAnalyzer, [Documentation](https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/Moq1300.md)

## Release 0.0.9

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
Moq1201 | Moq | Error | SetupShouldNotIncludeAsyncResultAnalyzer, [Documentation](https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/Moq1201.md)

## Release 0.3.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
Moq1400 | Moq      | Warning  | SetExplicitMockBehaviorAnalyzer, [Documentation](https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/Moq1400.md)
Moq1410 | Moq      | Info     | SetStrictMockBehaviorAnalyzer, [Documentation](https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/Moq1410.md)

## Release 0.4.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
Moq1202 | Usage | Warning | RaiseEventArgumentsShouldMatchEventSignatureAnalyzer, [Documentation](https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/Moq1202.md)
Moq1203 | Usage | Warning | MethodSetupShouldSpecifyReturnValueAnalyzer, [Documentation](https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/Moq1203.md)
Moq1204 | Usage | Warning | RaisesEventArgumentsShouldMatchEventSignatureAnalyzer, [Documentation](https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/Moq1204.md)
Moq1205 | Usage | Warning | EventSetupHandlerShouldMatchEventTypeAnalyzer, [Documentation](https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/Moq1205.md)
Moq1206 | Usage | Warning | ReturnsAsyncShouldBeUsedForAsyncMethodsAnalyzer, [Documentation](https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/Moq1206.md)
Moq1207 | Usage | Error | SetupSequenceShouldBeUsedOnlyForOverridableMembersAnalyzer, [Documentation](https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/Moq1207.md)
Moq1210 | Usage | Error | VerifyShouldBeUsedOnlyForOverridableMembersAnalyzer, [Documentation](https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/Moq1210.md)
Moq1301 | Usage | Warning | Mock.Get() should not take literals, [Documentation](https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/Moq1301.md)
Moq1302 | Usage | Warning | LINQ to Mocks expression should be valid, [Documentation](https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/Moq1302.md)
Moq1420 | Usage | Info | RedundantTimesSpecificationAnalyzer, [Documentation](https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/Moq1420.md)
Moq1500 | Usage | Warning | MockRepository.Verify() should be called, [Documentation](https://github.com/rjmurillo/moq.analyzers/blob/main/docs/rules/Moq1500.md)

### Changed Rules

Rule ID | New Category | New Severity | Old Category | Old Severity | Notes
--------|--------------|--------------|--------------|--------------|-------
Moq1000 | Usage | Warning | Moq | Warning | NoSealedClassMocksAnalyzer
Moq1001 | Usage | Warning | Moq | Warning | NoConstructorArgumentsForInterfaceMockRuleId
Moq1002 | Usage | Warning | Moq | Warning | NoMatchingConstructorRuleId
Moq1100 | Usage | Warning | Moq | Warning | CallbackSignatureShouldMatchMockedMethodAnalyzer
Moq1101 | Usage | Warning | Moq | Warning | NoMethodsInPropertySetupAnalyzer
Moq1200 | Usage | Error | Moq | Error | SetupShouldBeUsedOnlyForOverridableMembersAnalyzer
Moq1201 | Usage | Error | Moq | Error | SetupShouldNotIncludeAsyncResultAnalyzer
Moq1300 | Usage | Error | Moq | Error | AsShouldBeUsedOnlyForInterfaceAnalyzer
Moq1400 | Usage | Warning | Moq | Warning | SetExplicitMockBehaviorAnalyzer
Moq1410 | Usage | Info | Moq | Info | SetStrictMockBehaviorAnalyzer
