Import-Module (Join-Path $PSScriptRoot 'PerfUtils.psm1') -Force -DisableNameChecking

function Resolve-PerfResultsFolders {
    param(
        [Parameter(Mandatory=$true)]
        [String] $Baseline,

        [Parameter(Mandatory=$true)]
        [String] $Results
    )

    @{
        baselineFolder = Resolve-Path (Join-Path $Baseline "results")
        resultsFolder = Resolve-Path (Join-Path $Results "results")
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
        [switch] $ci
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
        & dotnet build $perfDiff -c Release
        if ($LASTEXITCODE -ne 0) { throw "dotnet build failed with exit code $LASTEXITCODE" }
        & dotnet run -c Release --project $perfDiff -- --baseline $baselineFolder --results $resultsFolder --failOnRegression
        return $LASTEXITCODE
    }
    finally {
        Set-Location $currentLocation
    }
}

Export-ModuleMember -Function Resolve-PerfResultsFolders, Get-PerfDiffProjectPath, Invoke-PerfResultsComparison
