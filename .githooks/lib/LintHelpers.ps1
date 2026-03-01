# Shared helper functions for git hook scripts.
# Dot-source this file: . "$PSScriptRoot/../lib/LintHelpers.ps1"

$script:HookExitCode = 0

function Test-ToolAvailable {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Command,

        [Parameter()]
        [string]$InstallHint
    )

    if (Get-Command $Command -ErrorAction SilentlyContinue) {
        return $true
    }

    if ($InstallHint) {
        Write-Warning "$Command not found. Install: $InstallHint"
    }

    return $false
}

function Get-StagedFiles {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string[]]$Extensions
    )

    $stagedFiles = git diff --cached --name-only --diff-filter=d
    if (-not $stagedFiles) {
        return @()
    }

    $patterns = $Extensions | ForEach-Object { [regex]::Escape($_) + '$' }
    $combined = ($patterns -join '|')

    $matched = $stagedFiles | Where-Object { $_ -match $combined }
    if (-not $matched) {
        return @()
    }

    return @($matched)
}

function Invoke-AutoFix {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [scriptblock]$FixCommand,

        [Parameter(Mandatory)]
        [string[]]$Files
    )

    & $FixCommand

    $modified = git diff --name-only -- $Files
    if ($modified) {
        Write-Host "Auto-fixed: $($modified -join ', ')"
        git add $modified
    }
}

function Set-HookFailed {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Check
    )

    Write-Host "FAIL: $Check" -ForegroundColor Red
    $script:HookExitCode = 1
}
