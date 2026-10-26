<#
.SYNOPSIS
    Runs the BUILT Moq.Analyzers DLLs against an arbitrary C# snippet.
.DESCRIPTION
    Usage:
      pwsh Invoke-AnalyzerOnSnippet.ps1 <snippet.cs> [moq-version]

    What it does:
      1. Locates the repo root from this script's location (works from any cwd).
      2. Verifies the analyzer DLLs exist (tells you to `dotnet build` if not).
      3. Generates a throwaway class-library project OUTSIDE the repo tree, so the
         repo's Directory.Build.props, central package management, and PedanticMode
         do not interfere. The project has:
           - a REAL Moq PackageReference, so IsMockReferenced() early exits do not
             silently disable every analyzer, and
           - the same three DLLs the shipped nupkg puts in analyzers/dotnet/cs
             (Moq.Analyzers.dll, Moq.CodeFixes.dll,
             Microsoft.CodeAnalysis.AnalyzerUtilities.dll) as <Analyzer> items.
      4. Writes an .editorconfig that forces every ID in src/Common/DiagnosticIds.cs
         to `warning`, so Info-severity rules (for example Moq1400) show up.
      5. Builds and prints every MoqXXXX diagnostic. It also prints compiler (CSxxxx)
         errors, analyzer crashes (AD0001), and analyzer LOAD failures (CS8032 or
         CS8034). Those three exit the build 0 as warnings, so without this check a
         broken run would look like a clean snippet.

    Environment overrides:
      MOQ_ANALYZERS_CONFIG   debug (default) or release. Picks artifacts/bin/Moq.Analyzers/<config>/.
      SNIPPET_TFM            Target framework of the harness project. Default: net10.0.
      SNIPPET_LANGVERSION    C# language version for the snippet. Default: latest (the host SDK's
                             newest stable C#). Set an older version (for example 12) to reproduce
                             a language-version bug; use preview for preview-only syntax.
      SNIPPET_OUTPUTTYPE     MSBuild OutputType of the harness project. Default: unset (a class
                             library), right for type and member declarations. Set Exe for a
                             snippet written as top-level statements, or the SDK rejects it with
                             CS8805. Do not set Exe for a declaration-only snippet: with no Main
                             it fails CS5001.
      KEEP_HARNESS=1         Keep the temp harness project directory and print its path.

    Exit codes:
      0 = harness ran (with or without Moq diagnostics)
      1 = usage or setup error
      2 = snippet failed to compile (CS errors printed)
      3 = an analyzer crashed (AD0001)
      4 = an analyzer or a dependency failed to LOAD (CS8032/CS8034/CS9057). The analyzers never
          ran, so "(none)" would be a false clean result (the #850 CS8032 incident class).
.PARAMETER Snippet
    Path to a self-contained C# file. It must include its own `using Moq;` and so on.
.PARAMETER MoqVersion
    Moq NuGet version. Default: 4.18.4 (the "new Moq" the test suite pins).
    Use 4.8.2 to reproduce the "old Moq" axis of the test matrix.
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string]$Snippet,

    [Parameter(Position = 1)]
    [string]$MoqVersion = '4.18.4'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false

if ([string]::IsNullOrEmpty($Snippet)) {
    Get-Help $PSCommandPath -Detailed | Out-String | Write-Output
    exit 1
}

function Get-EnvOrDefault([string]$Name, [string]$Default) {
    $value = [Environment]::GetEnvironmentVariable($Name)
    if ([string]::IsNullOrEmpty($value)) { return $Default }
    return $value
}

$config = Get-EnvOrDefault 'MOQ_ANALYZERS_CONFIG' 'debug'
$tfm = Get-EnvOrDefault 'SNIPPET_TFM' 'net10.0'
$langVersion = Get-EnvOrDefault 'SNIPPET_LANGVERSION' 'latest'
# Empty default = SDK default (a class library).
$outputType = Get-EnvOrDefault 'SNIPPET_OUTPUTTYPE' ''

# These values go verbatim into the generated Snippet.csproj XML. Reject XML-significant
# characters so a stray or hostile value cannot break the project file or inject MSBuild
# elements (for example an <Exec> target that dotnet build would run). This is a denylist,
# not an allowlist, so NuGet version ranges such as [4.8.2,4.9.0) still pass.
# MOQ_ANALYZERS_CONFIG is checked too, because it becomes part of the <Analyzer> paths.
$xmlValues = [ordered]@{
    CONFIG      = $config
    TFM         = $tfm
    LANGVERSION = $langVersion
    MOQ_VERSION = $MoqVersion
    OUTPUTTYPE  = $outputType
}
foreach ($entry in $xmlValues.GetEnumerator()) {
    # Also reject MSBuild property, item, and metadata expansion: $( @( %(
    if ($entry.Value -match "[<>&`"']" -or $entry.Value -match '[$@%]\(') {
        Write-Error "$($entry.Key) contains XML-unsafe characters: $($entry.Value)" -ErrorAction Continue
        exit 1
    }
}

# OUTPUTTYPE must be empty or a real MSBuild OutputType. Fail fast on a typo like "Eex".
if ($outputType -notin @('', 'Library', 'Exe', 'WinExe', 'Module')) {
    Write-Error "SNIPPET_OUTPUTTYPE must be one of Library, Exe, WinExe, Module (got: $outputType)" -ErrorAction Continue
    exit 1
}

if (-not (Test-Path -LiteralPath $Snippet -PathType Leaf)) {
    Write-Error "snippet file not found: $Snippet" -ErrorAction Continue
    exit 1
}
$snippetPath = (Resolve-Path -LiteralPath $Snippet).Path

$repoRoot = git -C $PSScriptRoot rev-parse --show-toplevel
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrEmpty($repoRoot)) {
    Write-Error "Unable to determine the repository root from $PSScriptRoot" -ErrorAction Continue
    exit 1
}

$analyzerDir = Join-Path $repoRoot "artifacts/bin/Moq.Analyzers/$config"
foreach ($dll in @('Moq.Analyzers.dll', 'Moq.CodeFixes.dll', 'Microsoft.CodeAnalysis.AnalyzerUtilities.dll')) {
    $dllPath = Join-Path $analyzerDir $dll
    if (-not (Test-Path -LiteralPath $dllPath -PathType Leaf)) {
        [Console]::Error.WriteLine("error: $dllPath not found.")
        [Console]::Error.WriteLine("Build first: cd $repoRoot && dotnet build src/Analyzers/Moq.Analyzers.csproj")
        [Console]::Error.WriteLine('(or set MOQ_ANALYZERS_CONFIG=release if you built with -c Release)')
        exit 1
    }
}

# The harness lives OUTSIDE the repo, so the repo's Directory.Build.props, central
# package management, and analyzer settings do not apply to the snippet.
$work = Join-Path ([System.IO.Path]::GetTempPath()) ("moq-analyzer-snippet." + [System.IO.Path]::GetRandomFileName())
New-Item -ItemType Directory -Path $work | Out-Null
$keepHarness = (Get-EnvOrDefault 'KEEP_HARNESS' '0') -eq '1'
if ($keepHarness) {
    [Console]::Error.WriteLine("harness project: $work")
}

try {
    Copy-Item -LiteralPath $snippetPath -Destination (Join-Path $work 'Snippet.cs')

    # Only emit <OutputType> when the caller set one; otherwise inherit the SDK default.
    $outputTypeElement = ''
    if ($outputType -ne '') {
        $outputTypeElement = "    <OutputType>$outputType</OutputType>"
    }

    @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>$tfm</TargetFramework>
$outputTypeElement
    <Nullable>enable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <LangVersion>$langVersion</LangVersion>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
    <EnableNETAnalyzers>false</EnableNETAnalyzers>
    <RunAnalyzersDuringBuild>true</RunAnalyzersDuringBuild>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Moq" Version="$MoqVersion" />
  </ItemGroup>
  <ItemGroup>
    <Analyzer Include="$analyzerDir/Moq.Analyzers.dll" />
    <Analyzer Include="$analyzerDir/Moq.CodeFixes.dll" />
    <Analyzer Include="$analyzerDir/Microsoft.CodeAnalysis.AnalyzerUtilities.dll" />
  </ItemGroup>
</Project>
"@ | Set-Content -LiteralPath (Join-Path $work 'Snippet.csproj')

    # Force every Moq rule to at least 'warning' so Info-severity rules appear in build output.
    # IDs come from the repo's single source of truth, so this never goes stale.
    $diagnosticIds = Get-Content -LiteralPath (Join-Path $repoRoot 'src/Common/DiagnosticIds.cs') -Raw
    $ids = [regex]::Matches($diagnosticIds, '"(Moq\d{4})"') |
        ForEach-Object { $_.Groups[1].Value } |
        Sort-Object -Unique -CaseSensitive
    $editorConfig = @('root = true', '', '[*.cs]') + ($ids | ForEach-Object { "dotnet_diagnostic.$_.severity = warning" })
    $editorConfig | Set-Content -LiteralPath (Join-Path $work '.editorconfig')

    $buildLog = Join-Path $work 'build.log'
    dotnet build (Join-Path $work 'Snippet.csproj') -nologo -v quiet -consoleLoggerParameters:NoSummary *> $buildLog
    $buildExit = $LASTEXITCODE
    $logLines = @(Get-Content -LiteralPath $buildLog)

    # MSBuild echoes each diagnostic once per target invocation, so strip the project
    # suffix and deduplicate.
    function Select-LogLine([string]$Pattern) {
        # Callers wrap the result in @() because PowerShell unwraps a 0- or 1-item array return.
        return @($logLines |
            Where-Object { $_ -cmatch $Pattern } |
            ForEach-Object { $_ -replace ' \[.*Snippet\.csproj\]$', '' } |
            Sort-Object -Unique -CaseSensitive)
    }

    $moqLines = @(Select-LogLine 'Moq\d{4}')
    $csErrors = @(Select-LogLine 'error CS\d+')
    # AD0001 = Roslyn's "analyzer threw an exception". It is a warning, so the build exits 0.
    $adCrashes = @(Select-LogLine 'AD0001')
    # CS8032 ("instance of analyzer cannot be created"), CS8034 ("unable to load Analyzer
    # assembly"), and CS9057 (the analyzer needs a newer compiler than the host runs) all mean
    # the analyzer never ran. CS8033 ("assembly does not contain any analyzers") is EXCLUDED:
    # it fires benignly for the code-fix and utility assemblies.
    $loadFailures = @(Select-LogLine 'CS803[24]|CS9057')

    if ($csErrors.Count -gt 0) {
        Write-Output '--- snippet failed to compile (fix these first; analyzers may be unreliable on broken code) ---'
        $csErrors | ForEach-Object { Write-Output $_ }
    }

    if ($loadFailures.Count -gt 0) {
        Write-Output '--- ANALYZER LOAD FAILURE (CS8032/CS8034/CS9057): the analyzers never loaded; any result below is meaningless ---'
        $loadFailures | ForEach-Object { Write-Output $_ }
    }

    if ($adCrashes.Count -gt 0) {
        Write-Output '--- ANALYZER CRASH (AD0001): priority-1 failure; an analyzer threw an exception ---'
        $adCrashes | ForEach-Object { Write-Output $_ }
    }

    Write-Output "--- Moq.Analyzers diagnostics (config=$config, Moq $MoqVersion, $tfm, C# $langVersion) ---"
    if ($moqLines.Count -gt 0) {
        $moqLines | ForEach-Object { Write-Output $_ }
    }
    else {
        Write-Output '(none)'
    }

    # A load failure outranks every other result: with no loaded analyzers, "(none)" would be a lie.
    if ($loadFailures.Count -gt 0) { exit 4 }
    # A crash outranks the rest: Roslyn disables the crashing analyzer for the session.
    if ($adCrashes.Count -gt 0) { exit 3 }
    if ($csErrors.Count -gt 0) { exit 2 }
    if ($buildExit -ne 0 -and $moqLines.Count -eq 0) {
        [Console]::Error.WriteLine('--- build failed for a non-snippet reason; full log follows ---')
        $logLines | ForEach-Object { [Console]::Error.WriteLine($_) }
        exit 1
    }
    exit 0
}
finally {
    if (-not $keepHarness) {
        Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue
    }
}
