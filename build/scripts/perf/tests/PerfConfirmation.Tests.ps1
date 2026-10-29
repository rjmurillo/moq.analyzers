BeforeAll {
    $script:PerfRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
    $script:RepoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')
    $script:ScratchRoot = Join-Path $script:RepoRoot "artifacts\pester\perf\PerfConfirmation-$([guid]::NewGuid())"
    New-Item -ItemType Directory -Force -Path $script:ScratchRoot | Out-Null
    Import-Module (Join-Path $script:PerfRoot 'PerfBaselineManager.psm1') -Force -DisableNameChecking

    # Stub for RunPerfTests.ps1. It logs "<parent>/<leaf>|root|filter|skipBuild|etl"
    # and writes a fake report. STUB_FAIL_RUN names a "<parent>/<leaf>" run that exits 1.
    $script:RunStub = Join-Path $script:ScratchRoot 'RunPerfTestsStub.ps1'
    Set-Content -Path $script:RunStub -Value @'
param(
    [string] $projects,
    [string[]] $filter,
    [string] $perftestRootFolder,
    [string] $output,
    [bool] $etl = $false,
    [bool] $ci = $false,
    [bool] $skipBuild = $false
)
$run = "$(Split-Path (Split-Path $output -Parent) -Leaf)/$(Split-Path $output -Leaf)"
Add-Content -Path $env:STUB_LOG -Value "$run|$perftestRootFolder|$($filter -join ',')|$skipBuild|$etl"
New-Item -ItemType Directory -Force -Path $output | Out-Null
New-Item -ItemType File -Force -Path (Join-Path $output 'stub-report-full-compressed.json') | Out-Null
# Real runs write build output to the pipeline.
Write-Output "stub run output $run"
if ($env:STUB_FAIL_RUN -eq $run) { exit 1 }
exit 0
'@

    # Stub for ComparePerfResults.ps1. STUB_FLAGGED_JSON is written to the flagged file
    # when set. STUB_COMPARE_EXIT is the exit code.
    $script:CompareStub = Join-Path $script:ScratchRoot 'ComparePerfResultsStub.ps1'
    Set-Content -Path $script:CompareStub -Value @'
param([string] $baseline, [string] $results, [switch] $ci, [string] $flaggedOutput)
Add-Content -Path $env:STUB_LOG -Value "compare|$flaggedOutput"
if ($env:STUB_FLAGGED_JSON) { Set-Content -Path $flaggedOutput -Value $env:STUB_FLAGGED_JSON }
exit [int]$env:STUB_COMPARE_EXIT
'@

    # Stub for ConfirmPerfResults.ps1. Each call returns the next code in STUB_CONFIRM_CODES.
    $script:ConfirmStub = Join-Path $script:ScratchRoot 'ConfirmPerfResultsStub.ps1'
    Set-Content -Path $script:ConfirmStub -Value @'
param([string] $flagged, [string] $rounds, [int] $minRounds, [int] $maxRounds)
$calls = @(Get-Content -Path $env:STUB_LOG | Where-Object { $_ -like 'confirm|*' }).Count
Add-Content -Path $env:STUB_LOG -Value "confirm|$minRounds|$maxRounds|$rounds"
Write-Output "stub confirm output"
exit [int](($env:STUB_CONFIRM_CODES -split ',')[$calls])
'@

    $script:TwoFlagged = '[{"BenchmarkId":"Ns.B.M(FileCount: 1000)","Metric":"P95","MinimumEffect":0.0,"BudgetNs":250000000.0},{"BenchmarkId":"Ns.A.M(FileCount: 1000)","Metric":"Mean","MinimumEffect":0.05,"BudgetNs":null},{"BenchmarkId":"Ns.B.M(FileCount: 1000)","Metric":"Mean","MinimumEffect":0.05,"BudgetNs":null}]'

    function Invoke-Confirmation {
        param([int] $MinRounds = 3, [int] $MaxRounds = 10, [int] $TimeBudgetMinutes = 40)

        Invoke-PerfConfirmation -FlaggedPath $script:Flagged -BaselineRoot '/base' -CurrentRoot '/head' -Projects 'a.csproj' `
            -Output $script:Output -MinRounds $MinRounds -MaxRounds $MaxRounds -TimeBudgetMinutes $TimeBudgetMinutes `
            -RunPerfTestsPath $script:RunStub -ConfirmPerfResultsPath $script:ConfirmStub
    }

    function Invoke-Comparison {
        [CmdletBinding()]
        param([bool] $UseCachedBaseline = $false)

        Invoke-PerfBaselineComparison -baselineSHA 'abc' -output $script:Output -filter "'*'" -useCachedBaseline $UseCachedBaseline `
            -RunPerfTestsPath $script:RunStub -ComparePerfResultsPath $script:CompareStub -ConfirmPerfResultsPath $script:ConfirmStub
    }
}

AfterAll {
    Remove-Item -Path $script:ScratchRoot -Recurse -Force -ErrorAction SilentlyContinue
}

Describe 'Invoke-PerfConfirmation' {
    BeforeEach {
        $script:CaseRoot = Join-Path $script:ScratchRoot "case-$([guid]::NewGuid())"
        $script:Output = Join-Path $script:CaseRoot 'perfResults'
        New-Item -ItemType Directory -Force -Path $script:Output | Out-Null
        $script:Flagged = Join-Path $script:CaseRoot 'flagged.json'
        Set-Content -Path $script:Flagged -Value $script:TwoFlagged
        $env:STUB_LOG = Join-Path $script:CaseRoot 'calls.log'
        New-Item -ItemType File -Force -Path $env:STUB_LOG | Out-Null
        $env:STUB_FAIL_RUN = ''
        $env:STUB_CONFIRM_CODES = '0'
    }

    AfterEach {
        Remove-Item Env:STUB_LOG, Env:STUB_FAIL_RUN, Env:STUB_CONFIRM_CODES -ErrorAction SilentlyContinue
    }

    It 'alternates the build order, builds only in round 1, and reruns only flagged benchmarks' {
        $env:STUB_CONFIRM_CODES = '3,0'

        $confirmed = Invoke-Confirmation

        $confirmed | Should -BeFalse
        $calls = @(Get-Content $env:STUB_LOG | ForEach-Object { , ($_ -split '\|') })
        ($calls | ForEach-Object { $_[0] }) | Should -Be @(
            'baseline/r1', 'current/r1',
            'current/r2', 'baseline/r2',
            'baseline/r3', 'current/r3', 'confirm',
            'current/r4', 'baseline/r4', 'confirm')
        $calls[0][1] | Should -Be '/base'
        $calls[1][1] | Should -Be '/head'
        $calls[0][2] | Should -Be 'Ns.A.M(FileCount: 1000),Ns.B.M(FileCount: 1000)'
        ($calls | Where-Object { $_[0] -like '*/r1' } | ForEach-Object { $_[3] }) | Should -Be @('False', 'False')
        ($calls | Where-Object { $_[0] -like '*/r[234]' } | ForEach-Object { $_[3] } | Sort-Object -Unique) | Should -Be 'True'
        ($calls | Where-Object { $_[0] -notlike 'confirm' } | ForEach-Object { $_[4] } | Sort-Object -Unique) | Should -Be 'False'
    }

    It 'passes the round bounds and the rounds folder to the decision' {
        Invoke-Confirmation | Out-Null

        $decision = (Get-Content $env:STUB_LOG | Where-Object { $_ -like 'confirm|*' }) -split '\|'
        $decision[1] | Should -Be '3'
        $decision[2] | Should -Be '10'
        $decision[3] | Should -Be (Join-Path $script:Output 'confirm')
    }

    It 'returns true when the decision confirms a regression' {
        $env:STUB_CONFIRM_CODES = '4'

        Invoke-Confirmation | Should -BeTrue
    }

    It 'returns only the decision, not the output of the runs' {
        $result = @(Invoke-Confirmation 6>$null)

        $result.Count | Should -Be 1
        $result[0] | Should -BeOfType [bool]
        $result[0] | Should -BeFalse
    }

    It 'forces a decision at the current round when another round would pass the time budget' {
        # Rounds take 10, 10, and 15 minutes. A fourth 15 minute round would end at 50 minutes.
        $global:PerfClockTicks = [System.Collections.Queue]::new([double[]](10, 20, 35))
        Mock Get-PerfElapsedMinutes -ModuleName PerfBaselineManager { $global:PerfClockTicks.Dequeue() }

        Invoke-Confirmation -TimeBudgetMinutes 40 | Out-Null

        $decision = (Get-Content $env:STUB_LOG | Where-Object { $_ -like 'confirm|*' }) -split '\|'
        $decision[2] | Should -Be '3'
        Remove-Variable -Name PerfClockTicks -Scope Global
    }

    It 'keeps asking for rounds while the next round fits the time budget' {
        $env:STUB_CONFIRM_CODES = '3,0'
        $global:PerfClockTicks = [System.Collections.Queue]::new([double[]](10, 20, 30, 40))
        Mock Get-PerfElapsedMinutes -ModuleName PerfBaselineManager { $global:PerfClockTicks.Dequeue() }

        Invoke-Confirmation -TimeBudgetMinutes 45 | Out-Null

        $decisions = @(Get-Content $env:STUB_LOG | Where-Object { $_ -like 'confirm|*' })
        ($decisions[0] -split '\|')[2] | Should -Be '10'
        ($decisions[1] -split '\|')[2] | Should -Be '4'
        Remove-Variable -Name PerfClockTicks -Scope Global
    }

    It 'throws before the minimum rounds when they cannot fit the time budget' {
        # Round 2 takes 20 minutes and ends at 25, so round 3 would end at about 45.
        $global:PerfClockTicks = [System.Collections.Queue]::new([double[]](5, 25))
        Mock Get-PerfElapsedMinutes -ModuleName PerfBaselineManager { $global:PerfClockTicks.Dequeue() }

        { Invoke-Confirmation -TimeBudgetMinutes 40 } | Should -Throw '*needs about 45 minutes for 3 rounds, over its 40 minute budget. The screen result stands*'
        @(Get-Content $env:STUB_LOG | Where-Object { $_ -like '*/r3|*' }).Count | Should -Be 0
        Remove-Variable -Name PerfClockTicks -Scope Global
    }

    It 'does not project later rounds from the build time in round 1' {
        # Round 1 builds and takes 30 minutes. Rounds 2 and 3 take 2 minutes each.
        $global:PerfClockTicks = [System.Collections.Queue]::new([double[]](30, 32, 34))
        Mock Get-PerfElapsedMinutes -ModuleName PerfBaselineManager { $global:PerfClockTicks.Dequeue() }

        Invoke-Confirmation -TimeBudgetMinutes 40 | Should -BeFalse

        @(Get-Content $env:STUB_LOG | Where-Object { $_ -like 'confirm|*' }).Count | Should -Be 1
        Remove-Variable -Name PerfClockTicks -Scope Global
    }

    It 'throws when the decision still asks for a round at the last round' {
        $env:STUB_CONFIRM_CODES = '3'

        { Invoke-Confirmation -MaxRounds 3 } | Should -Throw '*Confirmation analysis failed with exit code 3 after round 3*'
    }

    It 'throws when the decision fails' {
        $env:STUB_CONFIRM_CODES = '1'

        { Invoke-Confirmation } | Should -Throw '*Confirmation analysis failed with exit code 1 after round 3*'
    }

    It 'throws when a round fails' {
        $env:STUB_FAIL_RUN = 'current/r2'

        { Invoke-Confirmation } | Should -Throw '*Confirmation round 2 (current) failed with exit code 1*'
    }

    It 'throws when the flagged file lists no benchmarks' {
        Set-Content -Path $script:Flagged -Value '[]'

        { Invoke-Confirmation } | Should -Throw '*lists no benchmarks*'
        @(Get-Content $env:STUB_LOG).Count | Should -Be 0
    }

    It 'throws when MaxRounds is below MinRounds' {
        { Invoke-Confirmation -MinRounds 4 -MaxRounds 3 } | Should -Throw '*MaxRounds (3) must be at least MinRounds (4)*'
    }

    It 'removes rounds left from an earlier run' {
        $stale = Join-Path $script:Output 'confirm\baseline\r9'
        New-Item -ItemType Directory -Force -Path $stale | Out-Null

        Invoke-Confirmation | Out-Null

        $stale | Should -Not -Exist
        Join-Path $script:Output 'confirm\current\r3' | Should -Exist
    }

    It 'defaults the decision script to a file next to the module' {
        $paramAst = (Get-Command Invoke-PerfConfirmation).ScriptBlock.Ast.Body.ParamBlock.Parameters |
            Where-Object { $_.Name.VariablePath.UserPath -eq 'ConfirmPerfResultsPath' }
        $fileName = [regex]::Match($paramAst.DefaultValue.Extent.Text, '"([^"]+\.ps1)"').Groups[1].Value

        Join-Path $script:PerfRoot $fileName | Should -Exist
    }
}

Describe 'Invoke-PerfBaselineComparison confirmation handoff' {
    BeforeEach {
        $script:CaseRoot = Join-Path $script:ScratchRoot "case-$([guid]::NewGuid())"
        $script:Output = Join-Path $script:CaseRoot 'perfResults'
        New-Item -ItemType Directory -Force -Path $script:Output | Out-Null
        $env:STUB_LOG = Join-Path $script:CaseRoot 'calls.log'
        New-Item -ItemType File -Force -Path $env:STUB_LOG | Out-Null
        $env:STUB_FAIL_RUN = ''
        $env:STUB_COMPARE_EXIT = '1'
        $env:STUB_FLAGGED_JSON = $script:TwoFlagged
        $env:STUB_CONFIRM_CODES = '0'
        $script:GitLog = Join-Path $script:CaseRoot 'git.log'
        $caseRoot = $script:CaseRoot
        $gitLog = $script:GitLog
        Mock Get-RepoRoot { $caseRoot } -ModuleName PerfBaselineManager
        Mock git { Add-Content -Path $gitLog -Value ($args -join ' '); $global:LASTEXITCODE = 0 } -ModuleName PerfBaselineManager
        Push-Location $script:CaseRoot
    }

    AfterEach {
        Pop-Location
        Remove-Item Env:STUB_LOG, Env:STUB_FAIL_RUN, Env:STUB_COMPARE_EXIT, Env:STUB_FLAGGED_JSON, Env:STUB_CONFIRM_CODES -ErrorAction SilentlyContinue
    }

    It 'passes when the screen flags nothing' {
        $env:STUB_COMPARE_EXIT = '0'
        $env:STUB_FLAGGED_JSON = '[]'

        Invoke-Comparison

        Get-Content $env:STUB_LOG | Where-Object { $_ -like 'baseline/r*' -or $_ -like 'current/r*' -or $_ -like 'confirm|*' } | Should -BeNullOrEmpty
    }

    It 'writes the flagged file next to the results and removes a stale one' {
        $env:STUB_COMPARE_EXIT = '0'
        $env:STUB_FLAGGED_JSON = ''
        $flagged = Join-Path $script:Output 'flagged.json'
        Set-Content -Path $flagged -Value 'stale'

        Invoke-Comparison

        (Get-Content $env:STUB_LOG | Where-Object { $_ -like 'compare|*' }) | Should -Be "compare|$flagged"
        $flagged | Should -Not -Exist
    }

    It 'passes with a warning when confirmation does not reproduce the screen result' {
        Invoke-Comparison -WarningVariable warnings 3>$null

        $warnings | Should -BeLike '*flagged 3 benchmark(s), but confirmation rounds did not reproduce a regression*'
        @(Get-Content $env:STUB_LOG | Where-Object { $_ -like 'confirm|*' }).Count | Should -Be 1
    }

    It 'fails when confirmation reproduces the regression' {
        $env:STUB_CONFIRM_CODES = '4'

        { Invoke-Comparison } | Should -Throw '*Confirmation rounds reproduced a performance regression*'
    }

    It 'fails without confirmation when the comparison writes no flagged file' {
        $env:STUB_FLAGGED_JSON = ''

        { Invoke-Comparison } | Should -Throw '*Performance comparison failed with exit code 1.'
        Get-Content $env:STUB_LOG | Where-Object { $_ -like 'confirm|*' } | Should -BeNullOrEmpty
    }

    It 'fails without confirmation when the comparison flags no benchmarks' {
        $env:STUB_FLAGGED_JSON = '[]'

        { Invoke-Comparison } | Should -Throw '*failed with exit code 1 and flagged no benchmarks*'
    }

    It 'checks out the baseline for confirmation when the baseline came from the cache' {
        $cached = Join-Path $script:Output 'baseline'
        New-Item -ItemType Directory -Force -Path $cached | Out-Null
        New-Item -ItemType File -Force -Path (Join-Path $cached 'cached-report-full-compressed.json') | Out-Null
        Set-Content -Path (Join-Path $cached 'baseline-sha.txt') -Value 'abc' -NoNewline

        Invoke-Comparison -UseCachedBaseline $true

        $baselineFolder = Join-Path (Join-Path $script:CaseRoot 'artifacts') 'perfBaseline'
        Get-Content $script:GitLog | Should -Be "worktree add $baselineFolder abc -f"
        (Get-Content $env:STUB_LOG | Where-Object { $_ -like 'baseline/r1|*' }) -split '\|' | Select-Object -Index 1 | Should -Be $baselineFolder
    }

    It 'fails when the baseline checkout for confirmation fails' {
        $cached = Join-Path $script:Output 'baseline'
        New-Item -ItemType Directory -Force -Path $cached | Out-Null
        New-Item -ItemType File -Force -Path (Join-Path $cached 'cached-report-full-compressed.json') | Out-Null
        Set-Content -Path (Join-Path $cached 'baseline-sha.txt') -Value 'abc' -NoNewline
        Mock git { $global:LASTEXITCODE = 128 } -ModuleName PerfBaselineManager

        { Invoke-Comparison -UseCachedBaseline $true } | Should -Throw '*git worktree add failed with exit code 128*'
    }

    It 'reuses the baseline checkout from the screen run' {
        New-Item -ItemType Directory -Force -Path (Join-Path (Join-Path $script:CaseRoot 'artifacts') 'perfBaseline') | Out-Null

        Invoke-Comparison 3>$null

        @(Get-Content $script:GitLog).Count | Should -Be 1
    }
}
