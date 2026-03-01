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
        Write-Warning "  $Command not found. Install: $InstallHint"
    }
    else {
        Write-Warning "  $Command not found. Skipping."
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

    # Return as array even for single match
    return @($matched)
}

function Invoke-AutoFix {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Label,

        [Parameter(Mandatory)]
        [scriptblock]$FixCommand,

        [Parameter(Mandatory)]
        [string[]]$Files
    )

    & $FixCommand

    # Check if any staged files were modified by the fix
    $modified = git diff --name-only -- $Files
    if ($modified) {
        Write-Host "  Auto-fixed and re-staging: $($modified -join ', ')"
        git add $modified
    }
}

function Write-Section {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Title
    )

    Write-Host ""
    Write-Host "--- $Title ---" -ForegroundColor Cyan
}

function Write-Result {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Check,

        [Parameter(Mandatory)]
        [bool]$Passed
    )

    if ($Passed) {
        Write-Host "  PASS: $Check" -ForegroundColor Green
    }
    else {
        Write-Host "  FAIL: $Check" -ForegroundColor Red
        $script:HookExitCode = 1
    }
}
