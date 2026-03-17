<#
.SYNOPSIS
    Validates JSON files using python3 json.tool.
.DESCRIPTION
    Accepts one or more file paths as arguments. Validates each file
    individually and reports per-file failures. Exits non-zero if any
    file is invalid. Skips .verified.json files (snapshot test artifacts).
#>
[CmdletBinding()]
param(
    [Parameter(ValueFromRemainingArguments)]
    [string[]]$Files
)

if (-not $Files -or $Files.Count -eq 0) {
    exit 0
}

$code = 0
foreach ($f in $Files) {
    if ($f -match '\.verified\.json$') {
        continue
    }
    if (-not (Test-Path $f)) {
        continue
    }
    python3 -m json.tool "$f" | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "FAIL: json $f" -ForegroundColor Red
        $code = 1
    }
}

exit $code
