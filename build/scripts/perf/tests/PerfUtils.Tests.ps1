BeforeAll {
    $script:PerfRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
    $script:RepoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')
    $script:ScratchRoot = Join-Path $script:RepoRoot "artifacts\pester\perf\PerfUtils-$([guid]::NewGuid())"
    New-Item -ItemType Directory -Force -Path $script:ScratchRoot | Out-Null
    Import-Module (Join-Path $script:PerfRoot 'PerfUtils.psm1') -Force -DisableNameChecking
}

Describe 'Get-RepoRoot' {
    It 'resolves the repository root from the perf script folder' {
        (Get-RepoRoot -ScriptRoot $script:PerfRoot).Path | Should -Be $script:RepoRoot.Path
    }

    It 'reports an error for a folder that cannot resolve to an existing root' {
        $missing = Join-Path $script:ScratchRoot 'missing\child\leaf'
        $errors = & { Get-RepoRoot -ScriptRoot $missing } 2>&1
        $errors | Should -Not -BeNullOrEmpty
    }

    It 'handles a script root with trailing separator' {
        $scriptRoot = "$script:PerfRoot$([IO.Path]::DirectorySeparatorChar)"
        (Get-RepoRoot -ScriptRoot $scriptRoot).Path | Should -Be $script:RepoRoot.Path
    }
}

Describe 'Show-Invocation' {
    It 'writes a switch argument when the value is true' {
        $output = & { Show-Invocation -ScriptPath 'script.ps1' -Arguments @{ ci = $true } } 6>&1
        $output.ToString() | Should -Be 'Invoking: script.ps1 -ci'
    }

    It 'skips null arguments' {
        $output = & { Show-Invocation -ScriptPath 'script.ps1' -Arguments @{ filter = $null } } 6>&1
        $output.ToString() | Should -Be 'Invoking: script.ps1'
    }

    It 'quotes string arguments that contain whitespace' {
        $output = & { Show-Invocation -ScriptPath 'script.ps1' -Arguments @{ filter = 'File Count' } } 6>&1
        $output.ToString() | Should -Be 'Invoking: script.ps1 -filter "File Count"'
    }
}

Describe 'Ensure-Folder' {
    It 'creates a missing folder' {
        $path = Join-Path $script:ScratchRoot 'created'
        Ensure-Folder $path | Out-Null
        Test-Path $path | Should -BeTrue
    }

    It 'leaves an existing folder in place' {
        $path = Join-Path $script:ScratchRoot 'existing'
        New-Item -ItemType Directory -Force -Path $path | Out-Null
        Ensure-Folder $path | Out-Null
        Test-Path $path | Should -BeTrue
    }

    It 'preserves the original permissive handling of extra positional arguments' {
        $path = Join-Path $script:ScratchRoot 'extra'
        Ensure-Folder $path 'ignored' | Out-Null
        Test-Path $path | Should -BeTrue
    }
}

Describe 'Write-PerfLog' {
    It 'writes host messages' {
        $output = & { Write-PerfLog -Message 'hello' } 6>&1
        $output.ToString() | Should -Be 'hello'
    }

    It 'writes warnings when requested' {
        $output = & { Write-PerfLog -Message 'careful' -Level Warning } 3>&1
        $output.ToString() | Should -Match 'careful'
    }

    It 'rejects unsupported log levels' {
        { Write-PerfLog -Message 'bad' -Level Invalid } | Should -Throw
    }
}

Describe 'Invoke-PerfCommand' {
    It 'does not throw when the command sets a zero exit code' {
        { Invoke-PerfCommand -Command { $global:LASTEXITCODE = 0 } -FailureMessage 'failed' } | Should -Not -Throw
    }

    It 'throws with the native exit code when the command fails' {
        { Invoke-PerfCommand -Command { $global:LASTEXITCODE = 42 } -FailureMessage 'failed' } | Should -Throw '*failed with exit code 42*'
    }

    It 'requires a command script block' {
        { Invoke-PerfCommand -Command $null -FailureMessage 'failed' } | Should -Throw
    }
}
