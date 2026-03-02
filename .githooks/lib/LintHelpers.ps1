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

    # Snapshot files with pre-existing unstaged changes to avoid
    # silently including a developer's partial staging in the commit
    $dirtyBefore = @(git diff --name-only -- $Files)

    & $FixCommand

    $modified = git diff --name-only -- $Files
    if ($modified) {
        $safeToStage = @($modified | Where-Object { $_ -notin $dirtyBefore })
        $skipped = @($modified | Where-Object { $_ -in $dirtyBefore })

        if ($safeToStage.Count -gt 0) {
            Write-Host "Auto-fixed: $($safeToStage -join ', ')"
            git add $safeToStage
        }
        if ($skipped.Count -gt 0) {
            Write-Warning "Has unstaged changes, not re-staging: $($skipped -join ', ')"
        }
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
