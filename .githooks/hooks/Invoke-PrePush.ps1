[CmdletBinding()]
param()

$repoRoot = git rev-parse --show-toplevel
if ($LASTEXITCODE -ne 0 -or -not $repoRoot) {
    Write-Host "FAIL: Unable to determine repository root." -ForegroundColor Red
    exit 1
}
. "$PSScriptRoot/../lib/LintHelpers.ps1"

# Allow newer .NET runtimes to run tests targeting older TFMs
$env:DOTNET_ROLL_FORWARD = "LatestMajor"

try {
    $output = dotnet build (Join-Path $repoRoot "Moq.Analyzers.sln") /p:PedanticMode=true --verbosity quiet 2>&1
    $buildExitCode = $LASTEXITCODE
    $output = $output | Out-String
    $buildPassed = $buildExitCode -eq 0

    if (-not $buildPassed) {
        Set-HookFailed -Check "dotnet build"
        Write-Host $output
        Write-Host "Build failed. Skipping tests."
    }
    else {
        $runSettings = Join-Path $repoRoot "build/targets/tests/test.runsettings"
        $output = dotnet test (Join-Path $repoRoot "Moq.Analyzers.sln") --no-build --settings $runSettings --verbosity quiet 2>&1
        $testExitCode = $LASTEXITCODE
        $output = $output | Out-String
        if ($testExitCode -ne 0) {
            Set-HookFailed -Check "dotnet test"
            Write-Host $output
        }
    }
}
catch {
    Write-Host $_ -ForegroundColor Red
    Write-Host $_.ScriptStackTrace
    $script:HookExitCode = 1
}

if ($script:HookExitCode -ne 0) {
    Write-Host "Bypass: git push --no-verify" -ForegroundColor Yellow
}

exit $script:HookExitCode
