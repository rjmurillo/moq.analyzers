[CmdletBinding(PositionalBinding=$false)]
Param(
    [String] $flagged,   # file that lists the flagged benchmarks
    [String] $rounds,    # folder that holds the baseline/rN and current/rN results
    [int] $minRounds,    # fewest rounds before the stop rule applies
    [int] $maxRounds     # round count that forces a decision
  )

Import-Module (Join-Path $PSScriptRoot 'PerfResultsAnalyzer.psm1') -Force -DisableNameChecking

# The baseline manager calls this script inside its own pwsh process and reads $LASTEXITCODE.
# A plain exit sets that value. $host.SetShouldExit would instead force the exit
# code of the whole process, after the manager has decided what the result is.
try {
    $exitCode = Invoke-PerfConfirmationDecision @PSBoundParameters
    exit [int]$exitCode
}
catch {
    Write-Error "$_`n$($_.Exception)`n$($_.ScriptStackTrace)"
    exit 1
}
