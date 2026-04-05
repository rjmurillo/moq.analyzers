<#
.SYNOPSIS
    Runs dotnet build and test with CI-parity flags for pre-push validation.
.DESCRIPTION
    Sets DOTNET_ROLL_FORWARD=LatestMajor to allow tests targeting older TFMs
    (e.g., net8.0) to run under the installed SDK. Mirrors the exact build
    flags used in CI to catch issues before push.
#>
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$env:DOTNET_ROLL_FORWARD = "LatestMajor"

$repoRoot = git rev-parse --show-toplevel
if ($LASTEXITCODE -ne 0) {
    Write-Host "FAIL: Unable to determine repository root" -ForegroundColor Red
    exit 1
}
$sln = Join-Path $repoRoot "Moq.Analyzers.sln"

Write-Host "Building (Release, CI-parity flags)..."
dotnet build $sln `
    --configuration Release `
    --verbosity quiet `
    /p:PedanticMode=true `
    /p:Deterministic=true `
    /p:ContinuousIntegrationBuild=true `
    /p:UseSharedCompilation=false `
    /p:BuildInParallel=false `
    /nodeReuse:false

if ($LASTEXITCODE -ne 0) {
    Write-Host "FAIL: dotnet build" -ForegroundColor Red
    exit 1
}

Write-Host "Running tests..."
dotnet test $sln `
    --no-build `
    --configuration Release `
    --settings (Join-Path $repoRoot "build/targets/tests/test.runsettings") `
    --verbosity quiet

if ($LASTEXITCODE -ne 0) {
    Write-Host "FAIL: dotnet test" -ForegroundColor Red
    exit 1
}

exit 0
