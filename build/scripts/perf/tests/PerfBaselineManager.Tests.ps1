BeforeAll {
    $script:PerfRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
    $script:RepoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')
    $script:ScratchRoot = Join-Path $script:RepoRoot "artifacts\pester\perf\PerfBaseline-$([guid]::NewGuid())"
    New-Item -ItemType Directory -Force -Path $script:ScratchRoot | Out-Null
    Import-Module (Join-Path $script:PerfRoot 'PerfBaselineManager.psm1') -Force -DisableNameChecking
    Import-Module (Join-Path $script:PerfRoot 'PerfConfig.psm1') -Force -DisableNameChecking
}

AfterAll {
    Remove-Item -Path $script:ScratchRoot -Recurse -Force -ErrorAction SilentlyContinue
}

Describe 'Test-PerfResults' {
    It 'returns false when the results folder is missing' {
        Test-PerfResults -ResultsOutput (Join-Path $script:ScratchRoot 'missing') | Should -BeFalse
    }

    It 'returns false when no compressed report exists' {
        $results = Join-Path $script:ScratchRoot 'emptyResults'
        New-Item -ItemType Directory -Force -Path $results | Out-Null
        New-Item -ItemType File -Force -Path (Join-Path $results 'other.json') | Out-Null

        Test-PerfResults -ResultsOutput $results | Should -BeFalse
    }

    It 'returns true when a compressed benchmark report exists below the folder' {
        $results = Join-Path $script:ScratchRoot 'validResults'
        $nested = Join-Path $results 'nested'
        New-Item -ItemType Directory -Force -Path $nested | Out-Null
        New-Item -ItemType File -Force -Path (Join-Path $nested 'bench-report-full-compressed.json') | Out-Null

        Test-PerfResults -ResultsOutput $results | Should -BeTrue
    }
}

Describe 'New-PerfRunArguments' {
    It 'creates the shared RunPerfTests argument map' {
        $args = New-PerfRunArguments -PerfTestRootFolder '/repo' -Projects 'a.csproj' -Output '/out' -Filter 'abc'

        $args.perftestRootFolder | Should -Be '/repo'
        $args.projects | Should -Be 'a.csproj'
        $args.output | Should -Be '/out'
        $args.filter | Should -Be 'abc'
    }

    It 'adds ETL and CI only when requested' {
        $args = New-PerfRunArguments -PerfTestRootFolder '/repo' -Projects 'a.csproj' -Output '/out' -Filter 'abc' -Etl $true -Ci $true

        $args.etl | Should -BeTrue
        $args.ci | Should -BeTrue
    }

    It 'omits ETL and CI when they are false' {
        $args = New-PerfRunArguments -PerfTestRootFolder '/repo' -Projects 'a.csproj' -Output '/out' -Filter 'abc'

        $args.ContainsKey('etl') | Should -BeFalse
        $args.ContainsKey('ci') | Should -BeFalse
    }
}

Describe 'Warm-up and phase order (#1382)' {
    BeforeAll {
        # Stub for RunPerfTests.ps1. It logs one line per call and writes a fake report.
        # STUB_FAIL_PHASE makes that phase exit 1. STUB_NOREPORT_PHASE skips its report.
        # STUB_STARONLY_PHASE writes that phase's report only when the filter is '*'.
        # STUB_NODIR_PHASE skips creating that phase's results folder.
        $script:RunStub = Join-Path $script:ScratchRoot 'RunPerfTestsStub.ps1'
        Set-Content -Path $script:RunStub -Value @'
param(
    [string] $projects,
    [string[]] $filter,
    [string] $perftestRootFolder,
    [string] $output,
    [bool] $etl = $false,
    [bool] $ci = $false
)
$phase = Split-Path $output -Leaf
# The confirmation rerun writes to '<output>/confirm/<phase>'. Its phases are named 'confirm-<phase>'.
if ((Split-Path (Split-Path $output -Parent) -Leaf) -eq 'confirm') { $phase = "confirm-$phase" }
Add-Content -Path $env:STUB_LOG -Value "$phase|$perftestRootFolder|$projects|$($filter -join ',')|$etl|$ci"
if ($env:STUB_ROLLFORWARD_LOG) { Add-Content -Path $env:STUB_ROLLFORWARD_LOG -Value "$phase|$env:DOTNET_ROLL_FORWARD" }
$skipReport = ($env:STUB_NOREPORT_PHASE -eq $phase) -or ($env:STUB_STARONLY_PHASE -eq $phase -and $filter -ne '*')
if ($env:STUB_NODIR_PHASE -ne $phase) {
    New-Item -ItemType Directory -Force -Path $output | Out-Null
}
if (-not $skipReport -and $env:STUB_NODIR_PHASE -ne $phase) {
    New-Item -ItemType File -Force -Path (Join-Path $output "$phase-report-full-compressed.json") | Out-Null
}
# A trailing '*' (for example 'baseline*') fails only that phase's run with filter '*'.
$failOnStar = $filter -eq '*' -and $env:STUB_FAIL_PHASE -eq "$phase*"
if ($env:STUB_FAIL_PHASE -eq $phase -or $failOnStar) { exit 1 }
exit 0
'@
        # Round 1 comparison: STUB_COMPARE_FAIL makes it exit 1, STUB_COMPARE_EXIT sets any exit code,
        # STUB_REGRESSED_IDS is the JSON it writes to -regressedOutput.
        # Confirmation comparison (-confirmOnly given): STUB_CONFIRM_EXIT sets its exit code (default 0),
        # STUB_CONFIRMED_IDS is the JSON it writes to -regressedOutput.
        $script:CompareStub = Join-Path $script:ScratchRoot 'ComparePerfResultsStub.ps1'
        Set-Content -Path $script:CompareStub -Value @'
param([string] $baseline, [string] $results, [switch] $ci, [string] $regressedOutput, [string] $confirmOnly)
if ($env:STUB_ROLLFORWARD_LOG) { Add-Content -Path $env:STUB_ROLLFORWARD_LOG -Value "compare|$env:DOTNET_ROLL_FORWARD" }
if ($confirmOnly) {
    Add-Content -Path $env:STUB_LOG -Value "confirm-compare|$ci|$confirmOnly|$regressedOutput|$(Split-Path $baseline -Leaf)|$(Split-Path $results -Leaf)"
    if ($env:STUB_CONFIRMED_IDS -and $regressedOutput) { Set-Content -Path $regressedOutput -Value $env:STUB_CONFIRMED_IDS }
    if ($env:STUB_CONFIRM_EXIT) { exit ([int]$env:STUB_CONFIRM_EXIT) }
    exit 0
}
Add-Content -Path $env:STUB_LOG -Value "compare|$ci"
if ($env:STUB_REGRESSED_IDS -and $regressedOutput) { Set-Content -Path $regressedOutput -Value $env:STUB_REGRESSED_IDS }
if ($env:STUB_COMPARE_EXIT) { exit ([int]$env:STUB_COMPARE_EXIT) }
if ($env:STUB_COMPARE_FAIL) { exit 1 }
exit 0
'@
    }

    BeforeEach {
        $script:CaseRoot = Join-Path $script:ScratchRoot "case-$([guid]::NewGuid())"
        $script:Output = Join-Path $script:CaseRoot 'perfResults'
        New-Item -ItemType Directory -Force -Path $script:Output | Out-Null
        $env:STUB_LOG = Join-Path $script:CaseRoot 'calls.log'
        $env:STUB_FAIL_PHASE = ''
        $env:STUB_NOREPORT_PHASE = ''
        $env:STUB_STARONLY_PHASE = ''
        $env:STUB_NODIR_PHASE = ''
        $env:STUB_COMPARE_FAIL = ''
        $env:STUB_COMPARE_EXIT = ''
        $env:STUB_REGRESSED_IDS = ''
        $env:STUB_CONFIRM_EXIT = ''
        $env:STUB_CONFIRMED_IDS = ''
        $env:STUB_ROLLFORWARD_LOG = ''
        $script:SavedRollForward = $env:DOTNET_ROLL_FORWARD
        $caseRoot = $script:CaseRoot
        Mock Get-RepoRoot { $caseRoot } -ModuleName PerfBaselineManager
        Mock git { $global:LASTEXITCODE = 0 } -ModuleName PerfBaselineManager
        # Run from the case folder so a stray folder in the current directory (#1383)
        # is cleaned up with the case and is visible to the tests that check for it.
        Push-Location $script:CaseRoot
    }

    AfterEach {
        Pop-Location
        Remove-Item Env:STUB_LOG, Env:STUB_FAIL_PHASE, Env:STUB_NOREPORT_PHASE, Env:STUB_STARONLY_PHASE, Env:STUB_NODIR_PHASE, Env:STUB_COMPARE_FAIL, Env:STUB_COMPARE_EXIT, Env:STUB_REGRESSED_IDS, Env:STUB_CONFIRM_EXIT, Env:STUB_CONFIRMED_IDS, Env:STUB_ROLLFORWARD_LOG -ErrorAction SilentlyContinue
        $env:DOTNET_ROLL_FORWARD = $script:SavedRollForward
    }

    It 'runs every phase on the latest major runtime' {
        $env:STUB_ROLLFORWARD_LOG = Join-Path $script:CaseRoot 'rollforward.log'

        Invoke-PerfBaselineComparison -baselineSHA 'abc' -output $script:Output -filter "'*'" `
            -RunPerfTestsPath $script:RunStub -ComparePerfResultsPath $script:CompareStub

        Get-Content $env:STUB_ROLLFORWARD_LOG |
            Should -Be @('warmup|LatestMajor', 'baseline|LatestMajor', 'perfTest|LatestMajor', 'compare|LatestMajor')
    }

    It 'restores DOTNET_ROLL_FORWARD when it was <Case>' -ForEach @(
        @{ Case = 'set'; Previous = 'Disable' }
        @{ Case = 'unset'; Previous = $null }
    ) {
        $env:DOTNET_ROLL_FORWARD = $Previous

        Invoke-PerfBaselineComparison -baselineSHA 'abc' -output $script:Output -filter "'*'" `
            -RunPerfTestsPath $script:RunStub -ComparePerfResultsPath $script:CompareStub

        $env:DOTNET_ROLL_FORWARD | Should -Be $Previous
    }

    It 'restores DOTNET_ROLL_FORWARD when the comparison fails' {
        $env:DOTNET_ROLL_FORWARD = 'Disable'
        $env:STUB_FAIL_PHASE = 'perfTest'

        { Invoke-PerfBaselineComparison -baselineSHA 'abc' -output $script:Output -filter "'*'" `
            -RunPerfTestsPath $script:RunStub -ComparePerfResultsPath $script:CompareStub } |
            Should -Throw '*Performance test run failed with exit code 1*'

        $env:DOTNET_ROLL_FORWARD | Should -Be 'Disable'
    }

    It 'runs the warm-up before the baseline and head phases (AC1)' {
        Invoke-PerfBaselineComparison -baselineSHA 'abc' -output $script:Output -filter "'*'" `
            -RunPerfTestsPath $script:RunStub -ComparePerfResultsPath $script:CompareStub

        $phases = Get-Content $env:STUB_LOG | ForEach-Object { ($_ -split '\|')[0] }
        $phases | Should -Be @('warmup', 'baseline', 'perfTest', 'compare')
    }

    It 'runs the warm-up before the head phase when the baseline is cached (AC4)' {
        $cached = Join-Path $script:Output 'baseline'
        New-Item -ItemType Directory -Force -Path $cached | Out-Null
        New-Item -ItemType File -Force -Path (Join-Path $cached 'cached-report-full-compressed.json') | Out-Null

        Invoke-PerfBaselineComparison -baselineSHA 'abc' -output $script:Output -filter "'*'" -useCachedBaseline $true `
            -RunPerfTestsPath $script:RunStub -ComparePerfResultsPath $script:CompareStub

        $phases = Get-Content $env:STUB_LOG | ForEach-Object { ($_ -split '\|')[0] }
        $phases | Should -Be @('warmup', 'perfTest', 'compare')
    }

    It 'writes warm-up results to their own folder (AC2)' {
        Invoke-PerfWarmup -RepoRoot $script:CaseRoot -Output $script:Output -RunPerfTestsPath $script:RunStub

        Join-Path $script:Output 'warmup' | Should -Exist
        Join-Path $script:Output 'baseline' | Should -Not -Exist
        Join-Path $script:Output 'perfTest' | Should -Not -Exist
    }

    It 'pins the repo root, default project, warm-up filter, and ETL off (AC1)' {
        Invoke-PerfWarmup -RepoRoot $script:CaseRoot -Output $script:Output -RunPerfTestsPath $script:RunStub

        $fields = (Get-Content $env:STUB_LOG | Select-Object -First 1) -split '\|'
        $fields[1] | Should -Be $script:CaseRoot
        $fields[2] | Should -Be (Get-PerfDefaultProjects)
        $fields[3] | Should -Be '*Moq1000SealedClassBenchmarks*'
        $fields[4] | Should -Be 'False'
        $fields[5] | Should -Be 'False'
    }

    It 'keeps ETL off for the warm-up when the comparison runs with ETL (AC1)' {
        Invoke-PerfBaselineComparison -baselineSHA 'abc' -output $script:Output -filter "'*'" -etl $true `
            -RunPerfTestsPath $script:RunStub -ComparePerfResultsPath $script:CompareStub

        $etlByPhase = @{}
        Get-Content $env:STUB_LOG | Where-Object { $_ -notlike 'compare*' } | ForEach-Object {
            $fields = $_ -split '\|'
            $etlByPhase[$fields[0]] = $fields[4]
        }
        $etlByPhase['warmup'] | Should -Be 'False'
        $etlByPhase['baseline'] | Should -Be 'True'
        $etlByPhase['perfTest'] | Should -Be 'True'
    }

    It 'passes CI mode through to the warm-up (AC1)' {
        Invoke-PerfWarmup -RepoRoot $script:CaseRoot -Output $script:Output -Ci $true -RunPerfTestsPath $script:RunStub

        $fields = (Get-Content $env:STUB_LOG | Select-Object -First 1) -split '\|'
        $fields[5] | Should -Be 'True'
    }

    It 'stops the comparison when the warm-up exits non-zero (AC3)' {
        $env:STUB_FAIL_PHASE = 'warmup'

        { Invoke-PerfBaselineComparison -baselineSHA 'abc' -output $script:Output -filter "'*'" `
            -RunPerfTestsPath $script:RunStub -ComparePerfResultsPath $script:CompareStub } |
            Should -Throw '*Warm-up perf run failed with exit code 1*'

        $expected = "warmup|$($script:CaseRoot)|$(Get-PerfDefaultProjects)|*Moq1000SealedClassBenchmarks*|False|False"
        Get-Content $env:STUB_LOG | Should -Be $expected
    }

    It 'stops the comparison when the warm-up writes no report (AC3)' {
        $env:STUB_NOREPORT_PHASE = 'warmup'

        { Invoke-PerfBaselineComparison -baselineSHA 'abc' -output $script:Output -filter "'*'" `
            -RunPerfTestsPath $script:RunStub -ComparePerfResultsPath $script:CompareStub } |
            Should -Throw '*Warm-up perf run produced no benchmark report*'

        @(Get-Content $env:STUB_LOG).Count | Should -Be 1
    }

    It 'defaults every script path to a file next to the module (AC1)' -ForEach @(
        @{ Function = 'Invoke-PerfWarmup'; Parameter = 'RunPerfTestsPath' }
        @{ Function = 'Invoke-PerfBaselineComparison'; Parameter = 'RunPerfTestsPath' }
        @{ Function = 'Invoke-PerfBaselineComparison'; Parameter = 'ComparePerfResultsPath' }
    ) {
        $paramAst = (Get-Command $Function).ScriptBlock.Ast.Body.ParamBlock.Parameters |
            Where-Object { $_.Name.VariablePath.UserPath -eq $Parameter }
        $fileName = [regex]::Match($paramAst.DefaultValue.Extent.Text, '"([^"]+\.ps1)"').Groups[1].Value

        $fileName | Should -Not -BeNullOrEmpty
        Join-Path $script:PerfRoot $fileName | Should -Exist
    }

    It 'uses the scripts next to the module when no paths are passed (AC1)' {
        $runStub = $script:RunStub
        $compareStub = $script:CompareStub
        # Pester 6 no longer falls through to the real command when no filter matches.
        # The default mock calls the real cmdlet by its module-qualified name.
        Mock Join-Path { Microsoft.PowerShell.Management\Join-Path @PesterBoundParameters } -ModuleName PerfBaselineManager
        Mock Join-Path { $runStub } -ModuleName PerfBaselineManager -ParameterFilter { $ChildPath -eq 'RunPerfTests.ps1' }
        Mock Join-Path { $compareStub } -ModuleName PerfBaselineManager -ParameterFilter { $ChildPath -eq 'ComparePerfResults.ps1' }

        Invoke-PerfWarmup -RepoRoot $script:CaseRoot -Output $script:Output
        Invoke-PerfBaselineComparison -baselineSHA 'abc' -output $script:Output -filter "'*'"

        $phases = Get-Content $env:STUB_LOG | ForEach-Object { ($_ -split '\|')[0] }
        $phases | Should -Be @('warmup', 'warmup', 'baseline', 'perfTest', 'compare')
    }

    It 'reruns the baseline with filter * when the first run writes no report' {
        $env:STUB_STARONLY_PHASE = 'baseline'

        Invoke-PerfBaselineComparison -baselineSHA 'abc' -output $script:Output -filter 'Moq*' `
            -RunPerfTestsPath $script:RunStub -ComparePerfResultsPath $script:CompareStub

        $calls = @(Get-Content $env:STUB_LOG | ForEach-Object { , ($_ -split '\|') })
        ($calls | ForEach-Object { $_[0] }) | Should -Be @('warmup', 'baseline', 'baseline', 'perfTest', 'compare')
        $calls[1][3] | Should -Be 'Moq*'
        $calls[2][3] | Should -Be '*'
    }

    It 'stops when <Case>' -ForEach @(
        @{ Case = 'git worktree add fails'; Fail = ''; GitExit = 128; Message = '*git worktree add failed with exit code 128*' }
        @{ Case = 'the baseline run fails'; Fail = 'baseline'; GitExit = 0; Message = '*Baseline perf test run failed with exit code 1*' }
        @{ Case = 'the baseline rerun fails'; Fail = 'baseline*'; GitExit = 0; Message = '*Baseline rerun failed with exit code 1*' }
        @{ Case = 'the head run fails'; Fail = 'perfTest'; GitExit = 0; Message = '*Performance test run failed with exit code 1*' }
    ) {
        $env:STUB_FAIL_PHASE = $Fail
        $env:STUB_STARONLY_PHASE = 'baseline'
        $gitExit = $GitExit
        Mock git { $global:LASTEXITCODE = $gitExit } -ModuleName PerfBaselineManager

        { Invoke-PerfBaselineComparison -baselineSHA 'abc' -output $script:Output -filter 'Moq*' `
            -RunPerfTestsPath $script:RunStub -ComparePerfResultsPath $script:CompareStub } |
            Should -Throw $Message
    }

    It 'throws when the baseline run leaves no results folder (#1431)' {
        $env:STUB_NODIR_PHASE = 'baseline'

        { Invoke-PerfBaselineComparison -baselineSHA 'abc' -output $script:Output -filter "'*'" `
            -RunPerfTestsPath $script:RunStub -ComparePerfResultsPath $script:CompareStub } |
            Should -Throw "*Baseline perf run produced no benchmark report in '$(Join-Path $script:Output 'baseline')'*"

        $phases = Get-Content $env:STUB_LOG | ForEach-Object { ($_ -split '\|')[0] }
        $phases | Should -Be @('warmup', 'baseline')
    }

    It 'throws without a rerun when the baseline run with filter <Filter> writes no report (#1461)' -ForEach @(
        @{ Filter = '*' }
        @{ Filter = "'*'" }
    ) {
        $env:STUB_NOREPORT_PHASE = 'baseline'

        { Invoke-PerfBaselineComparison -baselineSHA 'abc' -output $script:Output -filter $Filter `
            -RunPerfTestsPath $script:RunStub -ComparePerfResultsPath $script:CompareStub } |
            Should -Throw "*Baseline perf run produced no benchmark report in '$(Join-Path $script:Output 'baseline')'*"

        $phases = Get-Content $env:STUB_LOG | ForEach-Object { ($_ -split '\|')[0] }
        $phases | Should -Be @('warmup', 'baseline')
    }

    It 'throws when the baseline rerun with filter * also writes no report (#1461)' {
        $env:STUB_NOREPORT_PHASE = 'baseline'

        { Invoke-PerfBaselineComparison -baselineSHA 'abc' -output $script:Output -filter 'Moq*' `
            -RunPerfTestsPath $script:RunStub -ComparePerfResultsPath $script:CompareStub } |
            Should -Throw "*Baseline perf run produced no benchmark report in '$(Join-Path $script:Output 'baseline')'*"

        $calls = @(Get-Content $env:STUB_LOG | ForEach-Object { , ($_ -split '\|') })
        ($calls | ForEach-Object { $_[0] }) | Should -Be @('warmup', 'baseline', 'baseline')
        $calls[2][3] | Should -Be '*'
    }

    It 'ignores a stale baseline report when the new baseline run writes none (#1461)' {
        $stale = Join-Path $script:Output 'baseline'
        New-Item -ItemType Directory -Force -Path $stale | Out-Null
        New-Item -ItemType File -Force -Path (Join-Path $stale 'old-report-full-compressed.json') | Out-Null
        $env:STUB_NOREPORT_PHASE = 'baseline'

        { Invoke-PerfBaselineComparison -baselineSHA 'abc' -output $script:Output -filter "'*'" `
            -RunPerfTestsPath $script:RunStub -ComparePerfResultsPath $script:CompareStub } |
            Should -Throw "*Baseline perf run produced no benchmark report in '$stale'*"

        Join-Path $stale 'old-report-full-compressed.json' | Should -Not -Exist
    }

    It 'continues to the comparison when the baseline <Case> writes a report (#1461)' -ForEach @(
        @{ Case = 'run'; Filter = "'*'"; StarOnly = ''; Phases = @('warmup', 'baseline', 'perfTest', 'compare') }
        @{ Case = 'rerun'; Filter = 'Moq*'; StarOnly = 'baseline'; Phases = @('warmup', 'baseline', 'baseline', 'perfTest', 'compare') }
    ) {
        $env:STUB_STARONLY_PHASE = $StarOnly

        { Invoke-PerfBaselineComparison -baselineSHA 'abc' -output $script:Output -filter $Filter `
            -RunPerfTestsPath $script:RunStub -ComparePerfResultsPath $script:CompareStub } |
            Should -Not -Throw

        $phases = Get-Content $env:STUB_LOG | ForEach-Object { ($_ -split '\|')[0] }
        $phases | Should -Be $Phases
    }

    It 'creates the perfTest folder under the output folder (#1461)' {
        # The stub does not create the head folder, so only Ensure-Folder can.
        $env:STUB_NODIR_PHASE = 'perfTest'

        $result = Invoke-PerfBaselineComparison -baselineSHA 'abc' -output $script:Output -filter "'*'" `
            -RunPerfTestsPath $script:RunStub -ComparePerfResultsPath $script:CompareStub

        $result | Should -BeNullOrEmpty
        Join-Path $script:Output 'perfTest' | Should -Exist
        Join-Path $script:CaseRoot 'Join-Path' | Should -Not -Exist
    }

    It 'runs the baseline when the cached baseline folder has no report (#1383)' {
        $cached = Join-Path $script:Output 'baseline'
        New-Item -ItemType Directory -Force -Path $cached | Out-Null

        Invoke-PerfBaselineComparison -baselineSHA 'abc' -output $script:Output -filter "'*'" -useCachedBaseline $true `
            -RunPerfTestsPath $script:RunStub -ComparePerfResultsPath $script:CompareStub

        $phases = Get-Content $env:STUB_LOG | ForEach-Object { ($_ -split '\|')[0] }
        $phases | Should -Be @('warmup', 'baseline', 'perfTest', 'compare')
    }

    It 'runs the baseline when the cached baseline folder does not exist (#1383)' {
        Invoke-PerfBaselineComparison -baselineSHA 'abc' -output $script:Output -filter "'*'" -useCachedBaseline $true `
            -RunPerfTestsPath $script:RunStub -ComparePerfResultsPath $script:CompareStub

        $phases = Get-Content $env:STUB_LOG | ForEach-Object { ($_ -split '\|')[0] }
        $phases | Should -Be @('warmup', 'baseline', 'perfTest', 'compare')
        Join-Path $script:CaseRoot 'Join-Path' | Should -Not -Exist
    }

    It 'stops when the comparison fails' {
        $env:STUB_COMPARE_FAIL = '1'

        { Invoke-PerfBaselineComparison -baselineSHA 'abc' -output $script:Output -filter "'*'" `
            -RunPerfTestsPath $script:RunStub -ComparePerfResultsPath $script:CompareStub } |
            Should -Throw '*Performance comparison failed with exit code 1*'
    }

    It 'passes CI mode to the comparison' {
        Invoke-PerfBaselineComparison -baselineSHA 'abc' -output $script:Output -filter "'*'" -ci $true `
            -RunPerfTestsPath $script:RunStub -ComparePerfResultsPath $script:CompareStub

        Get-Content $env:STUB_LOG | Select-Object -Last 1 | Should -Be 'compare|True'
    }

    It 'ignores a stale warm-up report from an earlier run (AC3)' {
        $stale = Join-Path $script:Output 'warmup'
        New-Item -ItemType Directory -Force -Path $stale | Out-Null
        New-Item -ItemType File -Force -Path (Join-Path $stale 'old-report-full-compressed.json') | Out-Null
        $env:STUB_NOREPORT_PHASE = 'warmup'

        { Invoke-PerfWarmup -RepoRoot $script:CaseRoot -Output $script:Output -RunPerfTestsPath $script:RunStub } |
            Should -Throw '*Warm-up perf run produced no benchmark report*'
    }

    Context 'Confirmation rerun' {
        BeforeEach {
            $script:IdA = 'Ns.Bench.Alpha.Run(FileCount: 1)'
            $script:IdB = 'Ns.Bench.Beta.Run(FileCount: 1000)'
            $script:Round1Ids = Join-Path $script:Output 'regressed-ids.json'
            $env:STUB_REGRESSED_IDS = (ConvertTo-Json -InputObject @($script:IdA, $script:IdB) -Compress)
        }

        It 'does not rerun when the comparison passes' {
            Invoke-PerfBaselineComparison -baselineSHA 'abc' -output $script:Output -filter "'*'" `
                -RunPerfTestsPath $script:RunStub -ComparePerfResultsPath $script:CompareStub

            $phases = Get-Content $env:STUB_LOG | ForEach-Object { ($_ -split '\|')[0] }
            $phases | Should -Be @('warmup', 'baseline', 'perfTest', 'compare')
            Join-Path $script:Output 'confirm' | Should -Not -Exist
        }

        It 'does not rerun when the comparison fails with exit code <ExitCode> instead of a regression' -ForEach @(
            @{ ExitCode = 1 }
            @{ ExitCode = 2 }
            @{ ExitCode = 4 }
        ) {
            $env:STUB_COMPARE_EXIT = "$ExitCode"

            { Invoke-PerfBaselineComparison -baselineSHA 'abc' -output $script:Output -filter "'*'" `
                -RunPerfTestsPath $script:RunStub -ComparePerfResultsPath $script:CompareStub } |
                Should -Throw "*Performance comparison failed with exit code $ExitCode*"

            $phases = Get-Content $env:STUB_LOG | ForEach-Object { ($_ -split '\|')[0] }
            $phases | Should -Be @('warmup', 'baseline', 'perfTest', 'compare')
        }

        It 'asks the comparison for the flagged ids in the output folder' {
            Invoke-PerfBaselineComparison -baselineSHA 'abc' -output $script:Output -filter "'*'" `
                -RunPerfTestsPath $script:RunStub -ComparePerfResultsPath $script:CompareStub

            $script:Round1Ids | Should -Exist
        }

        It 'reruns current before baseline when the comparison reports a regression' {
            $env:STUB_COMPARE_EXIT = '3'

            Invoke-PerfBaselineComparison -baselineSHA 'abc' -output $script:Output -filter "'*'" `
                -RunPerfTestsPath $script:RunStub -ComparePerfResultsPath $script:CompareStub

            $phases = Get-Content $env:STUB_LOG | ForEach-Object { ($_ -split '\|')[0] }
            $phases | Should -Be @('warmup', 'baseline', 'perfTest', 'compare', 'confirm-perfTest', 'confirm-baseline', 'confirm-compare')
        }

        It 'filters the rerun to exactly the flagged benchmarks in both phases' {
            $env:STUB_COMPARE_EXIT = '3'

            Invoke-PerfBaselineComparison -baselineSHA 'abc' -output $script:Output -filter "'*'" `
                -RunPerfTestsPath $script:RunStub -ComparePerfResultsPath $script:CompareStub

            $confirmCalls = @(Get-Content $env:STUB_LOG | Where-Object { $_ -like 'confirm-perfTest*' -or $_ -like 'confirm-baseline*' } | ForEach-Object { , ($_ -split '\|') })
            $confirmCalls.Count | Should -Be 2
            foreach ($call in $confirmCalls) {
                $call[3] | Should -Be "$($script:IdA),$($script:IdB)"
            }
        }

        It 'runs current from the repo root and baseline from the baseline worktree' {
            $env:STUB_COMPARE_EXIT = '3'

            Invoke-PerfBaselineComparison -baselineSHA 'abc' -output $script:Output -filter "'*'" `
                -RunPerfTestsPath $script:RunStub -ComparePerfResultsPath $script:CompareStub

            $byPhase = @{}
            Get-Content $env:STUB_LOG | ForEach-Object { $f = $_ -split '\|'; $byPhase[$f[0]] = $f }
            $byPhase['confirm-perfTest'][1] | Should -Be $script:CaseRoot
            $byPhase['confirm-baseline'][1] | Should -Be (Join-Path (Join-Path $script:CaseRoot 'artifacts') 'perfBaseline')
        }

        It 'reuses the baseline worktree that round 1 created' {
            $env:STUB_COMPARE_EXIT = '3'

            Invoke-PerfBaselineComparison -baselineSHA 'abc' -output $script:Output -filter "'*'" `
                -RunPerfTestsPath $script:RunStub -ComparePerfResultsPath $script:CompareStub

            Should -Invoke git -ModuleName PerfBaselineManager -Times 1 -Exactly
        }

        It 'creates the baseline worktree when round 1 used a cached baseline' {
            $env:STUB_COMPARE_EXIT = '3'
            $cached = Join-Path $script:Output 'baseline'
            New-Item -ItemType Directory -Force -Path $cached | Out-Null
            New-Item -ItemType File -Force -Path (Join-Path $cached 'cached-report-full-compressed.json') | Out-Null

            Invoke-PerfBaselineComparison -baselineSHA 'abc' -output $script:Output -filter "'*'" -useCachedBaseline $true `
                -RunPerfTestsPath $script:RunStub -ComparePerfResultsPath $script:CompareStub

            Should -Invoke git -ModuleName PerfBaselineManager -Times 1 -Exactly -ParameterFilter { $args[0] -eq 'worktree' -and $args[1] -eq 'add' -and $args[3] -eq 'abc' }
            $baselineCall = Get-Content $env:STUB_LOG | Where-Object { $_ -like 'confirm-baseline*' }
            ($baselineCall -split '\|')[1] | Should -Be (Join-Path (Join-Path $script:CaseRoot 'artifacts') 'perfBaseline')
        }

        It 'stops when the git worktree for the confirmation baseline cannot be created' {
            $env:STUB_COMPARE_EXIT = '3'
            $cached = Join-Path $script:Output 'baseline'
            New-Item -ItemType Directory -Force -Path $cached | Out-Null
            New-Item -ItemType File -Force -Path (Join-Path $cached 'cached-report-full-compressed.json') | Out-Null
            Mock git { $global:LASTEXITCODE = 128 } -ModuleName PerfBaselineManager

            { Invoke-PerfBaselineComparison -baselineSHA 'abc' -output $script:Output -filter "'*'" -useCachedBaseline $true `
                -RunPerfTestsPath $script:RunStub -ComparePerfResultsPath $script:CompareStub } |
                Should -Throw '*git worktree add failed with exit code 128*'
        }

        It 'compares confirm baseline to confirm current with the round 1 ids and passes CI and ETL through' {
            $env:STUB_COMPARE_EXIT = '3'
            Mock Resolve-PerfEtl { $true } -ModuleName PerfBaselineManager

            Invoke-PerfBaselineComparison -baselineSHA 'abc' -output $script:Output -filter "'*'" -etl $true -ci $true `
                -RunPerfTestsPath $script:RunStub -ComparePerfResultsPath $script:CompareStub

            $compare = (Get-Content $env:STUB_LOG | Where-Object { $_ -like 'confirm-compare*' }) -split '\|'
            $compare[1] | Should -Be 'True'
            $compare[2] | Should -Be $script:Round1Ids
            $compare[3] | Should -Be (Join-Path (Join-Path $script:Output 'confirm') 'regressed-ids.json')
            $compare[4] | Should -Be 'baseline'
            $compare[5] | Should -Be 'perfTest'
            $runs = @(Get-Content $env:STUB_LOG | Where-Object { $_ -like 'confirm-perfTest*' -or $_ -like 'confirm-baseline*' } | ForEach-Object { , ($_ -split '\|') })
            foreach ($run in $runs) { $run[5] | Should -Be 'True' }
        }

        It 'runs the rerun on the latest major runtime' {
            $env:STUB_COMPARE_EXIT = '3'
            $env:STUB_ROLLFORWARD_LOG = Join-Path $script:CaseRoot 'rollforward.log'

            Invoke-PerfBaselineComparison -baselineSHA 'abc' -output $script:Output -filter "'*'" `
                -RunPerfTestsPath $script:RunStub -ComparePerfResultsPath $script:CompareStub

            Get-Content $env:STUB_ROLLFORWARD_LOG | Select-Object -Last 3 |
                Should -Be @('confirm-perfTest|LatestMajor', 'confirm-baseline|LatestMajor', 'compare|LatestMajor')
        }

        It 'fails when the rerun confirms the regression' {
            $env:STUB_COMPARE_EXIT = '3'
            $env:STUB_CONFIRM_EXIT = '3'
            $env:STUB_CONFIRMED_IDS = (ConvertTo-Json -InputObject @($script:IdA) -Compress)

            { Invoke-PerfBaselineComparison -baselineSHA 'abc' -output $script:Output -filter "'*'" `
                -RunPerfTestsPath $script:RunStub -ComparePerfResultsPath $script:CompareStub } |
                Should -Throw '*regression confirmed by the rerun*1 of 2 flagged*'
        }

        It 'passes when the rerun does not confirm the regression' {
            $env:STUB_COMPARE_EXIT = '3'
            $env:STUB_CONFIRM_EXIT = '0'

            { Invoke-PerfBaselineComparison -baselineSHA 'abc' -output $script:Output -filter "'*'" `
                -RunPerfTestsPath $script:RunStub -ComparePerfResultsPath $script:CompareStub } |
                Should -Not -Throw
        }

        It 'logs how many benchmarks round 1 flagged and the rerun confirmed' {
            $env:STUB_COMPARE_EXIT = '3'
            $env:STUB_CONFIRM_EXIT = '3'
            $env:STUB_CONFIRMED_IDS = (ConvertTo-Json -InputObject @($script:IdB) -Compress)

            $log = & {
                try {
                    Invoke-PerfBaselineComparison -baselineSHA 'abc' -output $script:Output -filter "'*'" `
                        -RunPerfTestsPath $script:RunStub -ComparePerfResultsPath $script:CompareStub
                } catch { $null = $_ }
            } 6>&1 | Out-String

            $log | Should -BeLike '*Round 1 flagged 2, confirmed 1.*'
        }

        It 'reports zero confirmed when the rerun writes no ids' {
            $env:STUB_COMPARE_EXIT = '3'

            $log = Invoke-PerfBaselineComparison -baselineSHA 'abc' -output $script:Output -filter "'*'" `
                -RunPerfTestsPath $script:RunStub -ComparePerfResultsPath $script:CompareStub 6>&1 | Out-String

            $log | Should -BeLike '*Round 1 flagged 2, confirmed 0.*'
            $log | Should -BeLike '*runner noise*'
        }

        It 'fails when the confirmation comparison itself errors with exit code <ExitCode>' -ForEach @(
            @{ ExitCode = 1 }
            @{ ExitCode = 2 }
        ) {
            $env:STUB_COMPARE_EXIT = '3'
            $env:STUB_CONFIRM_EXIT = "$ExitCode"

            { Invoke-PerfBaselineComparison -baselineSHA 'abc' -output $script:Output -filter "'*'" `
                -RunPerfTestsPath $script:RunStub -ComparePerfResultsPath $script:CompareStub } |
                Should -Throw "*confirmation comparison failed with exit code $ExitCode*"
        }

        It 'fails without a rerun when the regression exit code comes with no flagged ids' {
            $env:STUB_COMPARE_EXIT = '3'
            $env:STUB_REGRESSED_IDS = '[]'

            { Invoke-PerfBaselineComparison -baselineSHA 'abc' -output $script:Output -filter "'*'" `
                -RunPerfTestsPath $script:RunStub -ComparePerfResultsPath $script:CompareStub } |
                Should -Throw '*Performance comparison failed with exit code 3*nothing to rerun*'

            $phases = Get-Content $env:STUB_LOG | ForEach-Object { ($_ -split '\|')[0] }
            $phases | Should -Be @('warmup', 'baseline', 'perfTest', 'compare')
        }

        It 'fails without a rerun when the regression exit code comes with no ids file' {
            $env:STUB_COMPARE_EXIT = '3'
            $env:STUB_REGRESSED_IDS = ''

            { Invoke-PerfBaselineComparison -baselineSHA 'abc' -output $script:Output -filter "'*'" `
                -RunPerfTestsPath $script:RunStub -ComparePerfResultsPath $script:CompareStub } |
                Should -Throw '*Flagged benchmark ids file not found*'

            $phases = Get-Content $env:STUB_LOG | ForEach-Object { ($_ -split '\|')[0] }
            $phases | Should -Be @('warmup', 'baseline', 'perfTest', 'compare')
        }

        It 'ignores a stale ids file left by an earlier run' {
            $env:STUB_COMPARE_EXIT = '3'
            $env:STUB_REGRESSED_IDS = ''
            Set-Content -Path $script:Round1Ids -Value '["stale.id"]'

            { Invoke-PerfBaselineComparison -baselineSHA 'abc' -output $script:Output -filter "'*'" `
                -RunPerfTestsPath $script:RunStub -ComparePerfResultsPath $script:CompareStub } |
                Should -Throw '*Flagged benchmark ids file not found*'
        }

        It 'stops before the baseline rerun when the current rerun <Case>' -ForEach @(
            @{ Case = 'fails'; Fail = 'confirm-perfTest'; NoReport = ''; Message = '*Confirmation run of the current branch failed with exit code 1*' }
            @{ Case = 'writes no report'; Fail = ''; NoReport = 'confirm-perfTest'; Message = '*Confirmation run of the current branch produced no benchmark report*' }
        ) {
            $env:STUB_COMPARE_EXIT = '3'
            $env:STUB_FAIL_PHASE = $Fail
            $env:STUB_NOREPORT_PHASE = $NoReport

            { Invoke-PerfBaselineComparison -baselineSHA 'abc' -output $script:Output -filter "'*'" `
                -RunPerfTestsPath $script:RunStub -ComparePerfResultsPath $script:CompareStub } |
                Should -Throw $Message

            $phases = Get-Content $env:STUB_LOG | ForEach-Object { ($_ -split '\|')[0] }
            $phases | Should -Be @('warmup', 'baseline', 'perfTest', 'compare', 'confirm-perfTest')
        }

        It 'stops before the confirmation comparison when the baseline rerun <Case>' -ForEach @(
            @{ Case = 'fails'; Fail = 'confirm-baseline'; NoReport = ''; Message = '*Confirmation run of the baseline failed with exit code 1*' }
            @{ Case = 'writes no report'; Fail = ''; NoReport = 'confirm-baseline'; Message = '*Confirmation run of the baseline produced no benchmark report*' }
        ) {
            $env:STUB_COMPARE_EXIT = '3'
            $env:STUB_FAIL_PHASE = $Fail
            $env:STUB_NOREPORT_PHASE = $NoReport

            { Invoke-PerfBaselineComparison -baselineSHA 'abc' -output $script:Output -filter "'*'" `
                -RunPerfTestsPath $script:RunStub -ComparePerfResultsPath $script:CompareStub } |
                Should -Throw $Message

            $phases = Get-Content $env:STUB_LOG | ForEach-Object { ($_ -split '\|')[0] }
            $phases | Should -Be @('warmup', 'baseline', 'perfTest', 'compare', 'confirm-perfTest', 'confirm-baseline')
        }

        It 'clears results from an earlier confirmation before rerunning' {
            $env:STUB_COMPARE_EXIT = '3'
            $stale = Join-Path (Join-Path $script:Output 'confirm') 'baseline'
            New-Item -ItemType Directory -Force -Path $stale | Out-Null
            New-Item -ItemType File -Force -Path (Join-Path $stale 'old-report-full-compressed.json') | Out-Null

            Invoke-PerfBaselineComparison -baselineSHA 'abc' -output $script:Output -filter "'*'" `
                -RunPerfTestsPath $script:RunStub -ComparePerfResultsPath $script:CompareStub

            Join-Path $stale 'old-report-full-compressed.json' | Should -Not -Exist
        }

        It 'restores DOTNET_ROLL_FORWARD when the rerun confirms the regression' {
            $env:DOTNET_ROLL_FORWARD = 'Disable'
            $env:STUB_COMPARE_EXIT = '3'
            $env:STUB_CONFIRM_EXIT = '3'

            { Invoke-PerfBaselineComparison -baselineSHA 'abc' -output $script:Output -filter "'*'" `
                -RunPerfTestsPath $script:RunStub -ComparePerfResultsPath $script:CompareStub } | Should -Throw

            $env:DOTNET_ROLL_FORWARD | Should -Be 'Disable'
        }

        It 'uses the same regression exit code as PerfDiff' {
            $source = Get-Content -Raw (Join-Path $script:RepoRoot 'src/tools/PerfDiff/Program.cs')
            $csharpCode = [int][regex]::Match($source, 'RegressionExitCode\s*=\s*(\d+)').Groups[1].Value
            $moduleCode = & (Get-Module PerfBaselineManager) { $script:PerfRegressionExitCode }

            $csharpCode | Should -BeGreaterThan 0
            $moduleCode | Should -Be $csharpCode
        }
    }
}

Describe 'ConvertTo-PerfBenchmarkFilter' {
    It 'keeps a benchmark id with parameters as a single exact glob' {
        ConvertTo-PerfBenchmarkFilter -Ids 'Ns.Bench.Alpha.Run(FileCount: 1000, MoqKey: x)' |
            Should -Be 'Ns.Bench.Alpha.Run(FileCount: 1000, MoqKey: x)'
    }

    It 'replaces glob wildcards and double quotes with a single-character wildcard' {
        ConvertTo-PerfBenchmarkFilter -Ids 'Ns.Run(a: *, b: ?, c: "x")' | Should -Be 'Ns.Run(a: ?, b: ?, c: ?x?)'
    }

    It 'returns one filter per distinct id and keeps their order' {
        $filters = ConvertTo-PerfBenchmarkFilter -Ids @('B.Run', 'A.Run', 'B.Run')

        $filters | Should -Be @('B.Run', 'A.Run')
    }

    It 'returns one filter for a single id' {
        $filters = @(ConvertTo-PerfBenchmarkFilter -Ids 'A.Run')

        $filters.Count | Should -Be 1
        $filters[0] | Should -Be 'A.Run'
    }

    It 'returns nothing for no ids' {
        @(ConvertTo-PerfBenchmarkFilter -Ids @()).Count | Should -Be 0
    }
}

Describe 'Read-PerfRegressedIds' {
    BeforeEach {
        $script:IdsFile = Join-Path $script:ScratchRoot "ids-$([guid]::NewGuid()).json"
    }

    It 'reads a JSON array of ids' {
        Set-Content -Path $script:IdsFile -Value '["a","b"]'

        Read-PerfRegressedIds -Path $script:IdsFile | Should -Be @('a', 'b')
    }

    It 'reads a single id' {
        Set-Content -Path $script:IdsFile -Value '["only"]'

        $ids = @(Read-PerfRegressedIds -Path $script:IdsFile)

        $ids.Count | Should -Be 1
        $ids[0] | Should -Be 'only'
    }

    It 'returns an empty array for []' {
        Set-Content -Path $script:IdsFile -Value '[]'

        @(Read-PerfRegressedIds -Path $script:IdsFile).Count | Should -Be 0
    }

    It 'throws when the file is missing' {
        { Read-PerfRegressedIds -Path (Join-Path $script:ScratchRoot 'no-such-ids.json') } | Should -Throw '*not found*'
    }

    It 'throws when the content is <Case>' -ForEach @(
        @{ Case = 'empty'; Content = '' }
        @{ Case = 'not JSON'; Content = 'not json' }
        @{ Case = 'an object'; Content = '{"a":1}' }
        @{ Case = 'null'; Content = 'null' }
        @{ Case = 'a bare string'; Content = '"a"' }
        @{ Case = 'an array of numbers'; Content = '[1,2]' }
        @{ Case = 'an array with an empty string'; Content = '["a",""]' }
        @{ Case = 'an array with null'; Content = '["a",null]' }
    ) {
        Set-Content -Path $script:IdsFile -Value $Content

        { Read-PerfRegressedIds -Path $script:IdsFile } | Should -Throw
    }
}

Describe 'DiffPerfToBaseline.ps1 exit code (#1431)' {
    It 'exits <ExitCode> when the comparison <Case>' -ForEach @(
        @{ Case = 'throws'; Body = "throw 'stub comparison failure'"; ExitCode = 1 }
        @{ Case = 'succeeds'; Body = "Write-Output 'stub comparison done'"; ExitCode = 0 }
    ) {
        # Run a copy of the real entry point next to a stub module in a child process.
        # The child's exit code is what CI sees.
        $entryRoot = Join-Path $script:ScratchRoot "entry-$([guid]::NewGuid())"
        New-Item -ItemType Directory -Force -Path $entryRoot | Out-Null
        Copy-Item -Path (Join-Path $script:PerfRoot 'DiffPerfToBaseline.ps1') -Destination $entryRoot
        Set-Content -Path (Join-Path $entryRoot 'PerfBaselineManager.psm1') -Value @"
function Invoke-PerfBaselineComparison { $Body }
Export-ModuleMember -Function Invoke-PerfBaselineComparison
"@
        $pwsh = (Get-Process -Id $PID).Path

        $childOutput = & $pwsh -NoProfile -NonInteractive -File (Join-Path $entryRoot 'DiffPerfToBaseline.ps1') -baselineSHA 'abc' 2>&1

        $LASTEXITCODE | Should -Be $ExitCode
        ($childOutput | Out-String) | Should -BeLike "*stub comparison*"
    }
}
