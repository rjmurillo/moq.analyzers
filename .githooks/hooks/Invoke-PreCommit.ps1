[CmdletBinding()]
param()

$repoRoot = git rev-parse --show-toplevel
if ($LASTEXITCODE -ne 0 -or -not $repoRoot) {
    Write-Host "FAIL: Unable to determine repository root." -ForegroundColor Red
    exit 1
}
. "$PSScriptRoot/../lib/LintHelpers.ps1"

try {
    # C# formatting (auto-fix + re-stage)
    $csFiles = Get-StagedFiles -Extensions @('.cs')
    if ($csFiles.Count -gt 0) {
        $slnPath = Join-Path $repoRoot "Moq.Analyzers.sln"
        $includePaths = ($csFiles | ForEach-Object { "--include", $_ })
        Invoke-AutoFix -Files $csFiles -FixCommand {
            dotnet format $slnPath --verbosity quiet @includePaths 2>&1 | Out-Null
        }
        $output = dotnet format $slnPath --verify-no-changes --verbosity quiet @includePaths 2>&1
        if ($LASTEXITCODE -ne 0) {
            Set-HookFailed -Check "dotnet format"
            Write-Host $output
        }
    }

    # Markdown linting (auto-fix + re-stage)
    $mdFiles = Get-StagedFiles -Extensions @('.md')
    if ($mdFiles.Count -gt 0) {
        if (Test-ToolAvailable -Command "markdownlint-cli2" -InstallHint "npm install -g markdownlint-cli2") {
            $fullPaths = $mdFiles | ForEach-Object { Join-Path $repoRoot $_ }
            Invoke-AutoFix -Files $mdFiles -FixCommand {
                & markdownlint-cli2 --fix @fullPaths 2>&1 | Out-Null
            }
            $output = & markdownlint-cli2 @fullPaths 2>&1
            if ($LASTEXITCODE -ne 0) {
                Set-HookFailed -Check "markdownlint-cli2"
                Write-Host $output
            }
        }
    }

    # YAML linting (lint only) and GitHub Actions linting share the file list
    $yamlFiles = Get-StagedFiles -Extensions @('.yml', '.yaml')
    if ($yamlFiles.Count -gt 0) {
        if (Test-ToolAvailable -Command "yamllint" -InstallHint "pip install yamllint") {
            $fullPaths = $yamlFiles | ForEach-Object { Join-Path $repoRoot $_ }
            $output = & yamllint -c (Join-Path $repoRoot ".yamllint.yml") @fullPaths 2>&1
            if ($LASTEXITCODE -ne 0) {
                Set-HookFailed -Check "yamllint"
                Write-Host $output
            }
        }

        # GitHub Actions linting (lint only, subset of YAML files)
        $workflowFiles = $yamlFiles | Where-Object { $_ -match '^\.github/workflows/' }
        if ($workflowFiles.Count -gt 0) {
            if (Test-ToolAvailable -Command "actionlint" -InstallHint "https://github.com/rhysd/actionlint#install") {
                $fullPaths = $workflowFiles | ForEach-Object { Join-Path $repoRoot $_ }
                $output = & actionlint @fullPaths 2>&1
                if ($LASTEXITCODE -ne 0) {
                    Set-HookFailed -Check "actionlint"
                    Write-Host $output
                }
            }
        }
    }

    # JSON linting (lint only, exclude .verified.json)
    $jsonFiles = Get-StagedFiles -Extensions @('.json')
    $jsonFiles = $jsonFiles | Where-Object { $_ -notmatch '\.verified\.json$' }
    if ($jsonFiles.Count -gt 0) {
        # Python may be available as 'python3' or 'python' depending on platform
        $pythonCmd = (Get-Command -Name python3, python -ErrorAction SilentlyContinue | Select-Object -First 1).Name
        if ($pythonCmd) {
            foreach ($file in $jsonFiles) {
                $fullPath = Join-Path $repoRoot $file
                $output = & $pythonCmd -m json.tool $fullPath 2>&1
                if ($LASTEXITCODE -ne 0) {
                    Set-HookFailed -Check "json: $file"
                    Write-Host $output
                }
            }
        }
        else {
            Write-Warning "python not found. Install: https://www.python.org/downloads/"
        }
    }

    # Shell script linting (lint only)
    $shellFiles = Get-StagedFiles -Extensions @('.sh', '.bash')
    if ($shellFiles.Count -gt 0) {
        if (Test-ToolAvailable -Command "shellcheck" -InstallHint "https://github.com/koalaman/shellcheck#installing") {
            $fullPaths = $shellFiles | ForEach-Object { Join-Path $repoRoot $_ }
            $output = & shellcheck @fullPaths 2>&1
            if ($LASTEXITCODE -ne 0) {
                Set-HookFailed -Check "shellcheck"
                Write-Host $output
            }
        }
    }
}
catch {
    Write-Host $_ -ForegroundColor Red
    Write-Host $_.ScriptStackTrace
    $script:HookExitCode = 1
}

if ($script:HookExitCode -ne 0) {
    Write-Host "Bypass: git commit --no-verify" -ForegroundColor Yellow
}

exit $script:HookExitCode
