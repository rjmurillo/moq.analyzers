[CmdletBinding(PositionalBinding=$false)]
Param(
    [string] $projects,           # semicolon separated list of relative paths to benchmark projects to run
    [string[]] $filter,           # filters for tests to run (support wildcards)
    [String] $perftestRootFolder, # root folder all of the  benchmark projects share
    [String] $output,             # folder to write the benchmark results to
    [bool]   $etl=$false,         # capture etl traces for performance tests
    [bool]   $ci=$false,          # run in ci mode (fail fast an keep all partial artifacts)
    [bool]   $skipBuild=$false    # reuse the existing build of the benchmark projects
  )

Import-Module (Join-Path $PSScriptRoot 'PerfTestRunner.psm1') -Force -DisableNameChecking

try {
    Invoke-PerfTests @PSBoundParameters
    # Callers read $LASTEXITCODE. With -skipBuild no native command runs here,
    # so without this exit the caller would see an older command's code.
    exit 0
}
catch {
    Write-Error "$_`n$($_.Exception)`n$($_.ScriptStackTrace)"
    $host.SetShouldExit(1)
    exit 1
}
