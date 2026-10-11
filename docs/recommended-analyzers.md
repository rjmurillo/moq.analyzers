# Analyzers that pair well with Moq.Analyzers

This page covers Roslyn analyzers for C# test projects. Start with the analyzers in the .NET SDK. Then add the analyzer
for your test framework and the analyzer for your mocking library. If your tests use
[Moq](https://github.com/devlooped/moq), that mocking analyzer is
[Moq.Analyzers](https://www.nuget.org/packages/Moq.Analyzers).

Moq.Analyzers is not an analyzer for every project. It reports only on code that calls Moq. Install it in the test
projects that reference Moq and skip it everywhere else.

The Moq.Analyzers maintainers wrote this page. The package tables state facts from NuGet and from each package's own
documentation. They do not rank the packages.

## Contents

- [Packages at a glance](#packages-at-a-glance)
- [Start with the .NET SDK analyzers](#start-with-the-net-sdk-analyzers)
- [Add the analyzer for your test framework](#add-the-analyzer-for-your-test-framework)
- [Add the analyzer for your mocking library](#add-the-analyzer-for-your-mocking-library)
- [General analyzers this repository runs on its tests](#general-analyzers-this-repository-runs-on-its-tests)
- [Reference both analyzers from one props file](#reference-both-analyzers-from-one-props-file)
- [Roll out new rules in stages](#roll-out-new-rules-in-stages)
- [FAQ](#faq)
- [Sources](#sources)

## Packages at a glance

Versions and download totals come from the NuGet search API, read on October 11, 2026 (UTC). The version is the latest
stable release on that date.

| Package | Use it when your test project | Do you add a package reference? | Version | Total downloads |
| ------- | ----------------------------- | ------------------------------- | ------- | --------------- |
| [Microsoft.CodeAnalysis.NetAnalyzers](https://www.nuget.org/packages/Microsoft.CodeAnalysis.NetAnalyzers) | Uses an SDK-style project | No. The .NET SDK includes it. On by default for .NET 5 or later | 10.0.401 | 90,262,497 |
| [xunit.analyzers](https://www.nuget.org/packages/xunit.analyzers) | Uses xUnit.net | No. `xunit` depends on it. `xunit.v3` brings it in through `xunit.v3.mtp-v2` | 2.2.0 | 1,059,435,869 |
| [NUnit.Analyzers](https://www.nuget.org/packages/NUnit.Analyzers) | Uses NUnit | Yes, unless your project file already has it | 4.15.0 | 169,936,438 |
| [MSTest.Analyzers](https://www.nuget.org/packages/MSTest.Analyzers) | Uses MSTest | No. `MSTest.TestFramework` 3.7 and later depends on it | 4.5.1 | 74,123,972 |
| [Moq.Analyzers](https://www.nuget.org/packages/Moq.Analyzers) | Uses Moq | Yes | 0.4.2 | 3,834,656 |
| [NSubstitute.Analyzers.CSharp](https://www.nuget.org/packages/NSubstitute.Analyzers.CSharp) | Uses NSubstitute | Yes | 1.0.17 | 36,355,189 |

For Microsoft.CodeAnalysis.NetAnalyzers, the version is the NuGet package. The copy in the SDK matches your SDK.

Download totals are not a quality ranking. NuGet downloads a dependency along with its parent. `xunit` depends on
`xunit.analyzers`, and their totals differ by less than 1%: 1,063,596,523 and 1,059,435,869.

## Start with the .NET SDK analyzers

The .NET SDK ships the code quality analyzers (rule IDs that start with `CA`). You do not install them. They run by
default in projects that target .NET 5 or later. For details, see
[Overview of .NET source code analysis](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/overview)
on Microsoft Learn.

The default mode enables a subset of the rules. To enable more, set `AnalysisMode` in the project file or in a
`Directory.Build.props` file:

```xml
<PropertyGroup>
  <AnalysisMode>Recommended</AnalysisMode>
</PropertyGroup>
```

Microsoft Learn lists five values: `None`, `Default`, `Minimum`, `Recommended`, and `All`.

A project that targets .NET Framework or .NET Standard does not run these analyzers by default. Set
`EnableNETAnalyzers` to `true` to turn them on.

## Add the analyzer for your test framework

Each framework ships its analyzer a different way. The dependency lists below come from the NuGet registration API for
the version named.

| Framework | Analyzer package | How it reaches your project |
| --------- | ---------------- | --------------------------- |
| xUnit.net v2 | `xunit.analyzers` | `xunit` 2.9.3 depends on `xunit.analyzers` 1.18.0 or later |
| xUnit.net v3 | `xunit.analyzers` | `xunit.v3` 4.0.2 depends on it through `xunit.v3.mtp-v2`, at 2.2.0 or later |
| NUnit | `NUnit.Analyzers` | `NUnit` 5.0.0 does not depend on it. The `dotnet new nunit` template adds the reference |
| MSTest | `MSTest.Analyzers` | `MSTest.TestFramework` 4.5.1 depends on it. The `MSTest` package depends on `MSTest.TestFramework` |

Rule lists:

- [xUnit.net analyzer rules](https://xunit.net/xunit.analyzers/rules/)
- [NUnit.Analyzers documentation](https://github.com/nunit/nunit.analyzers/tree/master/documentation)
- [MSTest code analysis](https://learn.microsoft.com/en-us/dotnet/core/testing/mstest-analyzers/overview) on Microsoft
  Learn

A project that you created with `dotnet new nunit` already references `NUnit.Analyzers`. Check your project file
before you add the package.

### xUnit.net v2 restores an older analyzer

NuGet restores the lowest version that satisfies a dependency range. A project that references only `xunit` 2.9.3
restores `xunit.analyzers` 1.18.0, not 2.2.0. To use a newer analyzer, reference it yourself:

```xml
<ItemGroup>
  <PackageReference Include="xunit.analyzers" Version="x.y.z" PrivateAssets="all" />
</ItemGroup>
```

Replace `x.y.z` with the version you want from [NuGet](https://www.nuget.org/packages/xunit.analyzers).

## Add the analyzer for your mocking library

A mocking library builds its setups from lambdas and expression trees. A mistake in a setup compiles. It fails only
when the test runs. A mocking analyzer reports the mistake when you build.

| Mocking library | Analyzer package | Does the library depend on it? |
| --------------- | ---------------- | ------------------------------ |
| Moq | `Moq.Analyzers` | No. `Moq` 4.21.0 does not depend on it. Add the package yourself |
| NSubstitute | `NSubstitute.Analyzers.CSharp` | No. `NSubstitute` 6.2.0 does not depend on it. Add the package yourself |

Use the analyzer that matches the library your tests call. An analyzer for a library you do not use reports nothing.

Moq.Analyzers has rules for setups, callbacks, events, and mock construction. Three examples:

- [Moq1200](rules/Moq1200.md) reports a `Setup` on a member Moq cannot override.
- [Moq1000](rules/Moq1000.md) reports a mock of a sealed class.
- [Moq1100](rules/Moq1100.md) reports a `Callback` whose parameters do not match the method.

For the full list, see the [rule reference](rules/README.md). To see every rule with an example and a fix, see
[Common Moq mistakes](common-moq-mistakes.md).

For NSubstitute rules, see the
[NSubstitute.Analyzers rule pages](https://github.com/nsubstitute/NSubstitute.Analyzers/tree/master/documentation/rules).

## General analyzers this repository runs on its tests

General code quality analyzers also run in test projects. This repository references the packages below from
[`build/targets/codeanalysis/CodeAnalysis.props`](../build/targets/codeanalysis/CodeAnalysis.props). The root
`Directory.Build.props` imports that file for every project. The list is not specific to tests.

- [Meziantou.Analyzer](https://www.nuget.org/packages/Meziantou.Analyzer)
- [Roslynator.Analyzers](https://www.nuget.org/packages/Roslynator.Analyzers)
- [StyleCop.Analyzers](https://www.nuget.org/packages/StyleCop.Analyzers)
- [SonarAnalyzer.CSharp](https://www.nuget.org/packages/SonarAnalyzer.CSharp)
- [Microsoft.VisualStudio.Threading.Analyzers](https://www.nuget.org/packages/Microsoft.VisualStudio.Threading.Analyzers)
- [Microsoft.CodeAnalysis.BannedApiAnalyzers](https://www.nuget.org/packages/Microsoft.CodeAnalysis.BannedApiAnalyzers)
- [Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers](https://www.nuget.org/packages/Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers)
- [ExhaustiveMatching.Analyzer](https://www.nuget.org/packages/ExhaustiveMatching.Analyzer)
- [EffectiveCSharp.Analyzers](https://www.nuget.org/packages/EffectiveCSharp.Analyzers)
- [Microsoft.CodeAnalysis.Analyzers](https://www.nuget.org/packages/Microsoft.CodeAnalysis.Analyzers), which checks
  code that implements Roslyn analyzers

The list shows what one repository uses. It is not a required set. Richard Murillo, a Moq.Analyzers maintainer, also
maintains EffectiveCSharp.Analyzers.

The test projects in this repository use xUnit.net v2, so they also get `xunit.analyzers`.

## Reference both analyzers from one props file

Create a `Directory.Build.props` file in the folder that holds your test projects. Put the references you add
yourself there. This example covers a folder of NUnit test projects that use Moq:

```xml
<Project>
  <ItemGroup>
    <PackageReference Include="Moq.Analyzers" Version="x.y.z" PrivateAssets="all" />
    <PackageReference Include="NUnit.Analyzers" Version="x.y.z" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

Replace each `x.y.z` with the current version on NuGet. With xUnit.net or MSTest, drop the `NUnit.Analyzers` line. With
NSubstitute, replace `Moq.Analyzers` with `NSubstitute.Analyzers.CSharp`.

If a project file already references one of these packages, remove that reference. NuGet reports `NU1504` for a
duplicate.

For `PrivateAssets`, central package management, nested `Directory.Build.props` files, and how to check the install,
see [Install Moq.Analyzers in every test project](install.md).

## Roll out new rules in stages

A new analyzer reports every existing violation on its first build. Set severity per rule in `.editorconfig`, then
raise it as you fix the code:

```ini
[*.cs]
# Lower from warning. Show in the IDE only. A command-line build does not report it.
dotnet_diagnostic.Moq1400.severity = suggestion

# Raise from warning. Fail the build.
dotnet_diagnostic.Moq1100.severity = error

# Turn a rule off.
dotnet_diagnostic.xUnit2000.severity = none
```

An `.editorconfig` file at the repository root applies to every project below it.

To see what each analyzer costs, build with `ReportAnalyzer` and detailed verbosity:

```shell
dotnet build --no-incremental -p:ReportAnalyzer=true -v:detailed
```

The log then has a `Total analyzer execution time` line. Below it, the log lists the time for each analyzer assembly
and each analyzer class, with its rule IDs. See [ReportAnalyzer](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/errors-warnings#reportanalyzer)
on Microsoft Learn.

## FAQ

### Which Roslyn analyzers should a C# test project use?

Use three layers. Keep the .NET SDK analyzers on. Add the analyzer for your test framework: `xunit.analyzers`,
`NUnit.Analyzers`, or `MSTest.Analyzers`. Add the analyzer for your mocking library: `Moq.Analyzers` for Moq, or
`NSubstitute.Analyzers.CSharp` for NSubstitute.

### Does every C# project need Moq.Analyzers?

No. Moq.Analyzers reports only on code that calls Moq. Install it in the test projects that reference Moq.

### Do I need to install Microsoft.CodeAnalysis.NetAnalyzers?

No, not when your project targets .NET 5 or later. The .NET SDK includes the analyzers and runs them by default.
Install the NuGet package only to update the rules on a different schedule from the SDK. Microsoft Learn recommends
the SDK copy where you can use it.

### Does xUnit.net need a separate analyzer package?

No. `xunit` depends on `xunit.analyzers`. `xunit.v3` brings it in through `xunit.v3.mtp-v2`. Reference
`xunit.analyzers` yourself only to get a newer version than the one your xUnit.net package restores.

### Does NUnit include its analyzers?

No. `NUnit` 5.0.0 has no dependency on `NUnit.Analyzers`. The `dotnet new nunit` template adds the reference for you.
If your project file does not have it, add the `NUnit.Analyzers` package.

### Can I use Moq.Analyzers with NUnit or MSTest?

Yes. Moq.Analyzers checks Moq calls. It does not depend on a test framework.

### Do analyzers slow the build?

Each analyzer adds work to the compiler. Measure it with `ReportAnalyzer`, as shown in
[Roll out new rules in stages](#roll-out-new-rules-in-stages).

## Sources

- NuGet search API and registration API, read October 11, 2026 (UTC): versions, download totals, and dependency
  lists.
- [Overview of .NET source code analysis](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/overview),
  Microsoft Learn: the SDK analyzers, `AnalysisMode`, and `EnableNETAnalyzers`.
- [Enable or install first-party .NET analyzers](https://learn.microsoft.com/en-us/visualstudio/code-quality/install-net-analyzers),
  Microsoft Learn: SDK copy or NuGet package.
- [MSTest code analysis](https://learn.microsoft.com/en-us/dotnet/core/testing/mstest-analyzers/overview), Microsoft
  Learn: `MSTest.Analyzers` is a dependency of `MSTest.TestFramework` from version 3.7.
- [NuGet dependency resolution](https://learn.microsoft.com/en-us/nuget/concepts/dependency-resolution), Microsoft
  Learn: the lowest applicable version rule.
- [`build/targets/codeanalysis/CodeAnalysis.props`](../build/targets/codeanalysis/CodeAnalysis.props): the general
  analyzers this repository references.

For a wider list that is not limited to test projects, see
[The Roslyn analyzers I use in my projects](https://www.meziantou.net/the-roslyn-analyzers-i-use.htm) by Gérald Barré
(May 31, 2021).
