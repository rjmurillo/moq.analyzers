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

    It 'throws when result folders are missing' {
        { Resolve-PerfResultsFolders -Baseline (Join-Path $script:ScratchRoot 'missingA') -Results (Join-Path $script:ScratchRoot 'missingB') } | Should -Throw '*not found*'
    }

    It 'throws when only the results folder is missing' {
        $baseline = Join-Path $script:ScratchRoot 'oneMissingBaseline'
        New-Item -ItemType Directory -Force -Path (Join-Path $baseline 'results') | Out-Null

        { Resolve-PerfResultsFolders -Baseline $baseline -Results (Join-Path $script:ScratchRoot 'stillMissing') } | Should -Throw '*not found*'
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

Describe 'Invoke-PerfResultsComparison exit-code contract' {
    BeforeEach {
        $script:comparisonBaseline = Join-Path $script:ScratchRoot "comparisonBaseline-$([guid]::NewGuid())"
        $script:comparisonResults = Join-Path $script:ScratchRoot "comparisonResults-$([guid]::NewGuid())"
        New-Item -ItemType Directory -Force -Path (Join-Path $script:comparisonBaseline 'results') | Out-Null
        New-Item -ItemType Directory -Force -Path (Join-Path $script:comparisonResults 'results') | Out-Null
    }

    It 'returns a scalar zero exit code when PerfDiff succeeds' {
        Mock dotnet { $global:LASTEXITCODE = 0 } -ModuleName PerfResultsAnalyzer

        $result = Invoke-PerfResultsComparison -baseline $script:comparisonBaseline -results $script:comparisonResults

        $result | Should -BeOfType [int]
        $result | Should -Be 0
        @($result).Count | Should -Be 1
    }

    It 'returns a scalar non-zero exit code when PerfDiff reports a regression' {
        $script:dotnetCallCount = 0
        Mock dotnet {
            $script:dotnetCallCount++
            if ($script:dotnetCallCount -eq 1) {
                $global:LASTEXITCODE = 0
            } else {
                $global:LASTEXITCODE = 1
            }
        } -ModuleName PerfResultsAnalyzer

        $result = Invoke-PerfResultsComparison -baseline $script:comparisonBaseline -results $script:comparisonResults

        $result | Should -BeOfType [int]
        $result | Should -Be 1
        @($result).Count | Should -Be 1
    }

    It 'throws when the PerfDiff build fails' {
        Mock dotnet { $global:LASTEXITCODE = 2 } -ModuleName PerfResultsAnalyzer

        { Invoke-PerfResultsComparison -baseline $script:comparisonBaseline -results $script:comparisonResults } | Should -Throw '*dotnet build failed*'
    }
}
