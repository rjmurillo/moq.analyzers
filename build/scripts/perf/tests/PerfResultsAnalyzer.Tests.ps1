BeforeAll {
    $script:PerfRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
    $script:RepoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')
    $script:ScratchRoot = Join-Path $script:RepoRoot "artifacts\pester\perf\PerfResults-$([guid]::NewGuid())"
    New-Item -ItemType Directory -Force -Path $script:ScratchRoot | Out-Null
    Import-Module (Join-Path $script:PerfRoot 'PerfResultsAnalyzer.psm1') -Force -DisableNameChecking
}

Describe 'Resolve-PerfResultsFolders' {
    It 'resolves baseline and current result folders' {
        $baseline = Join-Path $script:ScratchRoot 'baseline'
        $results = Join-Path $script:ScratchRoot 'perfTest'
        New-Item -ItemType Directory -Force -Path (Join-Path $baseline 'results') | Out-Null
        New-Item -ItemType Directory -Force -Path (Join-Path $results 'results') | Out-Null

        $folders = Resolve-PerfResultsFolders -Baseline $baseline -Results $results

        $folders.baselineFolder.Path | Should -Be (Join-Path $baseline 'results')
        $folders.resultsFolder.Path | Should -Be (Join-Path $results 'results')
    }

    It 'reports errors when result folders are missing' {
        $errors = & { Resolve-PerfResultsFolders -Baseline (Join-Path $script:ScratchRoot 'missingA') -Results (Join-Path $script:ScratchRoot 'missingB') } 2>&1
        $errors | Should -Not -BeNullOrEmpty
    }

    It 'handles folders that contain spaces' {
        $baseline = Join-Path $script:ScratchRoot 'baseline with spaces'
        $results = Join-Path $script:ScratchRoot 'perf with spaces'
        New-Item -ItemType Directory -Force -Path (Join-Path $baseline 'results') | Out-Null
        New-Item -ItemType Directory -Force -Path (Join-Path $results 'results') | Out-Null

        $folders = Resolve-PerfResultsFolders -Baseline $baseline -Results $results

        $folders.baselineFolder.Path | Should -Be (Join-Path $baseline 'results')
        $folders.resultsFolder.Path | Should -Be (Join-Path $results 'results')
    }
}

Describe 'Get-PerfDiffProjectPath' {
    It 'builds the existing PerfDiff project path' {
        Get-PerfDiffProjectPath -RepoRoot $script:RepoRoot | Should -Be (Join-Path $script:RepoRoot 'src\tools\PerfDiff\PerfDiff.csproj')
    }

    It 'accepts repository roots that contain spaces' {
        Get-PerfDiffProjectPath -RepoRoot '/repo with spaces' | Should -Be (Join-Path '/repo with spaces' 'src\tools\PerfDiff\PerfDiff.csproj')
    }

    It 'requires a repository root' {
        { Get-PerfDiffProjectPath -RepoRoot '' } | Should -Throw
    }
}
