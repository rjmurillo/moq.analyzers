[CmdletBinding()]
param()

$repoRoot = git rev-parse --show-toplevel
if ($LASTEXITCODE -ne 0 -or -not $repoRoot) {
    Write-Error "Unable to determine repository root."
    exit 1
}
. "$PSScriptRoot/../lib/LintHelpers.ps1"

# Allow newer .NET runtimes to run tests targeting older TFMs
$originalRollForward = $env:DOTNET_ROLL_FORWARD
$env:DOTNET_ROLL_FORWARD = "LatestMajor"

try {
    $slnPath = Join-Path $repoRoot "Moq.Analyzers.sln"

    $output = dotnet build $slnPath /p:PedanticMode=true --verbosity quiet 2>&1
    $buildExitCode = $LASTEXITCODE
    $output = $output | Out-String

    if ($buildExitCode -ne 0) {
        Set-HookFailed -Check "dotnet build"
        Write-Host $output
        Write-Host "Build failed. Skipping tests."
    }
    else {
        $runSettings = Join-Path $repoRoot "build/targets/tests/test.runsettings"
        $output = dotnet test $slnPath --no-build --settings $runSettings --verbosity quiet 2>&1
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
finally {
    if ($null -eq $originalRollForward) {
        Remove-Item Env:DOTNET_ROLL_FORWARD -ErrorAction SilentlyContinue
    }
    else {
        $env:DOTNET_ROLL_FORWARD = $originalRollForward
    }
}

if ($script:HookExitCode -ne 0) {
    Write-Host "Bypass: git push --no-verify" -ForegroundColor Yellow
}

exit $script:HookExitCode
