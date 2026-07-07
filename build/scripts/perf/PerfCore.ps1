# Based on https://github.com/dotnet/roslyn-analyzers/blob/43709af7570da7140fb3e9a5237f55ffb24677e7/eng/perf/PerfCore.ps1
[CmdletBinding(PositionalBinding=$false)]
Param(
    [string] $projects,
    [string][Alias('v')]$verbosity = "minimal",
    [string] $filter,
    [switch] $etl,
    [switch] $diff,
    [switch] $ci,
    [switch] $help,
    [Parameter(ValueFromRemainingArguments=$true)][String[]]$properties
  )

Import-Module (Join-Path $PSScriptRoot 'PerfUtils.psm1') -Force -DisableNameChecking
Import-Module (Join-Path $PSScriptRoot 'PerfConfig.psm1') -Force -DisableNameChecking

  function Print-Usage() {
    Write-Host "Common settings:"
    Write-Host "  -verbosity <value>      Msbuild verbosity: q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic] (short: -v)"
    Write-Host "  -help                   Print help and exit"
    Write-Host ""
  
    Write-Host "Actions:"
    Write-Host "  -diff                   Compare to baseline perf results"
    Write-Host ""
    
    Write-Host "Advanced settings:"
    Write-Host "  -etl                    Capture ETL traces of performance tests (Windows only, requires admin permissions, default value is 'false')"
    Write-Host "  -filter                 Filter for tests to run (supports wildcards)"
    Write-Host "  -projects <value>       Semi-colon delimited list of relative paths to benchmark projects."
    Write-Host ""
  
    Write-Host "Command line arguments not listed above are passed thru to msbuild."
    Write-Host "The above arguments can be shortened as much as to be unambiguous (e.g. -co for configuration, -t for test, etc.)."
  }

try {
    # Check if running on Windows and warn about ETL on non-Windows platforms
    $etl = Resolve-PerfEtl -Etl $etl

    if ($help -or (($null -ne $properties) -and ($properties.Contains('/help') -or $properties.Contains('/?')))) {
        Print-Usage
        exit 0
    }

    $projects = Normalize-PerfProjects -Projects $projects
    $filter = Normalize-PerfFilter -Filter $filter

    $RepoRoot = Get-RepoRoot
    $output = Get-PerfOutputRoot -RepoRoot $RepoRoot

    #  Diff two different SHAs
    if ($diff) {
        $forceBaseline = Test-ForcePerfBaseline
        $DiffPerfToBaseLine = Join-Path $RepoRoot "build\scripts\perf\DiffPerfToBaseline.ps1"
        $baselineJsonPath = Get-PerfBaselineJsonPath -RepoRoot $RepoRoot
        $baselinejson = Get-Content -Raw -Path $baselineJsonPath | ConvertFrom-Json
        $baselineSHA = $baselinejson.sha
        $baselineResultsDir = Join-Path $output "baseline"
        $baselineFolder = Join-Path $baselineResultsDir "results"

        Write-Host "Using baseline SHA: '$baselineSHA'."

        $useCachedBaseline = $false

        if ($forceBaseline) {
            Write-Warning "Forcing baseline results to be regenerated."
            $useCachedBaseline = $false
        } elseif (Test-Path $baselineFolder) {
            $exists = Get-ChildItem -Path $baselineFolder -Recurse -File |
                        Where-Object { $_.Name -like "*report-full-compressed.json" } |
                        Select-Object -First 1

            if ($exists) {
                Write-Warning "Using cached baseline results from: '$baselineFolder'."
                $useCachedBaseline = $true
            }
        }

        if (-not $useCachedBaseline) {
            Write-Warning "No cached baseline results found. Will run performance tests to generate new baseline."
        }

        $commandArguments = @{
            baselineSHA = $baselineSHA
            projects = $projects
            output = $output
            filter = $filter
            useCachedBaseline = $useCachedBaseline
        }
        if ($etl) { $commandArguments.etl = $True }
        if ($ci) { $commandArguments.ci =  $True}

        Show-Invocation -ScriptPath $DiffPerfToBaseLine -Arguments $commandArguments
        & $DiffPerfToBaseLine @commandArguments
        if ($LASTEXITCODE -ne 0) { throw "Diff perf to baseline failed with exit code $LASTEXITCODE." }
        exit
    }

    $commandArguments = @{
        projects = $projects
        filter = $filter
        perftestRootFolder = $RepoRoot
        output = "$output\perfTest"
    }
    if ($etl) { $commandArguments.etl = $True }
    if ($ci) { $commandArguments.ci =  $True}

    $RunPerfTests = Join-Path $RepoRoot "build\scripts\perf\RunPerfTests.ps1"

    Show-Invocation -ScriptPath $RunPerfTests -Arguments $commandArguments
    & $RunPerfTests @commandArguments
    if ($LASTEXITCODE -ne 0) { throw "Performance test run failed with exit code $LASTEXITCODE." }
    exit
}
catch {
    Write-Error "$_`n$($_.Exception)`n$($_.ScriptStackTrace)"
    $host.SetShouldExit(1)
    exit 1
}
