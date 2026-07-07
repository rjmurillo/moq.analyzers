# Diagnostic Coverage Testing

Analyzer tests must cover more than the diagnostic location.

Every rule needs descriptor metadata coverage for:

- Id
- Title
- Message format
- Description
- Category
- Default severity
- Default enabled state
- Help link
- Rule documentation

Rules with message format arguments also need one positive test that verifies
the formatted diagnostic message.

Use numbered markup with `DiagnosticResult.WithLocation(0).WithMessage(...)`
when a test needs both a precise span and an exact message:

```csharp
DiagnosticResult expected = new DiagnosticResult("Moq1000", DiagnosticSeverity.Warning)
    .WithLocation(0)
    .WithMessage("Sealed class 'FooSealed' cannot be mocked");

await Verifier.VerifyAnalyzerAsync(
    """
    internal sealed class FooSealed { }

    internal class UnitTest
    {
        private void Test()
        {
            new Mock<{|#0:FooSealed|}>();
        }
    }
    """,
    ReferenceAssemblyCatalog.Net80WithNewMoq,
    expected);
```
