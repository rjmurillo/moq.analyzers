# Moq.Analyzers

[![NuGet Version](https://img.shields.io/nuget/v/Moq.Analyzers?style=flat&logo=nuget&color=blue)](https://www.nuget.org/packages/Moq.Analyzers)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Moq.Analyzers?style=flat&logo=nuget)](https://www.nuget.org/packages/Moq.Analyzers)
[![Main build](https://github.com/rjmurillo/moq.analyzers/actions/workflows/main.yml/badge.svg)](https://github.com/rjmurillo/moq.analyzers/actions/workflows/main.yml)
[![Codacy Grade Badge](https://app.codacy.com/project/badge/Grade/fc7c184dcb1843d4b1ae1b926fb82d5a)](https://app.codacy.com/gh/rjmurillo/moq.analyzers/dashboard?utm_source=gh&utm_medium=referral&utm_content=&utm_campaign=Badge_grade)
[![Codacy Coverage Badge](https://app.codacy.com/project/badge/Coverage/fc7c184dcb1843d4b1ae1b926fb82d5a)](https://app.codacy.com/gh/rjmurillo/moq.analyzers/dashboard?utm_source=gh&utm_medium=referral&utm_content=&utm_campaign=Badge_coverage)
[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/rjmurillo/moq.analyzers)

**Moq.Analyzers** is a set of Roslyn analyzers that help you write unit tests with the popular
[Moq](https://github.com/devlooped/moq) framework. The analyzers catch common mistakes and warn you when
something is wrong with your Moq configuration.

## Analyzer rules

| ID                               | Category      | Title                                                                                   |
| -------------------------------- | ------------- | --------------------------------------------------------------------------------------- |
| [Moq1000](docs/rules/Moq1000.md) | Usage         | Sealed classes cannot be mocked                                                         |
| [Moq1001](docs/rules/Moq1001.md) | Usage         | Mocked interfaces cannot have constructor parameters                                    |
| [Moq1002](docs/rules/Moq1002.md) | Usage         | Parameters provided into mock do not match any existing constructors                    |
| [Moq1003](docs/rules/Moq1003.md) | Usage         | Internal type requires InternalsVisibleTo for DynamicProxy                              |
| [Moq1004](docs/rules/Moq1004.md) | Usage         | ILogger should not be mocked                                                            |
| [Moq1100](docs/rules/Moq1100.md) | Correctness   | Callback signature must match the signature of the mocked method                        |
| [Moq1101](docs/rules/Moq1101.md) | Correctness   | SetupGet/SetupSet/SetupProperty should be used for properties, not for methods          |
| [Moq1200](docs/rules/Moq1200.md) | Correctness   | Setup should be used only for overridable members                                       |
| [Moq1201](docs/rules/Moq1201.md) | Correctness   | Setup of async methods should use `.ReturnsAsync` instance instead of `.Result`         |
| [Moq1202](docs/rules/Moq1202.md) | Correctness   | Raise event arguments should match the event delegate signature                         |
| [Moq1203](docs/rules/Moq1203.md) | Correctness   | Method setup should specify a return value                                              |
| [Moq1204](docs/rules/Moq1204.md) | Correctness   | Raises event arguments should match event signature                                     |
| [Moq1205](docs/rules/Moq1205.md) | Correctness   | Event setup handler type should match event delegate type                               |
| [Moq1206](docs/rules/Moq1206.md) | Correctness   | Async method setups should use ReturnsAsync instead of Returns with async lambda        |
| [Moq1207](docs/rules/Moq1207.md) | Correctness   | SetupSequence should be used only for overridable members                               |
| [Moq1208](docs/rules/Moq1208.md) | Correctness   | Returns() delegate type mismatch on async method setup                                  |
| [Moq1210](docs/rules/Moq1210.md) | Correctness   | Verify should be used only for overridable members                                      |
| [Moq1300](docs/rules/Moq1300.md) | Usage         | `Mock.As()` should take interfaces only                                                 |
| [Moq1301](docs/rules/Moq1301.md) | Usage         | Mock.Get() should not take literals                                                     |
| [Moq1302](docs/rules/Moq1302.md) | Usage         | LINQ to Mocks expression should be valid                                                |
| [Moq1400](docs/rules/Moq1400.md) | Best Practice | Explicitly choose a mocking behavior instead of relying on the default (Loose) behavior |
| [Moq1410](docs/rules/Moq1410.md) | Best Practice | Explicitly set the Strict mocking behavior                                              |
| [Moq1420](docs/rules/Moq1420.md) | Usage         | Redundant `Times.AtLeastOnce()` specification can be removed                            |
| [Moq1500](docs/rules/Moq1500.md) | Best Practice | MockRepository.Verify() should be called                                                |
| [Moq1600](docs/rules/Moq1600.md) | Usage         | Protected setup should use `ItExpr` matchers                                            |

For details on each rule, see the [rule reference](docs/rules/README.md).
To find the rule behind a mistake, with examples and fixes, see [Common Moq mistakes](docs/common-moq-mistakes.md).
Rules Moq1200, Moq1207, and Moq1210 treat sealed default interface members as non-overridable because Moq cannot intercept them.

## Getting started

Before you install, make sure you use a
[supported version of the .NET SDK](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core).

To install Moq.Analyzers from NuGet, run this command in each test project:

```shell
dotnet add package Moq.Analyzers
```

After you install the package, the analyzers report diagnostics when you build and in IDEs that support Roslyn
analyzers.

### Configure rules

Moq.Analyzers uses the standard .NET settings to enable, disable, or suppress rules. To configure rules for your
project, see [Suppress code analysis warnings](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/suppress-warnings)
on Microsoft Learn.

## Contributions welcome

Moq.Analyzers keeps growing, and we welcome your help. You can report issues, build new features, or improve the
documentation. To get started, see the [contributing guide](./CONTRIBUTING.md).

- Docs-only pull requests skip build and test. Because no code changed, CI reports the base commit's coverage to
  Codacy for them.
- The list of skippable paths lives in `build/scripts/ci/classify-changed-paths.sh`. Any path not on that list
  runs the full pipeline.
- CI keeps artifacts from the main and mutation-testing workflows for seven days. For details, see the
  [CI workflow requirements](./CONTRIBUTING.md#ci-workflow-requirements).
- To learn what a pull request needs before it can merge, see the [merge requirements](./docs/merge-requirements.md).
