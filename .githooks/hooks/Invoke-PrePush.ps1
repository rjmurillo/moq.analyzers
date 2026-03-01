[CmdletBinding()]
param()

$repoRoot = git rev-parse --show-toplevel
. "$PSScriptRoot/../lib/LintHelpers.ps1"

# Allow newer .NET runtimes to run tests targeting older TFMs
$env:DOTNET_ROLL_FORWARD = "LatestMajor"

try {
    dotnet build (Join-Path $repoRoot "Moq.Analyzers.sln") /p:PedanticMode=true --verbosity quiet 2>&1
    $buildPassed = $LASTEXITCODE -eq 0

    if (-not $buildPassed) {
        Set-HookFailed -Check "dotnet build"
        Write-Host "Build failed. Skipping tests."
    }
    else {
        $runSettings = Join-Path $repoRoot "build/targets/tests/test.runsettings"
        dotnet test (Join-Path $repoRoot "Moq.Analyzers.sln") --no-build --settings $runSettings --verbosity quiet 2>&1
        if ($LASTEXITCODE -ne 0) {
            Set-HookFailed -Check "dotnet test"
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
