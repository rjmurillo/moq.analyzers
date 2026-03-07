<#
.SYNOPSIS
    Scans source files for TODO/FIXME/HACK/UNDONE comments and enforces issue linking.
.DESCRIPTION
    Finds tech debt markers in source code and validates that each one references a
    GitHub issue using the format: TODO(#123), FIXME(#456), HACK(#789), UNDONE(#101).

    Unlinked markers are reported as errors. This ensures technical debt is tracked
    in the issue tracker, not hidden in code comments.
.PARAMETER Path
    Root directory to scan. Defaults to the repository root.
.PARAMETER Extensions
    File extensions to scan. Defaults to common source file types.
.PARAMETER ExcludePatterns
    Regex patterns for paths to exclude from scanning.
.PARAMETER FailOnUnlinked
    If set, exits with code 1 when unlinked markers are found.
.EXAMPLE
    ./Scan-TodoComments.ps1 -FailOnUnlinked
#>
[CmdletBinding()]
param(
    [string]$Path,

    [string[]]$Extensions = @('*.cs', '*.ps1', '*.sh', '*.yaml', '*.yml', '*.json', '*.xml', '*.md', '*.csproj', '*.props', '*.targets'),

    [string[]]$ExcludePatterns = @(
        '(^|[\\/])bin[\\/]',
        '(^|[\\/])obj[\\/]',
        '(^|[\\/])artifacts[\\/]',
        '(^|[\\/])\.git[\\/]',
        '(^|[\\/])\.claude[\\/]',
        '(^|[\\/])\.serena[\\/]',
        '(^|[\\/])node_modules[\\/]',
        '\.verified\.(txt|xml|json)$',
        'Scan-TodoComments\.ps1$'
    ),

    [switch]$FailOnUnlinked
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not $Path) {
    $Path = git rev-parse --show-toplevel
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Unable to determine repository root."
        exit 1
    }
}

# Markers to scan for (case-insensitive)
$markers = @('TODO', 'FIXME', 'HACK', 'UNDONE')

# Pattern that matches any marker (linked or unlinked)
$anyMarkerPattern = '(?i)\b(?:' + ($markers -join '|') + ')\b'

# Use git ls-files to avoid descending into excluded directories (.git, node_modules, etc.)
$gitFiles = git -C $Path ls-files -- ($Extensions | ForEach-Object { "*$($_ -replace '^\*', '')" })
if ($LASTEXITCODE -ne 0) {
    Write-Warning "git ls-files failed; falling back to Get-ChildItem."
    $files = Get-ChildItem -Path $Path -Recurse -Include $Extensions -File
}
else {
    $files = $gitFiles | ForEach-Object {
        $fullPath = Join-Path $Path $_
        if (Test-Path -LiteralPath $fullPath) {
            Get-Item -LiteralPath $fullPath
        }
    }
}

$totalLinked = 0
$totalUnlinked = 0
$unlinkedDetails = [System.Collections.Generic.List[object]]::new()

foreach ($file in $files) {
    $relativePath = $file.FullName.Substring($Path.Length).TrimStart('\', '/')

    # Skip excluded paths
    $skip = $false
    foreach ($pattern in $ExcludePatterns) {
        if ($relativePath -match $pattern) {
            $skip = $true
            break
        }
    }
    if ($skip) { continue }

    $lineNumber = 0
    $fileContent = $null
    try {
        $fileContent = Get-Content -Path $file.FullName -ErrorAction Stop
    }
    catch {
        Write-Warning "Failed to read file: $relativePath - $_"
        continue
    }
    foreach ($line in $fileContent) {
        $lineNumber++

        # Find all marker occurrences in the line and evaluate each individually
        $markerMatches = [regex]::Matches($line, $anyMarkerPattern)
        foreach ($match in $markerMatches) {
            # Check if this specific marker is linked by examining what follows it
            $markerEnd = $match.Index + $match.Length
            $remainingLine = $line.Substring($markerEnd)

            # A linked marker has (#digits) immediately following (allowing whitespace)
            if ($remainingLine -match '^\s*\(\s*#\d+\s*\)') {
                $totalLinked++
            }
            else {
                $totalUnlinked++
                [void]$unlinkedDetails.Add([PSCustomObject]@{
                    File    = $relativePath
                    Line    = $lineNumber
                    Content = $line.Trim()
                    Marker  = $match.Value
                })
            }
        }
    }
}

$total = $totalLinked + $totalUnlinked

Write-Host ""
Write-Host "=== Tech Debt Marker Report ===" -ForegroundColor Cyan
Write-Host "Total markers found: $total"
Write-Host "  Linked to issues:  $totalLinked" -ForegroundColor Green
Write-Host "  Unlinked:          $totalUnlinked" -ForegroundColor $(if ($totalUnlinked -gt 0) { 'Yellow' } else { 'Green' })
Write-Host ""

if ($totalLinked -gt 0) {
    Write-Host "Linked markers are properly tracked." -ForegroundColor Green
}

if ($totalUnlinked -gt 0) {
    Write-Host "Unlinked markers (should reference a GitHub issue):" -ForegroundColor Yellow
    Write-Host ""
    foreach ($item in $unlinkedDetails) {
        Write-Host "  $($item.File):$($item.Line)" -ForegroundColor Yellow
        Write-Host "    $($item.Content)" -ForegroundColor DarkYellow
    }
    Write-Host ""
    Write-Host "Fix by adding an issue reference: TODO(#123): description" -ForegroundColor Yellow
}

# Output GitHub Actions summary if running in CI
if ($env:GITHUB_STEP_SUMMARY) {
    $summary = @"
### Tech Debt Marker Report

| Metric | Count |
|--------|-------|
| Total markers | $total |
| Linked to issues | $totalLinked |
| Unlinked | $totalUnlinked |

"@

    if ($totalUnlinked -gt 0) {
        $summary += @"

<details>
<summary>Unlinked markers ($totalUnlinked)</summary>

| File | Line | Content |
|------|------|---------|
"@
        foreach ($item in $unlinkedDetails) {
            $escapedContent = $item.Content -replace '\|', '\|'
            $summary += "| ``$($item.File)`` | $($item.Line) | ``$escapedContent`` |`n"
        }
        $summary += "`n</details>`n"
    }

    Add-Content -Path $env:GITHUB_STEP_SUMMARY -Value $summary
}

if ($FailOnUnlinked -and $totalUnlinked -gt 0) {
    Write-Host "FAIL: $totalUnlinked unlinked tech debt marker(s) found. Link them to GitHub issues." -ForegroundColor Red
    exit 1
}

exit 0
