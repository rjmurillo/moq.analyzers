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

    It 'passes every filter pattern after one --filter flag' {
        $arguments = New-PerfBenchmarkArguments -ProjectFullPath '/repo/tests/a.csproj' -Output '/repo/out' -Filter @('A.B(FileCount: 1000)', 'C.D')
        $arguments | Select-Object -Last 3 | Should -Be @('--filter', 'A.B(FileCount: 1000)', 'C.D')
        Format-PerfCommandArguments -Arguments ($arguments | Select-Object -Last 3) | Should -Be '--filter "A.B(FileCount: 1000)" C.D'
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

Describe 'Invoke-PerfTests build step' {
    BeforeEach {
        $script:DotnetCalls = [System.Collections.Generic.List[string]]::new()
        $calls = $script:DotnetCalls
        Mock dotnet { $calls.Add($args[0]); $global:LASTEXITCODE = 0 } -ModuleName PerfTestRunner
        $script:Root = Join-Path ([System.IO.Path]::GetTempPath()) "perf-runner-$([guid]::NewGuid())"
        New-Item -ItemType Directory -Force -Path $script:Root | Out-Null
    }

    AfterEach {
        Remove-Item -Path $script:Root -Recurse -Force -ErrorAction SilentlyContinue
    }

    # The benchmark process itself starts through ProcessStartInfo and fails on the
    # missing project. These tests check only the restore and build calls before it.
    It 'restores and builds the project by default' {
        { Invoke-PerfTests -projects 'missing.csproj' -filter 'x' -perftestRootFolder $script:Root -output (Join-Path $script:Root 'out') } |
            Should -Throw '*One or more benchmark projects failed*'

        $script:DotnetCalls | Should -Be @('restore', 'build')
    }

    It 'skips restore and build when skipBuild is set' {
        { Invoke-PerfTests -projects 'missing.csproj' -filter 'x' -perftestRootFolder $script:Root -output (Join-Path $script:Root 'out') -skipBuild $true } |
            Should -Throw '*One or more benchmark projects failed*'

        $script:DotnetCalls | Should -BeNullOrEmpty
    }
}

Describe 'RunPerfTests.ps1 exit code' {
    It 'resets a stale LASTEXITCODE when the run succeeds' {
        # Run a copy of the real entry point next to a stub module.
        $entryRoot = Join-Path ([System.IO.Path]::GetTempPath()) "run-entry-$([guid]::NewGuid())"
        New-Item -ItemType Directory -Force -Path $entryRoot | Out-Null
        try {
            Copy-Item -Path (Join-Path $script:PerfRoot 'RunPerfTests.ps1') -Destination $entryRoot
            Set-Content -Path (Join-Path $entryRoot 'PerfTestRunner.psm1') -Value @"
function Invoke-PerfTests { param([string] `$projects, [string[]] `$filter, [string] `$perftestRootFolder, [string] `$output, [bool] `$etl, [bool] `$ci, [bool] `$skipBuild) }
Export-ModuleMember -Function Invoke-PerfTests
"@
            $global:LASTEXITCODE = 3

            & (Join-Path $entryRoot 'RunPerfTests.ps1') -skipBuild $true

            $LASTEXITCODE | Should -Be 0
        }
        finally {
            Remove-Item -Path $entryRoot -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}
