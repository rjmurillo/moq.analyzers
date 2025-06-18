# Diagnostics / rules

| ID                               | Category      | Title                                                                                   |
| -------------------------------- | ------------- | --------------------------------------------------------------------------------------- |
| [Moq1000](./Moq1000.md)          | Usage         | Sealed classes cannot be mocked                                                         |
| [Moq1001](./Moq1001.md)          | Usage         | Mocked interfaces cannot have constructor parameters                                    |
| [Moq1002](./Moq1002.md)          | Usage         | Parameters provided into mock do not match any existing constructors                    |
| [Moq1100](./Moq1100.md)          | Correctness   | Callback signature must match the signature of the mocked method                        |
| [Moq1101](./Moq1101.md)          | Usage         | SetupGet/SetupSet should be used for properties, not for methods                        |
| [Moq1200](./Moq1200.md)          | Correctness   | Setup should be used only for overridable members                                       |
| [Moq1201](./Moq1201.md)          | Correctness   | Setup of async methods should use `.ReturnsAsync` instance instead of `.Result`         |
| [Moq1300](./Moq1300.md)          | Usage         | `Mock.As()` should take interfaces only                                                 |
| [Moq1400](./Moq1400.md)          | Best Practice | Explicitly choose a mocking behavior instead of relying on the default (Loose) behavior |
| [Moq1410](./Moq1410.md)          | Best Practice | Explicitly set the Strict mocking behavior                                              |
| [Moq1500](./Moq1500.md)          | Correctness   | Raise event arguments should match the event delegate signature                         |

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
| Moq1500-1999  | Reserved      | Reserved for future rules                                   |

- When adding new rules, assign the next available ID in the appropriate category range.
- Document new rules in this table, including their category and a concise title.
- For more, see the root [README.md](../../README.md).
