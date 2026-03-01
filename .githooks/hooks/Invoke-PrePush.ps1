[CmdletBinding()]
param()

$repoRoot = git rev-parse --show-toplevel
. "$PSScriptRoot/../lib/LintHelpers.ps1"

# Allow newer .NET runtimes to run tests targeting older TFMs
$env:DOTNET_ROLL_FORWARD = "LatestMajor"

Write-Host "Running pre-push checks..." -ForegroundColor Cyan

try {
    # --- Build ---
    Write-Section "Build"
    dotnet build (Join-Path $repoRoot "Moq.Analyzers.sln") /p:PedanticMode=true --verbosity quiet 2>&1
    $buildPassed = $LASTEXITCODE -eq 0
    Write-Result -Check "dotnet build" -Passed $buildPassed

    if (-not $buildPassed) {
        Write-Host "  Build failed. Skipping tests." -ForegroundColor Yellow
    }
    else {
        # --- Tests ---
        Write-Section "Tests"
        $runSettings = Join-Path $repoRoot "build/targets/tests/test.runsettings"
        dotnet test (Join-Path $repoRoot "Moq.Analyzers.sln") --no-build --settings $runSettings --verbosity quiet 2>&1
        Write-Result -Check "dotnet test" -Passed ($LASTEXITCODE -eq 0)
    }
}
catch {
    Write-Host $_ -ForegroundColor Red
    Write-Host $_.ScriptStackTrace
    $script:HookExitCode = 1
}

if ($script:HookExitCode -ne 0) {
    Write-Host ""
    Write-Host "Pre-push checks failed. Fix the errors above or use --no-verify to bypass." -ForegroundColor Yellow
}

exit $script:HookExitCode
