[CmdletBinding(PositionalBinding=$false)]
Param(
    [String] $baseline, # folder that contains the baseline results
    [String] $results,   # folder that contains the performance results
    [switch] $ci        # the scripts is running on a CI server
  )

Import-Module (Join-Path $PSScriptRoot 'PerfResultsAnalyzer.psm1') -Force -DisableNameChecking

try {
    $exitCode = Invoke-PerfResultsComparison @PSBoundParameters
    $host.SetShouldExit($exitCode)
    exit
}
catch {
    Write-Error "$_`n$($_.Exception)`n$($_.ScriptStackTrace)"
    $host.SetShouldExit(1)
    exit 1
}
