# Contributing

We welcome contributions. If you want to contribute to existing issues, check the
[help wanted](https://github.com/rjmurillo/moq.analyzers/labels/help%20wanted) or
[good first issue](https://github.com/rjmurillo/moq.analyzers/labels/good%20first%20issue) items in the backlog.

## AllAnalyzersVerifier for Comprehensive Testing

When writing tests that verify code patterns don't trigger unwanted diagnostics from any Moq analyzer, use the `AllAnalyzersVerifier` helper class:

```csharp
await AllAnalyzersVerifier.VerifyAllAnalyzersAsync(sourceCode, referenceAssemblyGroup);
```

This helper automatically discovers and tests against ALL Moq analyzers using reflection, eliminating the need to manually maintain a list of analyzers. The dynamic discovery ensures that new analyzers are automatically included in comprehensive testing without code changes.

**Note:** If you add a new analyzer, the `AllAnalyzersVerifier` will automatically discover and include it in testing. No manual updates are required to maintain comprehensive test coverage.

## CI Performance Benchmarking and Baseline Caching

This repository supports automated performance benchmarking in CI, with baseline result caching and manual override capabilities. Baseline results are cached per OS and SHA, and can be force-refreshed via workflow inputs. For details on usage, manual runs, and force options, see [docs/ci-performance.md](docs/ci-performance.md).

