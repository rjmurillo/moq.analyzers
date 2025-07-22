# Diagnostics / rules

| ID                      | Category      | Title                                                                                   | Implementation File                                                                                                                |
| ----------------------- | ------------- | --------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------- |
| [Moq1000](./Moq1000.md) | Usage         | Sealed classes cannot be mocked                                                         | [NoSealedClassMocksAnalyzer.cs](../../src/Analyzers/NoSealedClassMocksAnalyzer.cs)                                                 |
| [Moq1001](./Moq1001.md) | Usage         | Mocked interfaces cannot have constructor parameters                                    | [ConstructorArgumentsShouldMatchAnalyzer.cs](../../src/Analyzers/ConstructorArgumentsShouldMatchAnalyzer.cs)                       |
| [Moq1002](./Moq1002.md) | Usage         | Parameters provided into mock do not match any existing constructors                    | [ConstructorArgumentsShouldMatchAnalyzer.cs](../../src/Analyzers/ConstructorArgumentsShouldMatchAnalyzer.cs)                       |
| [Moq1100](./Moq1100.md) | Correctness   | Callback signature must match the signature of the mocked method                        | [CallbackSignatureShouldMatchMockedMethodAnalyzer.cs](../../src/Analyzers/CallbackSignatureShouldMatchMockedMethodAnalyzer.cs)     |
| [Moq1101](./Moq1101.md) | Usage         | SetupGet/SetupSet/SetupProperty should be used for properties, not for methods          | [NoMethodsInPropertySetupAnalyzer.cs](../../src/Analyzers/NoMethodsInPropertySetupAnalyzer.cs)                                     |
| [Moq1200](./Moq1200.md) | Correctness   | Setup should be used only for overridable members                                       | [SetupShouldBeUsedOnlyForOverridableMembersAnalyzer.cs](../../src/Analyzers/SetupShouldBeUsedOnlyForOverridableMembersAnalyzer.cs) |
| [Moq1201](./Moq1201.md) | Correctness   | Setup of async methods should use `.ReturnsAsync` instance instead of `.Result`         | [SetupShouldNotIncludeAsyncResultAnalyzer.cs](../../src/Analyzers/SetupShouldNotIncludeAsyncResultAnalyzer.cs)                     |
| [Moq1202](./Moq1202.md) | Correctness   | Raise event arguments should match the event delegate signature                         | [RaiseEventArgumentsShouldMatchEventSignatureAnalyzer.cs](../../src/Analyzers/RaiseEventArgumentsShouldMatchEventSignatureAnalyzer.cs)     |
| [Moq1203](./Moq1203.md) | Correctness   | Method setup should specify a return value                                              | [MethodSetupShouldSpecifyReturnValueAnalyzer.cs](../../src/Analyzers/MethodSetupShouldSpecifyReturnValueAnalyzer.cs)                       |
| [Moq1204](./Moq1204.md) | Correctness   | Raises event arguments should match event signature                                     | [RaisesEventArgumentsShouldMatchEventSignatureAnalyzer.cs](../../src/Analyzers/RaisesEventArgumentsShouldMatchEventSignatureAnalyzer.cs) |
| [Moq1205](./Moq1205.md) | Correctness   | Event setup handler type should match event delegate type                               | [EventSetupHandlerShouldMatchEventTypeAnalyzer.cs](../../src/Analyzers/EventSetupHandlerShouldMatchEventTypeAnalyzer.cs)            |
| [Moq1206](./Moq1206.md) | Correctness   | Async method setups should use ReturnsAsync instead of Returns with async lambda        | [ReturnsAsyncShouldBeUsedForAsyncMethodsAnalyzer.cs](../../src/Analyzers/ReturnsAsyncShouldBeUsedForAsyncMethodsAnalyzer.cs)       |
| [Moq1210](./Moq1210.md) | Correctness   | Verify should be used only for overridable members                                      | [VerifyShouldBeUsedOnlyForOverridableMembersAnalyzer.cs](../../src/Analyzers/VerifyShouldBeUsedOnlyForOverridableMembersAnalyzer.cs)       |
| [Moq1300](./Moq1300.md) | Usage         | `Mock.As()` should take interfaces only                                                 | [AsShouldBeUsedOnlyForInterfaceAnalyzer.cs](../../src/Analyzers/AsShouldBeUsedOnlyForInterfaceAnalyzer.cs)                         |
| [Moq1301](./Moq1301.md) | Usage         | Mock.Get() should not take literals                                                     | [MockGetShouldNotTakeLiteralsAnalyzer.cs](../../src/Analyzers/MockGetShouldNotTakeLiteralsAnalyzer.cs)                             |
| [Moq1302](./Moq1302.md) | Usage         | LINQ to Mocks expression should be valid                                               | [LinqToMocksExpressionShouldBeValidAnalyzer.cs](../../src/Analyzers/LinqToMocksExpressionShouldBeValidAnalyzer.cs)                 |
| [Moq1400](./Moq1400.md) | Best Practice | Explicitly choose a mocking behavior instead of relying on the default (Loose) behavior | [SetExplicitMockBehaviorAnalyzer.cs](../../src/Analyzers/SetExplicitMockBehaviorAnalyzer.cs)                                       |
| [Moq1410](./Moq1410.md) | Best Practice | Explicitly set the Strict mocking behavior                                              | [SetStrictMockBehaviorAnalyzer.cs](../../src/Analyzers/SetStrictMockBehaviorAnalyzer.cs)                                           |
| [Moq1420](./Moq1420.md) | Best Practice | Redundant Times.AtLeastOnce() specification can be removed                              | [RedundantTimesSpecificationAnalyzer.cs](../../src/Analyzers/RedundantTimesSpecificationAnalyzer.cs)                               |
| [Moq1500](./Moq1500.md) | Best Practice | MockRepository.Verify() should be called                                                | [MockRepositoryVerifyAnalyzer.cs](../../src/Analyzers/MockRepositoryVerifyAnalyzer.cs)                                             |
## Guidance for Future Rules

### Categories
- **Usage**: Rules that guide correct use of Moq APIs (e.g., not mocking sealed classes, correct use of As<T>, etc.)
- **Correctness**: Rules that prevent bugs or incorrect test logic (e.g., callback signatures, constructor arguments).
- **Best Practice**: Rules that encourage maintainable, robust, or idiomatic Moq usage (e.g., explicit/strict behavior).

### Diagnostic ID Ranges
| Range         | Category      | Description / Example Rules                                 |
|---------------|---------------|-------------------------------------------------------------|
| Moq1000-1099  | Usage         | Prohibits sealed class mocks, restricts As<T> to interfaces |
| Moq1100-1199  | Correctness   | Ensures callback signatures match, setup is valid           |
| Moq1200-1299  | Correctness   | Prevents async result setups, checks constructor args       |
| Moq1300-1399  | Usage         | Restricts use of literals, enforces API usage patterns      |
| Moq1400-1499  | Best Practice | Encourages explicit/strict mock behavior                    |
| Moq1500-1599  | Best Practice | Repository and verification patterns                         |
| Moq1600-1999  | Reserved      | Reserved for future rules                                   |

- When adding new rules, assign the next available ID in the appropriate category range.
- Document new rules in this table, including their category, a concise title, and links to both documentation and implementation.
- For more, see the root [README.md](../../README.md).
