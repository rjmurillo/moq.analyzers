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
  
try {
    # Check if running on Windows and warn about ETL on non-Windows platforms
    $isWindowsPlatform = $PSVersionTable.PSVersion.Major -le 5 -or $IsWindows
    if ($etl -and -not $isWindowsPlatform) {
        Write-Host "Warning: ETL tracing is only supported on Windows. Disabling ETL for this run." -ForegroundColor Yellow
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
        $DiffPerfToBaseLine = Join-Path $RepoRoot "build\scripts\perf\DiffPerfToBaseline.ps1"
        $baselinejson = Get-Content -Raw -Path (Join-Path $RepoRoot "build\perf\baseline.json") | ConvertFrom-Json
        $baselineSHA = $baselinejson.sha
        Write-Host "Running performance comparison against baseline: '$baselineSHA'"
        $commandArguments = @{
            baselineSHA = $baselineSHA
            projects = $projects
            output = $output
            filter = $filter
        }
        if ($etl) { $commandArguments.etl = $True }
        if ($ci) { $commandArguments.ci =  $True}
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