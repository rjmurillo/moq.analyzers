BeforeAll {
    $script:HookScript = Join-Path -Path $PSScriptRoot -ChildPath '..' -AdditionalChildPath 'Test-LargeFiles.ps1'

    function Initialize-TempRepo {
        $repo = Join-Path $TestDrive ([guid]::NewGuid())
        New-Item -ItemType Directory -Path $repo | Out-Null
        git -C $repo init --quiet
        $repo
    }

    function Add-StagedFile {
        param(
            [string]$Repo,
            [string]$Name,
            [string]$Content
        )

        Set-Content -LiteralPath (Join-Path $Repo $Name) -Value $Content -NoNewline
        # -A stages every file. A pathspec would treat the brackets as a glob.
        git -C $Repo add -A
    }

    function Invoke-LargeFileHook {
        param([string]$Repo)

        Push-Location -LiteralPath $Repo
        try {
            $output = & pwsh -NoProfile -NonInteractive -File $script:HookScript -MaxFileSizeKB 1 -MaxLineCount 3 2>&1
            [pscustomobject]@{
                ExitCode = $LASTEXITCODE
                Output   = ($output | Out-String)
            }
        }
        finally {
            Pop-Location
        }
    }
}

Describe 'Test-LargeFiles' {
    It 'passes when no file is too large' {
        $repo = Initialize-TempRepo
        Add-StagedFile -Repo $repo -Name '[x].bin' -Content 'small'

        $result = Invoke-LargeFileHook -Repo $repo

        $result.ExitCode | Should -Be 0
    }

    It 'flags an oversized file with a plain name' {
        $repo = Initialize-TempRepo
        Add-StagedFile -Repo $repo -Name 'big.bin' -Content ('a' * 2048)

        $result = Invoke-LargeFileHook -Repo $repo

        $result.ExitCode | Should -Be 1
        $result.Output | Should -Match 'big\.bin'
    }

    It 'flags an oversized file with brackets in its name' {
        $repo = Initialize-TempRepo
        Add-StagedFile -Repo $repo -Name '[x].bin' -Content ('a' * 2048)

        $result = Invoke-LargeFileHook -Repo $repo

        $result.ExitCode | Should -Be 1
        $result.Output | Should -Match ([regex]::Escape('[x].bin'))
    }

    It 'flags a long source file with brackets in its name' {
        $repo = Initialize-TempRepo
        Add-StagedFile -Repo $repo -Name '[x].cs' -Content ("line`n" * 5)

        $result = Invoke-LargeFileHook -Repo $repo

        $result.ExitCode | Should -Be 1
        $result.Output | Should -Match ([regex]::Escape('[x].cs (5 lines'))
    }

    It 'flags an oversized hidden file' {
        $repo = Initialize-TempRepo
        Add-StagedFile -Repo $repo -Name '.big' -Content ('a' * 2048)

        $result = Invoke-LargeFileHook -Repo $repo

        $result.ExitCode | Should -Be 1
        $result.Output | Should -Match '\.big'
    }
}
