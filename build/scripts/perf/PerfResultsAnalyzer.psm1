Import-Module (Join-Path $PSScriptRoot 'PerfUtils.psm1') -Force -DisableNameChecking

function Resolve-PerfResultsFolders {
    param(
        [Parameter(Mandatory=$true)]
        [String] $Baseline,

        [Parameter(Mandatory=$true)]
        [String] $Results
    )

    $baselinePath = Join-Path $Baseline "results"
    if (-not (Test-Path -LiteralPath $baselinePath)) {
        throw "Perf results folder not found: '$baselinePath'"
    }

    $resultsPath = Join-Path $Results "results"
    if (-not (Test-Path -LiteralPath $resultsPath)) {
        throw "Perf results folder not found: '$resultsPath'"
    }

    @{
        baselineFolder = Resolve-Path $baselinePath
        resultsFolder = Resolve-Path $resultsPath
    }
}

function Get-PerfDiffProjectPath {
    param(
        [Parameter(Mandatory=$true)]
        [string]$RepoRoot
    )

    Join-Path $RepoRoot "src\tools\PerfDiff\PerfDiff.csproj"
}

function Invoke-PerfResultsComparison {
    param(
        [String] $baseline,
        [String] $results,
        [switch] $ci,
        [String] $flaggedOutput
    )

    $currentLocation = Get-Location
    try {
        $folders = Resolve-PerfResultsFolders -Baseline $baseline -Results $results
        $baselineFolder = $folders.baselineFolder
        $resultsFolder = $folders.resultsFolder

        Write-Host "Comparing performance results baseline: '$baselineFolder' "
        Write-Host " - baseline: '$baselineFolder' "
        Write-Host " - results: '$resultsFolder' "

        $RepoRoot = Get-RepoRoot
        $perfDiff = Get-PerfDiffProjectPath -RepoRoot $RepoRoot
        & dotnet build $perfDiff -c Release | Out-Host
        if ($LASTEXITCODE -ne 0) { throw "dotnet build failed with exit code $LASTEXITCODE" }
        $perfDiffArgs = @("--baseline", $baselineFolder, "--results", $resultsFolder, "--failOnRegression")
        if ($flaggedOutput) { $perfDiffArgs += @("--flaggedOutput", $flaggedOutput) }
        & dotnet run -c Release --project $perfDiff -- @perfDiffArgs | Out-Host
        return [int]$LASTEXITCODE
    }
    finally {
        Set-Location $currentLocation
    }
}

function Invoke-PerfConfirmationDecision {
    <#
    .SYNOPSIS
    Runs PerfDiff confirm on the rounds so far and returns its exit code.

    .DESCRIPTION
    Exit codes: 0 means no confirmed regression, 3 means run another round,
    4 means a confirmed regression, and any other value is an error.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [String] $flagged,

        [Parameter(Mandatory=$true)]
        [String] $rounds,

        [Parameter(Mandatory=$true)]
        [int] $minRounds,

        [Parameter(Mandatory=$true)]
        [int] $maxRounds
    )

    $RepoRoot = Get-RepoRoot
    $perfDiff = Get-PerfDiffProjectPath -RepoRoot $RepoRoot
    & dotnet run -c Release --project $perfDiff -- confirm --flagged $flagged --rounds $rounds --minRounds $minRounds --maxRounds $maxRounds | Out-Host
    return [int]$LASTEXITCODE
}

Export-ModuleMember -Function Resolve-PerfResultsFolders, Get-PerfDiffProjectPath, Invoke-PerfResultsComparison, Invoke-PerfConfirmationDecision
