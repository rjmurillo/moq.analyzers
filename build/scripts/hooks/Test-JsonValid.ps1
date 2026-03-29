<#
.SYNOPSIS
    Validates JSON files using Python's json.tool module.
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

# Python may be available as 'python3' (Linux/macOS) or 'python' (Windows)
$pythonCmd = (Get-Command -Name python3, python -ErrorAction SilentlyContinue | Select-Object -First 1).Name
if (-not $pythonCmd) {
    Write-Host "FAIL: python not found. Install: https://www.python.org/downloads/" -ForegroundColor Red
    exit 1
}

$code = 0
foreach ($f in $Files) {
    if ($f -match '\.verified\.json$') {
        continue
    }
    if (-not (Test-Path $f)) {
        continue
    }
    & $pythonCmd -m json.tool "$f" | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "FAIL: json $f" -ForegroundColor Red
        $code = 1
    }
}

exit $code
