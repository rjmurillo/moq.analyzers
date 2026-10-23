[CmdletBinding(PositionalBinding=$false)]
Param(
    [String] $baseline, # folder that contains the baseline results
    [String] $results,   # folder that contains the performance results
    [switch] $ci,       # the scripts is running on a CI server
    [String] $regressedOutput, # file to write the ids of the benchmarks that tripped a gate to (JSON array)
    [String] $confirmOnly      # file of ids from a first round; only those benchmarks are gated
  )

Import-Module (Join-Path $PSScriptRoot 'PerfResultsAnalyzer.psm1') -Force -DisableNameChecking

try {
    $exitCode = Invoke-PerfResultsComparison @PSBoundParameters
    $host.SetShouldExit([int]$exitCode)
    exit
}
catch {
    Write-Error "$_`n$($_.Exception)`n$($_.ScriptStackTrace)"
    $host.SetShouldExit(1)
    exit 1
}
