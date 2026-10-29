[CmdletBinding(PositionalBinding=$false)]
Param(
    [String] $baseline, # folder that contains the baseline results
    [String] $results,   # folder that contains the performance results
    [switch] $ci,       # the scripts is running on a CI server
    [String] $flaggedOutput # file to write the flagged benchmarks to
  )

Import-Module (Join-Path $PSScriptRoot 'PerfResultsAnalyzer.psm1') -Force -DisableNameChecking

# The baseline manager calls this script inside its own pwsh process and reads $LASTEXITCODE.
# A plain exit sets that value. $host.SetShouldExit would instead force the exit
# code of the whole process, after the manager has decided what the result is.
try {
    $exitCode = Invoke-PerfResultsComparison @PSBoundParameters
    exit [int]$exitCode
}
catch {
    Write-Error "$_`n$($_.Exception)`n$($_.ScriptStackTrace)"
    exit 1
}
