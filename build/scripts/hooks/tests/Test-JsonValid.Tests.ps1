BeforeAll {
    $script:HookScript = Join-Path -Path $PSScriptRoot -ChildPath '..' -AdditionalChildPath 'Test-JsonValid.ps1'

    function Invoke-JsonHook {
        param([string]$File)

        # Quote the path. On Linux and macOS, PowerShell expands wildcards
        # in an unquoted native argument, so [x].json could become x.json.
        $output = & pwsh -NoProfile -NonInteractive -File $script:HookScript "$File" 2>&1
        [pscustomobject]@{
            ExitCode = $LASTEXITCODE
            Output   = ($output | Out-String)
        }
    }
}

Describe 'Test-JsonValid' {
    BeforeEach {
        $script:WorkDir = Join-Path $TestDrive ([guid]::NewGuid())
        New-Item -ItemType Directory -Path $script:WorkDir | Out-Null
    }

    It 'fails invalid JSON in a plain file name' {
        $file = Join-Path $script:WorkDir 'bad.json'
        Set-Content -LiteralPath $file -Value '{ not json'

        $result = Invoke-JsonHook -File $file

        $result.ExitCode | Should -Be 1
        $result.Output | Should -Match 'FAIL: json'
    }

    It 'fails invalid JSON in a file name with brackets' {
        $file = Join-Path $script:WorkDir '[x].json'
        Set-Content -LiteralPath $file -Value '{ not json'

        $result = Invoke-JsonHook -File $file

        $result.ExitCode | Should -Be 1
        $result.Output | Should -Match ([regex]::Escape('[x].json'))
    }

    It 'passes valid JSON in a file name with brackets' {
        $file = Join-Path $script:WorkDir '[x].json'
        Set-Content -LiteralPath $file -Value '{ "a": 1 }'

        $result = Invoke-JsonHook -File $file

        $result.ExitCode | Should -Be 0
    }

    It 'checks the bracketed file, not a file its wildcard matches' {
        Set-Content -LiteralPath (Join-Path $script:WorkDir 'x.json') -Value '{ "a": 1 }'
        $file = Join-Path $script:WorkDir '[x].json'
        Set-Content -LiteralPath $file -Value '{ not json'

        $result = Invoke-JsonHook -File $file

        $result.ExitCode | Should -Be 1
    }

    It 'skips a file that does not exist' {
        $result = Invoke-JsonHook -File (Join-Path $script:WorkDir 'missing.json')

        $result.ExitCode | Should -Be 0
    }

    It 'skips verified snapshot files' {
        $file = Join-Path $script:WorkDir 'snap.verified.json'
        Set-Content -LiteralPath $file -Value '{ not json'

        $result = Invoke-JsonHook -File $file

        $result.ExitCode | Should -Be 0
    }
}
