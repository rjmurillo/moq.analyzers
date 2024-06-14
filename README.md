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

* Moq1000: Sealed classes cannot be mocked
* Moq1001: Mocked interfaces cannot have constructor parameters
* Moq1002: Parameters provided into mock do not match any existing constructors
* Moq1100: Callback signature must match the signature of the mocked method
* Moq1101: SetupGet/SetupSet should be used for properties, not for methods
* Moq1200: Setup should be used only for overridable members
* Moq1201: Setup of async methods should use `.ReturnsAsync` instance instead of `.Result`
* Moq1300: Mock.As() should take interfaces

See [docs/rules](./docs/rules/README.md) for full documentation.

## Getting started

Moq.Analyzers is installed from NuGet. Run this command for your test project(s):

```powershell
dotnet add package Moq.Analyzers
```

> NOTE: You must use a [supported version](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) of
> the .NET SDK (i.e. 6.0 or later).

## Contributions welcome

Moq.Analyzers continues to evolve and add new features. Any help will be appreciated. You can report issues,
develop new features, improve the documentation, or do other cool stuff. See [CONTRIBUTING.md](./CONTRIBUTING.md).
