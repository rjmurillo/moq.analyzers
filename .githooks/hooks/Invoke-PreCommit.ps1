[CmdletBinding()]
param()

$repoRoot = git rev-parse --show-toplevel
. "$PSScriptRoot/../lib/LintHelpers.ps1"

Write-Host "Running pre-commit checks..." -ForegroundColor Cyan

try {
    # --- C# formatting (auto-fix + re-stage) ---
    $csFiles = Get-StagedFiles -Extensions @('.cs')
    if ($csFiles.Count -gt 0) {
        Write-Section "C# Formatting"
        $includePaths = ($csFiles | ForEach-Object { "--include", $_ })
        Invoke-AutoFix -Label "dotnet format" -Files $csFiles -FixCommand {
            dotnet format "$repoRoot/Moq.Analyzers.sln" --verbosity quiet @includePaths 2>&1 | Out-Null
        }
        # Verify formatting is clean after fix
        $output = dotnet format "$repoRoot/Moq.Analyzers.sln" --verify-no-changes --verbosity quiet @includePaths 2>&1
        Write-Result -Check "dotnet format" -Passed ($LASTEXITCODE -eq 0)
        if ($LASTEXITCODE -ne 0) {
            Write-Host $output
        }
    }

    # --- Markdown linting (auto-fix + re-stage) ---
    $mdFiles = Get-StagedFiles -Extensions @('.md')
    if ($mdFiles.Count -gt 0) {
        Write-Section "Markdown Lint"
        if (Test-ToolAvailable -Command "markdownlint-cli2" -InstallHint "npm install -g markdownlint-cli2") {
            $fullPaths = $mdFiles | ForEach-Object { Join-Path $repoRoot $_ }
            Invoke-AutoFix -Label "markdownlint-cli2" -Files $mdFiles -FixCommand {
                markdownlint-cli2 --fix $fullPaths 2>&1 | Out-Null
            }
            # Verify lint is clean after fix
            $output = markdownlint-cli2 $fullPaths 2>&1
            Write-Result -Check "markdownlint-cli2" -Passed ($LASTEXITCODE -eq 0)
            if ($LASTEXITCODE -ne 0) {
                Write-Host $output
            }
        }
    }

    # --- YAML linting (lint only) ---
    $yamlFiles = Get-StagedFiles -Extensions @('.yml', '.yaml')
    if ($yamlFiles.Count -gt 0) {
        Write-Section "YAML Lint"
        if (Test-ToolAvailable -Command "yamllint" -InstallHint "pip install yamllint") {
            $fullPaths = $yamlFiles | ForEach-Object { Join-Path $repoRoot $_ }
            $output = yamllint -c (Join-Path $repoRoot ".yamllint.yml") $fullPaths 2>&1
            Write-Result -Check "yamllint" -Passed ($LASTEXITCODE -eq 0)
            if ($LASTEXITCODE -ne 0) {
                Write-Host $output
            }
        }
    }

    # --- JSON linting (lint only, exclude .verified.json) ---
    $jsonFiles = Get-StagedFiles -Extensions @('.json')
    $jsonFiles = $jsonFiles | Where-Object { $_ -notmatch '\.verified\.json$' }
    if ($jsonFiles.Count -gt 0) {
        Write-Section "JSON Lint"
        if (Test-ToolAvailable -Command "python3" -InstallHint "https://www.python.org/downloads/") {
            $jsonPassed = $true
            foreach ($file in $jsonFiles) {
                $fullPath = Join-Path $repoRoot $file
                $output = python3 -m json.tool $fullPath 2>&1
                if ($LASTEXITCODE -ne 0) {
                    Write-Host "  Invalid JSON: $file"
                    Write-Host $output
                    $jsonPassed = $false
                }
            }
            Write-Result -Check "json validation" -Passed $jsonPassed
        }
    }

    # --- Shell script linting (lint only) ---
    $shellFiles = Get-StagedFiles -Extensions @('.sh', '.bash')
    if ($shellFiles.Count -gt 0) {
        Write-Section "Shell Lint"
        if (Test-ToolAvailable -Command "shellcheck" -InstallHint "https://github.com/koalaman/shellcheck#installing") {
            $fullPaths = $shellFiles | ForEach-Object { Join-Path $repoRoot $_ }
            $output = shellcheck $fullPaths 2>&1
            Write-Result -Check "shellcheck" -Passed ($LASTEXITCODE -eq 0)
            if ($LASTEXITCODE -ne 0) {
                Write-Host $output
            }
        }
    }

    # --- GitHub Actions linting (lint only) ---
    $workflowFiles = Get-StagedFiles -Extensions @('.yml', '.yaml')
    $workflowFiles = $workflowFiles | Where-Object { $_ -match '^\.github/workflows/' }
    if ($workflowFiles.Count -gt 0) {
        Write-Section "GitHub Actions Lint"
        if (Test-ToolAvailable -Command "actionlint" -InstallHint "https://github.com/rhysd/actionlint#install") {
            $fullPaths = $workflowFiles | ForEach-Object { Join-Path $repoRoot $_ }
            $output = actionlint $fullPaths 2>&1
            Write-Result -Check "actionlint" -Passed ($LASTEXITCODE -eq 0)
            if ($LASTEXITCODE -ne 0) {
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
    Write-Host ""
    Write-Host "Pre-commit checks failed. Fix the errors above or use --no-verify to bypass." -ForegroundColor Yellow
}

exit $script:HookExitCode
