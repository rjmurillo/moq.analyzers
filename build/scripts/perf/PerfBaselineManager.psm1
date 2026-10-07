Import-Module (Join-Path $PSScriptRoot 'PerfUtils.psm1') -Force -DisableNameChecking
Import-Module (Join-Path $PSScriptRoot 'PerfConfig.psm1') -Force -DisableNameChecking

# The first benchmark process in a CI job runs faster than every later one (#1382).
# One short discarded run keeps both measured phases out of that first slot.
$script:PerfWarmupFilter = '*Moq1000SealedClassBenchmarks*'

function Test-PerfResults {
    param(
        [String] $ResultsOutput
    )

    if (-not (Test-Path $ResultsOutput)) {
            Write-Warning "Results directory '$ResultsOutput' does not exist after running baseline tests."
            return $false
        } else {
            # There can be issues with things mismatching between the baseline and the branch we're on
            # so we need to ensure that the baseline results are in the expected location
            $exists = Get-ChildItem -Path $ResultsOutput -Recurse -File |
                        Where-Object { $_.Name -like "*report-full-compressed.json" } |
                        Select-Object -First 1

            if (-not $exists) {
                Write-Warning "No baseline results found in '$ResultsOutput'."
                return $false
            }
        }

    return $true
}

function New-PerfRunArguments {
    param(
        [Parameter(Mandatory=$true)]
        [String] $PerfTestRootFolder,

        [string] $Projects,

        [Parameter(Mandatory=$true)]
        [String] $Output,

        [string] $Filter,

        [bool] $Etl = $false,

        [bool] $Ci = $false
    )

    $commandArgs = @{
        perftestRootFolder = $PerfTestRootFolder
        projects = $Projects
        output = $Output
        filter = $Filter
    }
    if ($Etl) { $commandArgs.etl = $True }
    if ($Ci) { $commandArgs.ci =  $True}

    return $commandArgs
}

function Invoke-PerfWarmup {
    <#
    .SYNOPSIS
    Runs one discarded benchmark process before the measured phases.

    .DESCRIPTION
    On CI runners the first benchmark process in a job runs faster than every
    later one. This run takes that slot so the baseline and current runs are
    measured under the same conditions. See issue #1382.

    .PARAMETER RepoRoot
    Root of the current branch. The warm-up always runs the current branch.

    .PARAMETER Output
    Perf results root. Warm-up results go to its 'warmup' subfolder.

    .PARAMETER Ci
    Passes CI mode to RunPerfTests.ps1.

    .PARAMETER RunPerfTestsPath
    Script that runs the benchmarks. Tests replace it with a stub.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [String] $RepoRoot,

        [Parameter(Mandatory=$true)]
        [String] $Output,

        [bool] $Ci = $false,

        [String] $RunPerfTestsPath = (Join-Path $PSScriptRoot "RunPerfTests.ps1")
    )

    $warmupOutput = Join-Path $Output "warmup"
    if (Test-Path $warmupOutput) {
        Remove-Item -Path $warmupOutput -Recurse -Force
    }

    # ETL stays off so a local Windows run does not prompt for elevation twice.
    $warmupArgs = New-PerfRunArguments -PerfTestRootFolder $RepoRoot -Projects (Get-PerfDefaultProjects) -Output $warmupOutput -Filter $script:PerfWarmupFilter -Etl $false -Ci $Ci

    Write-PerfLog -Message "Running warm-up benchmark process. Results are discarded."
    Show-Invocation -ScriptPath $RunPerfTestsPath -Arguments $warmupArgs
    & $RunPerfTestsPath @warmupArgs
    if ($LASTEXITCODE -ne 0) { throw "Warm-up perf run failed with exit code $LASTEXITCODE." }

    if (-not (Test-PerfResults $warmupOutput)) {
        throw "Warm-up perf run produced no benchmark report in '$warmupOutput'. Check that filter '$($script:PerfWarmupFilter)' matches a benchmark."
    }
}

function Invoke-PerfBaselineComparison {
    param(
        [String] $baselineSHA,
        [String] $output,
        [string] $projects,
        [string] $filter,
        [bool] $etl = $false,
        [bool] $ci = $false,
        [bool] $useCachedBaseline = $false,
        [String] $RunPerfTestsPath = (Join-Path $PSScriptRoot "RunPerfTests.ps1"),
        [String] $ComparePerfResultsPath = (Join-Path $PSScriptRoot "ComparePerfResults.ps1")
    )

    $RepoRoot = Get-RepoRoot
    $RunPerfTests = $RunPerfTestsPath
    $ComparePerfResults = $ComparePerfResultsPath
    $Temp = Join-Path $RepoRoot "artifacts"

    Invoke-PerfWarmup -RepoRoot $RepoRoot -Output $output -Ci $ci -RunPerfTestsPath $RunPerfTests

    # Get baseline results
    Write-Host "Running Baseline Tests"

    # Ensure output directory has been created
    Ensure-Folder Join-Path $output "baseline"
    $resultsOutput = Join-Path $output "baseline"

    if ($useCachedBaseline -and (Test-Path $resultsOutput)) {
        Write-Warning "Using cached baseline results from '$resultsOutput'. No new baseline benchmarks will be run."
    } else {
        # Checkout SHA
        $baselineFolder = Join-Path $Temp "perfBaseline"
        & git worktree add $baselineFolder $baselineSHA -f
        if ($LASTEXITCODE -ne 0) { throw "git worktree add failed with exit code $LASTEXITCODE" }

        $baselineCommandArgs = New-PerfRunArguments -PerfTestRootFolder $baselineFolder -Projects $projects -Output $resultsOutput -Filter $filter -Etl $etl -Ci $ci

        Show-Invocation -ScriptPath $RunPerfTests -Arguments $baselineCommandArgs
        & $RunPerfTests @baselineCommandArgs
        if ($LASTEXITCODE -ne 0) { throw "Baseline perf test run failed with exit code $LASTEXITCODE." }

        # Ensure the results exist
        $needRerun = -not (Test-PerfResults $resultsOutput)

        if ($needRerun) {
            if (-not ($filter -eq "*" -or $filter -eq "'*'")) {
                Write-Warning "The filter '$filter' may not match any benchmarks. We're going to try again without a filter."
                $baselineCommandArgs.filter = "*"

                Show-Invocation -ScriptPath $RunPerfTests -Arguments $baselineCommandArgs
                & $RunPerfTests @baselineCommandArgs
                if ($LASTEXITCODE -ne 0) { throw "Baseline rerun failed with exit code $LASTEXITCODE." }
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
    Ensure-Folder Join-Path $output "perfTest"
    $testOutput = Join-Path $output "perfTest"

    $commandArgs = New-PerfRunArguments -PerfTestRootFolder $RepoRoot -Projects $projects -Output $testOutput -Filter $filter -Etl $etl -Ci $ci

    Show-Invocation -ScriptPath $RunPerfTests -Arguments $commandArgs

    # Get perf results
    Write-Host "Running performance tests"
    & $RunPerfTests @commandArgs
    if ($LASTEXITCODE -ne 0) { throw "Performance test run failed with exit code $LASTEXITCODE." }
    Write-Host "Done with performance run"

    # Diff perf results
    $ComparePerfResultsArgs = @{
            baseline = $resultsOutput
            results = $testOutput
    }
    if ($ci) { $ComparePerfResultsArgs.ci = $True }

    Show-Invocation -ScriptPath $ComparePerfResults -Arguments $ComparePerfResultsArgs
    & $ComparePerfResults @ComparePerfResultsArgs
    if ($LASTEXITCODE -ne 0) { throw "Performance comparison failed with exit code $LASTEXITCODE." }
}

Export-ModuleMember -Function Test-PerfResults, New-PerfRunArguments, Invoke-PerfWarmup, Invoke-PerfBaselineComparison
