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

  function Show-Invocation {
    param(
        [string]$ScriptPath,
        [hashtable]$Arguments
    )
    $parts = @($ScriptPath)
    foreach ($key in $Arguments.Keys) {
        $value = $Arguments[$key]
        if ($null -eq $value) { continue }

        if ($value -eq $true) {
            $parts += "-$key"
        }
        elseif ($value -is [string] -and $value -match '\s') {
            $parts += "-$key `"$value`""
        }
        else {
            $parts += "-$key $value"
        }
    }
    Write-Host "Invoking: $($parts -join ' ')"
}

try {
    # Check if running on Windows and warn about ETL on non-Windows platforms
    $isWindowsPlatform = $PSVersionTable.PSVersion.Major -le 5 -or $IsWindows
    if ($etl -and -not $isWindowsPlatform) {
        Write-Warning "ETL tracing is only supported on Windows. Disabling ETL for this run."
        $etl = $false
    }

    if ($help -or (($null -ne $properties) -and ($properties.Contains('/help') -or $properties.Contains('/?')))) {
        Print-Usage
        exit 0
    }

    if ([string]::IsNullOrWhiteSpace($projects)) {
        $projects = "tests\Moq.Analyzers.Benchmarks\Moq.Analyzers.Benchmarks.csproj"
    }

    if ([string]::IsNullOrWhiteSpace($filter)) {
        $filter = "'*'"
    }

    $RepoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..\..')
    $output = Join-Path $RepoRoot "artifacts\performance\perfResults"

    #  Diff two different SHAs
    if ($diff) {
        $forceBaseline = $env:FORCE_PERF_BASELINE -eq 'true'
        $DiffPerfToBaseLine = Join-Path $RepoRoot "build\scripts\perf\DiffPerfToBaseline.ps1"
        $baselineJsonPath = Resolve-Path (Join-Path $RepoRoot "build\perf\baseline.json")
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
    exit
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    $host.SetShouldExit(1)
    exit 1
}
finally {
}