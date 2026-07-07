BeforeAll {
    $script:PerfRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
    Import-Module (Join-Path $script:PerfRoot 'PerfTestRunner.psm1') -Force -DisableNameChecking
}

Describe 'Split-PerfProjects' {
    It 'splits semicolon-delimited project paths' {
        Split-PerfProjects -Projects 'a.csproj;b.csproj' | Should -Be @('a.csproj', 'b.csproj')
    }

    It 'preserves a single project path' {
        Split-PerfProjects -Projects 'a.csproj' | Should -Be 'a.csproj'
    }

    It 'preserves trailing empty entries from the original split behavior' {
        Split-PerfProjects -Projects 'a.csproj;' | Should -Be @('a.csproj', '')
    }
}

Describe 'Get-PerfProjectFullPath' {
    It 'combines the test root and project path' {
        Get-PerfProjectFullPath -PerfTestRootFolder '/repo' -Project 'tests/a.csproj' | Should -Be (Join-Path '/repo' 'tests/a.csproj')
    }

    It 'accepts project paths that contain spaces' {
        Get-PerfProjectFullPath -PerfTestRootFolder '/repo' -Project 'tests/a project.csproj' | Should -Be (Join-Path '/repo' 'tests/a project.csproj')
    }

    It 'requires a root folder' {
        { Get-PerfProjectFullPath -PerfTestRootFolder '' -Project 'tests/a.csproj' } | Should -Throw
    }
}

Describe 'New-PerfBenchmarkArguments' {
    It 'creates the baseline BenchmarkDotNet argument list' {
        $args = New-PerfBenchmarkArguments -ProjectFullPath '/repo/tests/a.csproj' -Output '/repo/out' -Filter "'*'"
        $args | Should -Be @('run', '-c', 'Release', '--no-build', '--project', '/repo/tests/a.csproj', '--', '--outliers', 'DontRemove', '--memory', '--threading', '--exceptions', '--exporters', 'JSON', '--artifacts', '/repo/out', '--filter', "'*'")
    }

    It 'adds CI arguments before the filter' {
        $args = New-PerfBenchmarkArguments -ProjectFullPath '/repo/tests/a.csproj' -Output '/repo/out' -Filter 'abc' -Ci $true
        $args | Should -Contain '--stopOnFirstError'
        $args | Should -Contain '--keepFiles'
    }

    It 'adds the ETW profiler before the filter' {
        $args = New-PerfBenchmarkArguments -ProjectFullPath '/repo/tests/a.csproj' -Output '/repo/out' -Filter 'abc' -Etl $true
        $args | Should -Contain '--profiler'
        $args | Should -Contain 'ETW'
    }
}

Describe 'Format-PerfCommandArguments' {
    It 'joins arguments with spaces' {
        Format-PerfCommandArguments -Arguments @('run', '-c', 'Release') | Should -Be 'run -c Release'
    }

    It 'quotes arguments with whitespace' {
        Format-PerfCommandArguments -Arguments @('run', '--project', '/repo/a project.csproj') | Should -Be 'run --project "/repo/a project.csproj"'
    }

    It 'preserves wildcard arguments without adding quotes' {
        Format-PerfCommandArguments -Arguments @('--filter', '*') | Should -Be '--filter *'
    }
}
