<#
.SYNOPSIS
    Checks staged files against size and line-count thresholds.
.DESCRIPTION
    Prevents accidentally committing oversized files or source files
    that have grown beyond a maintainable line count. Binary and
    generated files are excluded from the line-count check.
.PARAMETER MaxFileSizeKB
    Maximum file size in kilobytes. Default 500 KB.
.PARAMETER MaxLineCount
    Maximum number of lines for source files. Default 1000 lines.
.PARAMETER SourceExtensions
    File extensions treated as source code for line-count checks.
#>
[CmdletBinding()]
param(
    [int]$MaxFileSizeKB = 500,
    [int]$MaxLineCount = 1000,
    [string[]]$SourceExtensions = @('.cs', '.ps1', '.sh', '.yaml', '.yml', '.json', '.xml', '.md')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = git rev-parse --show-toplevel
if ($LASTEXITCODE -ne 0) {
    Write-Host "FAIL: Unable to determine repository root" -ForegroundColor Red
    exit 1
}
$stagedFiles = git diff --cached --name-only --diff-filter=d
if ($LASTEXITCODE -ne 0 -or -not $stagedFiles) {
    exit 0
}

$maxBytes = $MaxFileSizeKB * 1024
$violations = @()

foreach ($file in $stagedFiles) {
    $fullPath = Join-Path $repoRoot $file
    if (-not (Test-Path $fullPath)) {
        continue
    }

    $fileInfo = Get-Item -LiteralPath $fullPath
    if ($fileInfo.Length -gt $maxBytes) {
        $sizeKB = [math]::Round($fileInfo.Length / 1024, 1)
        $violations += "  $file ($($sizeKB) KB exceeds $($MaxFileSizeKB) KB limit)"
    }

    $ext = [System.IO.Path]::GetExtension($file)
    if ($ext -and $SourceExtensions -contains $ext) {
        $lineCount = (Get-Content -LiteralPath $fullPath | Measure-Object -Line).Lines
        if ($lineCount -gt $MaxLineCount) {
            $violations += "  $file ($lineCount lines exceeds $MaxLineCount line limit)"
        }
    }
}

if ($violations.Count -gt 0) {
    Write-Host "Large file(s) detected:" -ForegroundColor Yellow
    $violations | ForEach-Object { Write-Host $_ -ForegroundColor Yellow }
    exit 1
}

exit 0
