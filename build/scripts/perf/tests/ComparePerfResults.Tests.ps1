BeforeAll {
    $script:PerfRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
    $script:RepoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')
    $script:ScratchRoot = Join-Path $script:RepoRoot "artifacts\pester\perf\ComparePerfResults-$([guid]::NewGuid())"
    New-Item -ItemType Directory -Force -Path $script:ScratchRoot | Out-Null

    # Runs the real ComparePerfResults.ps1 next to a stub analyzer module, invoked with & from a caller script
    # the way Invoke-PerfBaselineComparison does, and returns the $LASTEXITCODE the caller saw.
    function script:Get-CallerSeenExitCode {
        param([Parameter(Mandatory)][string] $StubBody)

        $folder = Join-Path $script:ScratchRoot ([guid]::NewGuid().ToString())
        New-Item -ItemType Directory -Force -Path $folder | Out-Null
        Copy-Item (Join-Path $script:PerfRoot 'ComparePerfResults.ps1') $folder
        Set-Content -Path (Join-Path $folder 'PerfResultsAnalyzer.psm1') -Value $StubBody
        $caller = Join-Path $folder 'caller.ps1'
        Set-Content -Path $caller -Value @'
param([string] $Child)
& $Child -baseline 'b' -results 'r' 2>$null
Write-Output "CALLER_SAW=$LASTEXITCODE"
'@
        $output = & pwsh -NoProfile -File $caller -Child (Join-Path $folder 'ComparePerfResults.ps1') 2>&1
        $line = $output | Where-Object { "$_" -match '^CALLER_SAW=' } | Select-Object -Last 1
        return [int](("$line") -replace '^CALLER_SAW=', '')
    }
}

Describe 'ComparePerfResults.ps1 exit code' {
    It 'surfaces the comparison exit code to a caller that reads LASTEXITCODE' {
        $stub = 'function Invoke-PerfResultsComparison { param($baseline, $results, $ci, $regressedOutput, $confirmOnly) return 3 }; Export-ModuleMember -Function Invoke-PerfResultsComparison'

        Get-CallerSeenExitCode -StubBody $stub | Should -Be 3
    }

    It 'surfaces a zero exit code when the comparison passes' {
        $stub = 'function Invoke-PerfResultsComparison { param($baseline, $results, $ci, $regressedOutput, $confirmOnly) return 0 }; Export-ModuleMember -Function Invoke-PerfResultsComparison'

        Get-CallerSeenExitCode -StubBody $stub | Should -Be 0
    }

    It 'surfaces exit code 1 when the comparison throws' {
        $stub = 'function Invoke-PerfResultsComparison { param($baseline, $results, $ci, $regressedOutput, $confirmOnly) throw "boom" }; Export-ModuleMember -Function Invoke-PerfResultsComparison'

        Get-CallerSeenExitCode -StubBody $stub | Should -Be 1
    }
}

AfterAll {
    Remove-Item -Recurse -Force -Path $script:ScratchRoot -ErrorAction SilentlyContinue
}
