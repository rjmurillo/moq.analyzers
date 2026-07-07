[CmdletBinding(PositionalBinding=$false)]
Param(
    [String] $baselineSHA,              # git SHA to use as the baseline for performance
    [String] $output,                   # common folder to write the benchmark results to
    [string] $projects,                 # semicolon separated list of relative paths to benchmark projects to run
    [string] $filter,                   # filter for tests to run (supports wildcards)
    [bool] $etl = $false,               # capture etl traces for performance tests
    [bool] $ci = $false,                # run in ci mode (fail fast an keep all partial artifacts)
    [bool] $useCachedBaseline = $false  # use cached baseline results if available
  )

Import-Module (Join-Path $PSScriptRoot 'PerfBaselineManager.psm1') -Force -DisableNameChecking

try {
    Invoke-PerfBaselineComparison @PSBoundParameters
}
catch {
    Write-Error "$_`n$($_.Exception)`n$($_.ScriptStackTrace)"
    $host.SetShouldExit(1)
    exit 1
}
