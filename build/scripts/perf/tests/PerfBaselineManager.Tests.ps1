BeforeAll {
    $script:PerfRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
    $script:RepoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')
    $script:ScratchRoot = Join-Path $script:RepoRoot "artifacts\pester\perf\PerfBaseline-$([guid]::NewGuid())"
    New-Item -ItemType Directory -Force -Path $script:ScratchRoot | Out-Null
    Import-Module (Join-Path $script:PerfRoot 'PerfBaselineManager.psm1') -Force -DisableNameChecking
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
