[CmdletBinding(PositionalBinding=$false)]
Param(
    [String] $baselineSHA,              # git SHA to use as the baseline for performance
    [String] $output,                   # common folder to write the benchmark results to
    [string] $projects,                 # semicolon separated list of relative paths to benchmark projects to run
    [string] $filter,                   # filter for tests to run (supports wildcards)
    [bool] $etl = $false,               # capture etl traces for performance tests
    [bool] $ci = $false,                # run in ci mode (fail fast an keep all partial artifacts)
    [bool] $useCachedBaseline = $false  # use cached baseline results if available
  )

function EnsureFolder {
    param (
        [String] $path # path to create if it does not exist
    )
    If(!(test-path $path))
    {
        New-Item -ItemType Directory -Force -Path $path
    }
}

function Show-Invocation {
    param(
        [string]$ScriptPath,
        [hashtable]$Arguments
    )
    $parts = @($ScriptPath)
    foreach ($key in $Arguments.Keys) {
        $value = $Arguments[$key]
        if ($null -eq $value) { continue }

        if ($value -eq $true) {
            $parts += "-$key"
        }
        elseif ($value -is [string] -and $value -match '\s') {
            $parts += "-$key `"$value`""
        }
        else {
            $parts += "-$key $value"
        }
    }
    Write-Host "Invoking: $($parts -join ' ')"
}

function Test-PerfResults {
    param()

    if (-not (Test-Path $resultsOutput)) {
            Write-Warning "Results directory '$resultsOutput' does not exist after running baseline tests."
            return $false
        } else {
            # There can be issues with things mismatching between the baseline and the branch we're on
            # so we need to ensure that the baseline results are in the expected location
            $exists = Get-ChildItem -Path $resultsOutput -Recurse -File |
                        Where-Object { $_.Name -like "*report-full-compressed.json" } |
                        Select-Object -First 1

            if (-not $exists) {
                Write-Warning "No baseline results found in '$resultsOutput'."
                return $false
            }
        }

    return $true
}

# Setup paths
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..\..')
$RunPerfTests = Join-Path $PSScriptRoot "RunPerfTests.ps1"
$ComparePerfResults = Join-Path $PSScriptRoot "ComparePerfResults.ps1"
$Temp = Join-Path $RepoRoot "artifacts"

try {
    # Get baseline results
    Write-Host "Running Baseline Tests"

    # Ensure output directory has been created
    EnsureFolder Join-Path $output "baseline"
    $resultsOutput = Join-Path $output "baseline"

    if ($useCachedBaseline -and (Test-Path $resultsOutput)) {
        Write-Warning "Using cached baseline results from '$resultsOutput'. No new baseline benchmarks will be run."
    } else {
        # Checkout SHA
        $baselineFolder = Join-Path $Temp "perfBaseline"
        Invoke-Expression "git worktree add $baselineFolder $baselineSHA -f"

        $baselineCommandArgs = @{
            perftestRootFolder = $baselineFolder
            projects = $projects
            output = $resultsOutput
            filter = $filter
        }
        if ($etl) { $baselineCommandArgs.etl = $True }
        if ($ci) { $baselineCommandArgs.ci =  $True}

        Show-Invocation -ScriptPath $RunPerfTests -Arguments $baselineCommandArgs
        & $RunPerfTests @baselineCommandArgs

        # Ensure the results exist
        $needRerun = -not (Test-PerfResults $resultsOutput)

        if ($needRerun) {
            if (-not ($filter -eq "*" -or $filter -eq "'*'")) {
                Write-Warning "The filter '$filter' may not match any benchmarks. We're going to try again without a filter."
                $baselineCommandArgs.filter = "*"

                Show-Invocation -ScriptPath $RunPerfTests -Arguments $baselineCommandArgs
                & $RunPerfTests @baselineCommandArgs
            }

            if (-not (Test-Path $resultsOutput)) {
                Write-Error "Results directory '$resultsOutput' does not exist after running baseline tests."
                $host.SetShouldExit(1)
                exit 1
            }
        }
    }

    Write-Host "Done with baseline run"

    # Ensure output directory has been created
    EnsureFolder Join-Path $output "perfTest"
    $testOutput = Join-Path $output "perfTest"

    $commandArgs = @{
        perftestRootFolder = $RepoRoot
        projects = $projects
        output = $testOutput
        filter = $filter
    }
    if ($etl) { $commandArgs.etl = $True }
    if ($ci) { $commandArgs.ci =  $True}

    Show-Invocation -ScriptPath $RunPerfTests -Arguments $commandArgs

    # Get perf results
    Write-Host "Running performance tests"
    & $RunPerfTests @commandArgs
    Write-Host "Done with performance run"

    # Diff perf results
    $ComparePerfResultsArgs = @{
            baseline = $resultsOutput
            results = $testOutput
    }
    if ($ci) { $ComparePerfResultsArgs.ci = $True }

    Show-Invocation -ScriptPath $ComparePerfResults -Arguments $ComparePerfResultsArgs
    & $ComparePerfResults @ComparePerfResultsArgs
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    $host.SetShouldExit(1)
    exit 1
}
finally {
}