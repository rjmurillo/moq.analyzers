# Moq.Analyzers

[![NuGet Version](https://img.shields.io/nuget/v/Moq.Analyzers?style=flat&logo=nuget&color=blue)](https://www.nuget.org/packages/Moq.Analyzers)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Moq.Analyzers?style=flat&logo=nuget)](https://www.nuget.org/packages/Moq.Analyzers)
[![Main build](https://github.com/rjmurillo/moq.analyzers/actions/workflows/main.yml/badge.svg)](https://github.com/rjmurillo/moq.analyzers/actions/workflows/main.yml)
[![Codacy Grade Badge](https://app.codacy.com/project/badge/Grade/fc7c184dcb1843d4b1ae1b926fb82d5a)](https://app.codacy.com/gh/rjmurillo/moq.analyzers/dashboard?utm_source=gh&utm_medium=referral&utm_content=&utm_campaign=Badge_grade)
[![Codacy Coverage Badge](https://app.codacy.com/project/badge/Coverage/fc7c184dcb1843d4b1ae1b926fb82d5a)](https://app.codacy.com/gh/rjmurillo/moq.analyzers/dashboard?utm_source=gh&utm_medium=referral&utm_content=&utm_campaign=Badge_coverage)

**Moq.Analyzers** is a Roslyn analyzer that helps you to write unit tests using the popular
[Moq](https://github.com/devlooped/moq) framework. Moq.Analyzers protects you from common mistakes and warns you if
something is wrong with your Moq configuration.

## Analyzer rules

| ID                               | Category      | Title                                                                                   |
| -------------------------------- | ------------- | --------------------------------------------------------------------------------------- |
| [Moq1000](docs/rules/Moq1000.md) | Usage         | Sealed classes cannot be mocked                                                         |
| [Moq1001](docs/rules/Moq1001.md) | Usage         | Mocked interfaces cannot have constructor parameters                                    |
| [Moq1002](docs/rules/Moq1002.md) | Usage         | Parameters provided into mock do not match any existing constructors                    |
| [Moq1100](docs/rules/Moq1100.md) | Correctness   | Callback signature must match the signature of the mocked method                        |
| [Moq1101](docs/rules/Moq1101.md) | Usage         | SetupGet/SetupSet/SetupProperty should be used for properties, not for methods          |
| [Moq1200](docs/rules/Moq1200.md) | Correctness   | Setup should be used only for overridable members                                       |
| [Moq1201](docs/rules/Moq1201.md) | Correctness   | Setup of async methods should use `.ReturnsAsync` instance instead of `.Result`         |
| [Moq1202](docs/rules/Moq1202.md) | Correctness   | Raise event arguments should match the event delegate signature                         |
| [Moq1203](docs/rules/Moq1203.md) | Correctness   | Event setup handler type should match event delegate type                               |
| [Moq1204](docs/rules/Moq1204.md) | Correctness   | Raises event arguments should match event signature                                     |
| [Moq1300](docs/rules/Moq1300.md) | Usage         | `Mock.As()` should take interfaces only                                                 |
| [Moq1301](docs/rules/Moq1301.md) | Usage         | Mock.Get() should not take literals                                                     |
| [Moq1400](docs/rules/Moq1400.md) | Best Practice | Explicitly choose a mocking behavior instead of relying on the default (Loose) behavior |
| [Moq1410](docs/rules/Moq1410.md) | Best Practice | Explicitly set the Strict mocking behavior                                              |

See [docs/rules/README.md](docs/rules/README.md) for full documentation.

## Getting started

Moq.Analyzers is installed from NuGet. Run this command for your test project(s):

```powershell
dotnet add package Moq.Analyzers
```

> NOTE: You must use a [supported version](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) of
> the .NET SDK (i.e. 8.0 or later).

### Configuring rules

Moq.Analyzers follows existing conventions for enabling, disabling, or suppressing rules. See
[Suppress code analysis warnings - .NET | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/suppress-warnings)
for documentation on how to configure rules for your project.

## Contributions welcome

Moq.Analyzers continues to evolve and add new features. Any help will be appreciated. You can report issues,
develop new features, improve the documentation, or do other cool stuff. See [CONTRIBUTING.md](./CONTRIBUTING.md).
