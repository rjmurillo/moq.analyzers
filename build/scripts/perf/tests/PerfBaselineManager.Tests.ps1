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
        $script:RunStub = Join-Path $script:ScratchRoot 'RunPerfTestsStub.ps1'
        Set-Content -Path $script:RunStub -Value @'
param(
    [string] $projects,
    [string] $filter,
    [string] $perftestRootFolder,
    [string] $output,
    [bool] $etl = $false,
    [bool] $ci = $false
)
$phase = Split-Path $output -Leaf
Add-Content -Path $env:STUB_LOG -Value "$phase|$perftestRootFolder|$projects|$filter|$etl|$ci"
if ($env:STUB_ROLLFORWARD_LOG) { Add-Content -Path $env:STUB_ROLLFORWARD_LOG -Value "$phase|$env:DOTNET_ROLL_FORWARD" }
$skipReport = ($env:STUB_NOREPORT_PHASE -eq $phase) -or ($env:STUB_STARONLY_PHASE -eq $phase -and $filter -ne '*')
New-Item -ItemType Directory -Force -Path $output | Out-Null
if (-not $skipReport) {
    New-Item -ItemType File -Force -Path (Join-Path $output "$phase-report-full-compressed.json") | Out-Null
}
# A trailing '*' (for example 'baseline*') fails only that phase's run with filter '*'.
$failOnStar = $filter -eq '*' -and $env:STUB_FAIL_PHASE -eq "$phase*"
if ($env:STUB_FAIL_PHASE -eq $phase -or $failOnStar) { exit 1 }
exit 0
'@
        # STUB_COMPARE_FAIL makes the comparison exit 1.
        $script:CompareStub = Join-Path $script:ScratchRoot 'ComparePerfResultsStub.ps1'
        Set-Content -Path $script:CompareStub -Value @'
param([string] $baseline, [string] $results, [switch] $ci)
Add-Content -Path $env:STUB_LOG -Value "compare|$ci"
if ($env:STUB_ROLLFORWARD_LOG) { Add-Content -Path $env:STUB_ROLLFORWARD_LOG -Value "compare|$env:DOTNET_ROLL_FORWARD" }
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
        $env:STUB_COMPARE_FAIL = ''
        $env:STUB_ROLLFORWARD_LOG = ''
        $script:SavedRollForward = $env:DOTNET_ROLL_FORWARD
        $caseRoot = $script:CaseRoot
        Mock Get-RepoRoot { $caseRoot } -ModuleName PerfBaselineManager
        Mock git { $global:LASTEXITCODE = 0 } -ModuleName PerfBaselineManager
        # Ensure-Folder Join-Path (issue #1383) creates a stray 'Join-Path' folder in
        # the current directory. Run from the case folder so it is cleaned up with it.
        Push-Location $script:CaseRoot
    }

    AfterEach {
        Pop-Location
        Remove-Item Env:STUB_LOG, Env:STUB_FAIL_PHASE, Env:STUB_NOREPORT_PHASE, Env:STUB_STARONLY_PHASE, Env:STUB_COMPARE_FAIL, Env:STUB_ROLLFORWARD_LOG -ErrorAction SilentlyContinue
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
}
