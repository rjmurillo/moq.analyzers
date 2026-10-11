# Install Moq.Analyzers in every test project

Run `dotnet add package Moq.Analyzers` in one test project, or add one `PackageReference` to a `Directory.Build.props`
file to cover every project under a folder. Mark a reference you write by hand with `PrivateAssets="all"`, so the
analyzer does not flow to projects or packages that reference yours.

| You want to cover | Add this | Where |
| ----------------- | -------- | ----- |
| One project | `dotnet add package Moq.Analyzers` | The test project folder |
| Every project under a folder | A `PackageReference` item | `Directory.Build.props` in that folder |
| Only the projects that reference Moq | A conditional `PackageReference` item | `Directory.Build.targets` at the repository root |
| A repository that uses central package management | A `PackageVersion` item plus a `PackageReference` item, or one `GlobalPackageReference` item | `Directory.Packages.props` |

Every snippet on this page was built with .NET SDK 10.0.401 and Moq.Analyzers 0.4.2. The sample solution has one xUnit
test project that references Moq and one class library that does not.

## Contents

- [Prerequisites](#prerequisites)
- [One project](#one-project)
- [Every test project with Directory.Build.props](#every-test-project-with-directorybuildprops)
- [Only test projects](#only-test-projects)
- [Central package management](#central-package-management)
- [Nested Directory.Build.props files](#nested-directorybuildprops-files)
- [Check that it works](#check-that-it-works)
- [Set rule severity once](#set-rule-severity-once)
- [Update the package](#update-the-package)
- [FAQ](#faq)
- [Next steps](#next-steps)

## Prerequisites

- A [supported version of the .NET SDK](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core).
- A project that references [Moq](https://www.nuget.org/packages/Moq). Moq.Analyzers reports nothing in a project
  that does not use Moq.

In the snippets, replace `x.y.z` with the current version on [NuGet](https://www.nuget.org/packages/Moq.Analyzers).

## One project

Run this command in the test project folder:

```shell
dotnet add package Moq.Analyzers
```

The command writes this reference to the project file:

```xml
<PackageReference Include="Moq.Analyzers" Version="0.4.2">
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  <PrivateAssets>all</PrivateAssets>
</PackageReference>
```

The command adds `PrivateAssets` because the package is marked as a
[development dependency](https://learn.microsoft.com/en-us/nuget/reference/nuspec#developmentdependency).

## Every test project with Directory.Build.props

MSBuild imports a `Directory.Build.props` file into every project in the same folder and its subfolders. Put the file
in the folder that holds your test projects, for example `tests/Directory.Build.props`:

```xml
<Project>
  <ItemGroup>
    <PackageReference Include="Moq.Analyzers" Version="x.y.z" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

Keep `PrivateAssets="all"`. Only `dotnet add package` adds it for you. Without it, the sample build showed two
problems:

- A project that references yours also runs the analyzer.
- `dotnet pack` lists Moq.Analyzers as a dependency of your package.

With `PrivateAssets="all"`, both problems went away. For the full list of values, see
[Controlling dependency assets](https://learn.microsoft.com/en-us/nuget/consume-packages/package-references-in-project-files#controlling-dependency-assets)
on Microsoft Learn.

You can also put the file at the repository root to cover every project. In the sample, the class library that does
not reference Moq loaded the analyzer and built with zero warnings.

## Only test projects

Use this approach when test projects and product projects share a folder. Add a `Directory.Build.targets` file at the
repository root. The condition adds the analyzer to each project that references the Moq package:

```xml
<Project>
  <ItemGroup Condition="'@(PackageReference->AnyHaveMetadataValue('Identity', 'Moq'))' == 'true'">
    <PackageReference Include="Moq.Analyzers" Version="x.y.z" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

This condition must go in `Directory.Build.targets`. MSBuild imports `Directory.Build.props` before it reads the
project's own package references, so the same condition in `Directory.Build.props` adds nothing.

The condition checks direct references only. A project that gets Moq through another project needs its own Moq
reference, or a reference to Moq.Analyzers in its project file.

### Do not rely on IsTestProject from the test SDK

A condition on `IsTestProject` looks like the obvious choice:

```xml
<ItemGroup Condition="'$(IsTestProject)' == 'true'">
  <PackageReference Include="Moq.Analyzers" Version="x.y.z" PrivateAssets="all" />
</ItemGroup>
```

In the sample, this condition added nothing, in `Directory.Build.props` and in `Directory.Build.targets`. The
`Microsoft.NET.Test.Sdk` package sets `IsTestProject`, and NuGet restore does not import properties from packages. The
property is empty when restore decides which packages to download.

The condition works when each test project sets the property in its own project file:

```xml
<PropertyGroup>
  <IsTestProject>true</IsTestProject>
</PropertyGroup>
```

With that property in place, the condition worked in both files.

## Central package management

With [central package management](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management),
versions live in `Directory.Packages.props`. You have two options.

### Set the version centrally and reference the package per folder

Add a `PackageVersion` item to `Directory.Packages.props`:

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="Moq.Analyzers" Version="x.y.z" />
  </ItemGroup>
</Project>
```

Then reference the package without a version in `Directory.Build.props`:

```xml
<Project>
  <ItemGroup>
    <PackageReference Include="Moq.Analyzers" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

You need both items:

- A `Version` attribute on the `PackageReference` item fails restore with error `NU1008`.
- A `PackageReference` item with no matching `PackageVersion` item fails restore with error `NU1010`.

`dotnet add package Moq.Analyzers` writes both items for you in a repository that uses central package management.

### Add the package to every project

A `GlobalPackageReference` item adds the package to every project in the repository. Add it to
`Directory.Packages.props`:

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <GlobalPackageReference Include="Moq.Analyzers" Version="x.y.z" />
  </ItemGroup>
</Project>
```

NuGet applies `PrivateAssets="All"` to a global package reference, so you do not set it. You need no
`PackageReference` item and no `PackageVersion` item. Global package references need .NET SDK 7.0.100 or later. See
[Global package references](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management#global-package-references)
on Microsoft Learn.

## Nested Directory.Build.props files

MSBuild stops at the first `Directory.Build.props` file it finds above a project. If your repository root already has
one, a new `tests/Directory.Build.props` hides it from the test projects. In the sample, a `TreatWarningsAsErrors`
setting in the root file stopped applying to the test project.

To keep both files, import the parent file from the new one. This line comes from
[Customize the build by folder](https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-by-directory#use-case-multi-level-merging)
on Microsoft Learn:

```xml
<Project>
  <Import Project="$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '$(MSBuildThisFileDirectory)../'))" Condition="'' != $([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '$(MSBuildThisFileDirectory)../'))" />
  <ItemGroup>
    <PackageReference Include="Moq.Analyzers" Version="x.y.z" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

The `Condition` attribute skips the import when no parent file exists.

## Check that it works

Add a mock with no explicit behavior to a test:

```csharp
var mock = new Mock<ISample>();
```

Run `dotnet build`. The build reports [Moq1400](rules/Moq1400.md) on that line:

```text
tests/Sample.Tests/UnitTest1.cs(15,20): warning Moq1400: Explicitly choose a mocking behavior for Sample.Tests.ISample instead of relying on the default (Loose) behavior (https://github.com/rjmurillo/moq.analyzers/blob/80cffeef62f9c25df474e55811f6d4960d5d3645/docs/rules/Moq1400.md) [tests/Sample.Tests/Sample.Tests.csproj]
```

If you see no warning, check these items:

- The project references Moq.
- The file name is exactly `Directory.Build.props`. Linux file systems are case sensitive.
- The file sits in the project folder or in a folder above it.
- No closer `Directory.Build.props` file hides it. See
  [Nested Directory.Build.props files](#nested-directorybuildprops-files).

## Set rule severity once

Put an `.editorconfig` file at the repository root. It applies to every project below it:

```ini
root = true

[*.cs]
dotnet_diagnostic.Moq1400.severity = error
dotnet_diagnostic.Moq1410.severity = none
```

In the sample, `error` failed the build on the Moq1400 line, and `none` removed the diagnostic.

A [global AnalyzerConfig file](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-files#global-analyzerconfig)
named `.globalconfig` at the repository root also worked in the sample:

```ini
is_global = true

dotnet_diagnostic.Moq1400.severity = error
```

Each [rule page](rules/README.md) lists the rule's default severity and shows how to suppress a single violation.

## Update the package

Change the version in the one file that holds it. Every covered project picks up the new version on the next restore.
Tools such as Dependabot and Renovate can propose the change as a pull request.

## FAQ

### How do I add Moq.Analyzers to all projects in a solution?

Add a `PackageReference` item to a `Directory.Build.props` file in the folder that contains the projects. See
[Every test project with Directory.Build.props](#every-test-project-with-directorybuildprops).

### Do I need PrivateAssets="all"?

Yes, when you write the reference by hand. `dotnet add package` and `GlobalPackageReference` set it for you. Without
it, the analyzer flows to projects that reference yours, and `dotnet pack` lists it as a dependency of your package.

### Will it affect projects that do not use Moq?

No. In the sample, a class library with no Moq reference loaded the analyzer and built with zero warnings. To keep the
package out of those projects, see [Only test projects](#only-test-projects).

### How do I turn off a rule for all projects?

Set `dotnet_diagnostic.MoqXXXX.severity = none` in an `.editorconfig` file at the repository root. Replace `MoqXXXX`
with the rule ID.

### Does it work with central package management?

Yes. Add a `PackageVersion` item and reference the package without a version, or add one `GlobalPackageReference`
item. See [Central package management](#central-package-management).

## Next steps

- [Common Moq mistakes](common-moq-mistakes.md) maps each mistake to the rule that reports it.
- The [rule reference](rules/README.md) lists every rule.
- The [README](../README.md) covers the project and how to contribute.
