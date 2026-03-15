# Complete Analyzer and Code Fix Catalog

## 24 Diagnostic Rules

### Usage Category

| ID | Analyzer | Description |
|---|---|---|
| Moq1000 | NoSealedClassMocksAnalyzer | Sealed classes cannot be mocked |
| Moq1001 | ConstructorArgumentsShouldMatchAnalyzer | Mocked interfaces cannot have constructor parameters |
| Moq1002 | ConstructorArgumentsShouldMatchAnalyzer | Constructor parameters must match an existing constructor |
| Moq1003 | InternalTypeMustHaveInternalsVisibleToAnalyzer | Internal type requires InternalsVisibleTo for DynamicProxy |
| Moq1004 | NoMockOfLoggerAnalyzer | ILogger should not be mocked |
| Moq1300 | AsShouldBeUsedOnlyForInterfaceAnalyzer | Mock.As() should take interfaces only |
| Moq1301 | MockGetShouldNotTakeLiteralsAnalyzer | Mock.Get() should not take literals |
| Moq1302 | LinqToMocksExpressionShouldBeValidAnalyzer | LINQ to Mocks expression should be valid |
| Moq1420 | RedundantTimesSpecificationAnalyzer | Redundant Times.AtLeastOnce() can be removed |

### Correctness Category

| ID | Analyzer | Description |
|---|---|---|
| Moq1100 | CallbackSignatureShouldMatchMockedMethodAnalyzer | Callback signature must match mocked method |
| Moq1101 | NoMethodsInPropertySetupAnalyzer | SetupGet/SetupSet/SetupProperty for properties, not methods |
| Moq1200 | SetupShouldBeUsedOnlyForOverridableMembersAnalyzer | Setup should target overridable members only |
| Moq1201 | SetupShouldNotIncludeAsyncResultAnalyzer | Async methods should use .ReturnsAsync instead of .Result |
| Moq1202 | RaiseEventArgumentsShouldMatchEventSignatureAnalyzer | Raise event args must match event delegate signature |
| Moq1203 | MethodSetupShouldSpecifyReturnValueAnalyzer | Method setup should specify a return value |
| Moq1204 | RaisesEventArgumentsShouldMatchEventSignatureAnalyzer | Raises event args must match event delegate signature |
| Moq1205 | EventSetupHandlerShouldMatchEventTypeAnalyzer | Event setup handler type should match event delegate type |
| Moq1206 | ReturnsAsyncShouldBeUsedForAsyncMethodsAnalyzer | Async setups should use ReturnsAsync, not Returns with async lambda |
| Moq1207 | SetupSequenceShouldBeUsedOnlyForOverridableMembersAnalyzer | SetupSequence should target overridable members only |
| Moq1208 | ReturnsDelegateShouldReturnTaskAnalyzer | Returns() delegate type mismatch on async method setup |
| Moq1210 | VerifyShouldBeUsedOnlyForOverridableMembersAnalyzer | Verify should target overridable members only |

Note: Moq1209 is intentionally reserved (undocumented reason).

### Best Practice Category

| ID | Analyzer | Description |
|---|---|---|
| Moq1400 | SetExplicitMockBehaviorAnalyzer | Explicitly choose a mocking behavior |
| Moq1410 | SetStrictMockBehaviorAnalyzer | Explicitly set Strict mocking behavior |
| Moq1500 | MockRepositoryVerifyAnalyzer | MockRepository.Verify() should be called |

## 5 Code Fix Providers

| Fixer | Fixes | Action |
|---|---|---|
| CallbackSignatureShouldMatchMockedMethodFixer | Moq1100 | Corrects callback signature |
| SetExplicitMockBehaviorFixer | Moq1400 | Inserts/replaces MockBehavior parameter |
| SetStrictMockBehaviorFixer | Moq1410 | Inserts/replaces MockBehavior.Strict |
| ReturnsDelegateShouldReturnTaskFixer | Moq1208 | Replaces Returns() with ReturnsAsync() |
| VerifyOverridableMembersFixer | Moq1210 | Fixes verify calls on non-overridable members |

## Registration Pattern Distribution

- RegisterOperationAction (preferred, ADR-007): 17 analyzers
- RegisterSyntaxNodeAction (legacy, needs justification): 7 analyzers
- Template Method (MockBehaviorDiagnosticAnalyzerBase): Moq1400, Moq1410

## Analyzers Without Code Fixes (gaps)

Moq1000, Moq1001, Moq1002, Moq1003, Moq1004, Moq1101, Moq1200, Moq1201, Moq1202, Moq1203,
Moq1204, Moq1205, Moq1206, Moq1207, Moq1300, Moq1301, Moq1302, Moq1420, Moq1500
