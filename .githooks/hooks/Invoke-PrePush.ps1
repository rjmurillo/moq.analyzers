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
    # Run tech debt marker scan to catch unlinked TODOs before push
    $scanScript = Join-Path $repoRoot "build/scripts/todo-scanner/Scan-TodoComments.ps1"
    if (Test-Path -LiteralPath $scanScript) {
        Write-Host "Running tech debt marker scan..." -ForegroundColor Cyan
        & $scanScript -FailOnUnlinked
        if ($LASTEXITCODE -ne 0) {
            Set-HookFailed -Check "todo-scanner"
        }
    }
    else {
        Write-Warning "Tech debt scanner not found at '$scanScript'. Skipping scan."
    }

    $slnPath = Join-Path $repoRoot "Moq.Analyzers.sln"

    # Mirror CI build flags from .github/actions/setup-restore-build/action.yml
    # to prevent issues from escaping the local environment.
    $buildArgs = @(
        $slnPath
        "--configuration", "Release"
        "--verbosity", "quiet"
        "/p:PedanticMode=true"
        "/p:Deterministic=true"
        "/p:ContinuousIntegrationBuild=true"
        "/p:UseSharedCompilation=false"
        "/p:BuildInParallel=false"
        "/nodeReuse:false"
    )

    Write-Host "Building (Release, CI-parity flags)..." -ForegroundColor Cyan
    $output = dotnet build @buildArgs 2>&1
    $buildExitCode = $LASTEXITCODE
    $output = $output | Out-String

    if ($buildExitCode -ne 0) {
        Set-HookFailed -Check "dotnet build"
        Write-Host $output
        Write-Host "Build failed. Skipping tests."
    }
    else {
        $runSettings = Join-Path $repoRoot "build/targets/tests/test.runsettings"
        $output = dotnet test $slnPath --no-build --configuration Release --settings $runSettings --verbosity quiet 2>&1
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
    $env:DOTNET_ROLL_FORWARD = $originalRollForward
}

if ($script:HookExitCode -ne 0) {
    Write-Host "Bypass: git push --no-verify" -ForegroundColor Yellow
}

exit $script:HookExitCode
