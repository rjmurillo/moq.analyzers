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

    # Detect files with pre-existing unstaged changes (e.g., from partial staging via git add -p).
    # These files should NOT be auto-staged to avoid inadvertently including changes the developer
    # deliberately excluded from the commit.
    $preExistingUnstaged = @(git diff --name-only -- $Files)

    & $FixCommand

    $modified = git diff --name-only -- $Files
    if ($modified) {
        $safeToStage = @($modified | Where-Object { $_ -notin $preExistingUnstaged })
        $unsafeToStage = @($modified | Where-Object { $_ -in $preExistingUnstaged })

        if ($safeToStage) {
            Write-Host "Auto-fixed: $($safeToStage -join ', ')"
            git add $safeToStage
        }

        if ($unsafeToStage) {
            Write-Warning "Auto-fixed but NOT staged (had pre-existing unstaged changes): $($unsafeToStage -join ', ')"
            Write-Warning "Please review and stage these files manually."
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
