[CmdletBinding(PositionalBinding=$false)]
Param(
    [string] $projects,           # semicolon separated list of relative paths to benchmark projects to run
    [string] $filter,             # filter for tests to run (supports wildcards)
    [String] $perftestRootFolder, # root folder all of the  benchmark projects share
    [String] $output,             # folder to write the benchmark results to
    [bool]   $etl=$false,         # capture etl traces for performance tests
    [bool]   $ci=$false           # run in ci mode (fail fast an keep all partial artifacts)
  )

Import-Module (Join-Path $PSScriptRoot 'PerfTestRunner.psm1') -Force -DisableNameChecking

try {
    Invoke-PerfTests @PSBoundParameters
}
catch {
    Write-Error "$_`n$($_.Exception)`n$($_.ScriptStackTrace)"
    $host.SetShouldExit(1)
    exit 1
}
