BeforeAll {
    $script:PerfRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
    $script:RepoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')
    Import-Module (Join-Path $script:PerfRoot 'PerfConfig.psm1') -Force -DisableNameChecking
}

Describe 'Normalize-PerfProjects' {
    It 'uses the benchmark project when no projects are supplied' {
        Normalize-PerfProjects -Projects $null | Should -Be 'tests\Moq.Analyzers.Benchmarks\Moq.Analyzers.Benchmarks.csproj'
    }

    It 'preserves a caller supplied project list' {
        Normalize-PerfProjects -Projects 'a.csproj;b.csproj' | Should -Be 'a.csproj;b.csproj'
    }

    It 'uses the benchmark project for whitespace input' {
        Normalize-PerfProjects -Projects '   ' | Should -Be 'tests\Moq.Analyzers.Benchmarks\Moq.Analyzers.Benchmarks.csproj'
    }
}

Describe 'Normalize-PerfFilter' {
    It 'uses the quoted wildcard when no filter is supplied' {
        Normalize-PerfFilter -Filter $null | Should -Be "'*'"
    }

    It 'preserves a caller supplied filter' {
        Normalize-PerfFilter -Filter '*(FileCount: 1)' | Should -Be '*(FileCount: 1)'
    }

    It 'uses the quoted wildcard for whitespace input' {
        Normalize-PerfFilter -Filter "`t" | Should -Be "'*'"
    }
}

Describe 'Test-PerfWindowsPlatform' {
    It 'treats Windows PowerShell as Windows' {
        Test-PerfWindowsPlatform -PowerShellMajorVersion 5 -IsWindowsPlatform $false | Should -BeTrue
    }

    It 'uses the platform flag on PowerShell 7' {
        Test-PerfWindowsPlatform -PowerShellMajorVersion 7 -IsWindowsPlatform $true | Should -BeTrue
    }

    It 'returns false for non-Windows PowerShell 7' {
        Test-PerfWindowsPlatform -PowerShellMajorVersion 7 -IsWindowsPlatform $false | Should -BeFalse
    }
}

Describe 'Resolve-PerfEtl' {
    It 'preserves disabled ETL' {
        Resolve-PerfEtl -Etl $false -IsWindowsPlatform $false | Should -BeFalse
    }

    It 'preserves ETL on Windows' {
        Resolve-PerfEtl -Etl $true -IsWindowsPlatform $true | Should -BeTrue
    }

    It 'disables ETL on non-Windows platforms' {
        Resolve-PerfEtl -Etl $true -IsWindowsPlatform $false | Should -BeFalse
    }
}

Describe 'Perf paths and environment' {
    It 'builds the existing performance output path' {
        Get-PerfOutputRoot -RepoRoot $script:RepoRoot | Should -Be (Join-Path $script:RepoRoot 'artifacts\performance\perfResults')
    }

    It 'uses the original case-insensitive force baseline comparison' {
        $oldValue = $env:FORCE_PERF_BASELINE
        try {
            $env:FORCE_PERF_BASELINE = 'true'
            Test-ForcePerfBaseline | Should -BeTrue
            $env:FORCE_PERF_BASELINE = 'TRUE'
            Test-ForcePerfBaseline | Should -BeTrue
        }
        finally {
            $env:FORCE_PERF_BASELINE = $oldValue
        }
    }

    It 'resolves the checked-in baseline JSON path' {
        (Get-PerfBaselineJsonPath -RepoRoot $script:RepoRoot).Path | Should -Be (Join-Path $script:RepoRoot 'build\perf\baseline.json')
    }
}
