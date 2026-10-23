Import-Module (Join-Path $PSScriptRoot 'PerfUtils.psm1') -Force -DisableNameChecking
Import-Module (Join-Path $PSScriptRoot 'PerfConfig.psm1') -Force -DisableNameChecking

# The first benchmark process in a CI job runs faster than every later one (#1382).
# One short discarded run keeps both measured phases out of that first slot.
$script:PerfWarmupFilter = '*Moq1000SealedClassBenchmarks*'

# PerfDiff exits with this code when a regression gate trips with --failOnRegression.
# Any other nonzero code means the comparison itself failed, which a rerun cannot fix.
# Same value as Program.RegressionExitCode in src/tools/PerfDiff/Program.cs.
$script:PerfRegressionExitCode = 3

function Test-PerfResults {
    param(
        [String] $ResultsOutput
    )

    if (-not (Test-Path $ResultsOutput)) {
            Write-Warning "Results directory '$ResultsOutput' does not exist."
            return $false
        } else {
            # There can be issues with things mismatching between the baseline and the branch we're on
            # so we need to ensure that the baseline results are in the expected location
            $exists = Get-ChildItem -Path $ResultsOutput -Recurse -File |
                        Where-Object { $_.Name -like "*report-full-compressed.json" } |
                        Select-Object -First 1

            if (-not $exists) {
                Write-Warning "No benchmark report found in '$ResultsOutput'."
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

        [string[]] $Filter,

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

function ConvertTo-PerfBenchmarkFilter {
    <#
    .SYNOPSIS
    Turns benchmark ids into BenchmarkDotNet glob filters that each match exactly one benchmark.

    .DESCRIPTION
    A benchmark id is its full name, for example
    'Moq.Analyzers.Benchmarks.Moq1200Benchmarks.Moq1200WithDiagnostics(FileCount: 1)'.
    BenchmarkDotNet matches a filter against that full name, parameters included, and treats only
    '*' and '?' as wildcards. Neither can be escaped, so each is replaced with '?', which matches
    that character and no other. A double quote is replaced the same way because the argument
    quoting in Format-PerfCommandArguments cannot carry one.

    .PARAMETER Ids
    Benchmark ids from the first comparison round.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [AllowEmptyCollection()]
        [string[]] $Ids
    )

    @($Ids | ForEach-Object { $_ -replace '[*?"]', '?' } | Select-Object -Unique)
}

function Read-PerfRegressedIds {
    <#
    .SYNOPSIS
    Reads the JSON array of benchmark ids that PerfDiff writes with --regressedOutput.

    .PARAMETER Path
    File written by PerfDiff. A missing or malformed file throws.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [String] $Path
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Flagged benchmark ids file not found: '$Path'"
    }

    try {
        $parsed = Get-Content -Raw -LiteralPath $Path | ConvertFrom-Json -NoEnumerate
    } catch {
        throw "Flagged benchmark ids file '$Path' is not valid JSON: $($_.Exception.Message)"
    }

    if ($parsed -isnot [System.Array] -or @($parsed | Where-Object { $_ -isnot [string] -or [string]::IsNullOrEmpty($_) }).Count -gt 0) {
        throw "Flagged benchmark ids file '$Path' must be a JSON array of non-empty strings."
    }

    @($parsed)
}

function Invoke-PerfConfirmation {
    <#
    .SYNOPSIS
    Reruns only the benchmarks flagged in round 1, current first and baseline second, and compares them.

    .DESCRIPTION
    Runner noise does not repeat, a real regression does. Round 1 runs the whole baseline and then the
    whole current branch, so slow drift lands on one side. This rerun covers the few flagged benchmarks,
    in the opposite order, and fails only on benchmarks flagged in both rounds. Throws when the
    regression is confirmed or when any step fails. Returns normally when round 2 does not confirm.

    .PARAMETER BaselineWorktree
    Worktree that round 1 created for the baseline SHA. Pass $null when round 1 used a cached baseline;
    this function then creates it.

    .PARAMETER RegressedIdsPath
    JSON array of ids that PerfDiff wrote in round 1.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [String] $RepoRoot,

        [Parameter(Mandatory=$true)]
        [String] $Output,

        [Parameter(Mandatory=$true)]
        [String] $BaselineSHA,

        [String] $BaselineWorktree,

        [String] $Projects,

        [Parameter(Mandatory=$true)]
        [String] $RegressedIdsPath,

        [bool] $Etl = $false,

        [bool] $Ci = $false,

        [Parameter(Mandatory=$true)]
        [String] $RunPerfTestsPath,

        [Parameter(Mandatory=$true)]
        [String] $ComparePerfResultsPath
    )

    $flaggedIds = @(Read-PerfRegressedIds -Path $RegressedIdsPath)
    if ($flaggedIds.Count -eq 0) {
        throw "Performance comparison failed with exit code $script:PerfRegressionExitCode. PerfDiff flagged no benchmark ids in '$RegressedIdsPath', so there is nothing to rerun."
    }

    Write-PerfLog -Message "Round 1 flagged $($flaggedIds.Count) benchmark(s). Rerunning only those, current first, then baseline."
    $flaggedIds | ForEach-Object { Write-PerfLog -Message "  flagged: $_" }
    $filters = ConvertTo-PerfBenchmarkFilter -Ids $flaggedIds

    $confirmRoot = Join-Path $Output "confirm"
    if (Test-Path $confirmRoot) {
        Remove-Item -Path $confirmRoot -Recurse -Force
    }
    $currentOutput = Join-Path $confirmRoot "perfTest"
    $baselineOutput = Join-Path $confirmRoot "baseline"
    Ensure-Folder $currentOutput | Out-Null
    Ensure-Folder $baselineOutput | Out-Null

    # Current first, baseline second: the reverse of round 1, so drift lands on the other side.
    $currentArgs = New-PerfRunArguments -PerfTestRootFolder $RepoRoot -Projects $Projects -Output $currentOutput -Filter $filters -Etl $Etl -Ci $Ci
    Show-Invocation -ScriptPath $RunPerfTestsPath -Arguments $currentArgs
    & $RunPerfTestsPath @currentArgs
    if ($LASTEXITCODE -ne 0) { throw "Confirmation run of the current branch failed with exit code $LASTEXITCODE." }
    if (-not (Test-PerfResults $currentOutput)) {
        throw "Confirmation run of the current branch produced no benchmark report in '$currentOutput'. Check that the flagged ids still match a benchmark."
    }

    if (-not $BaselineWorktree) {
        $BaselineWorktree = Join-Path (Join-Path $RepoRoot "artifacts") "perfBaseline"
        & git worktree add $BaselineWorktree $BaselineSHA -f
        if ($LASTEXITCODE -ne 0) { throw "git worktree add failed with exit code $LASTEXITCODE" }
    }

    $baselineArgs = New-PerfRunArguments -PerfTestRootFolder $BaselineWorktree -Projects $Projects -Output $baselineOutput -Filter $filters -Etl $Etl -Ci $Ci
    Show-Invocation -ScriptPath $RunPerfTestsPath -Arguments $baselineArgs
    & $RunPerfTestsPath @baselineArgs
    if ($LASTEXITCODE -ne 0) { throw "Confirmation run of the baseline failed with exit code $LASTEXITCODE." }
    if (-not (Test-PerfResults $baselineOutput)) {
        throw "Confirmation run of the baseline produced no benchmark report in '$baselineOutput'. Check that the flagged ids exist at baseline SHA '$BaselineSHA'."
    }

    $confirmedIdsPath = Join-Path $confirmRoot "regressed-ids.json"
    $compareArgs = @{
        baseline = $baselineOutput
        results = $currentOutput
        regressedOutput = $confirmedIdsPath
        confirmOnly = $RegressedIdsPath
    }
    if ($Ci) { $compareArgs.ci = $True }

    Show-Invocation -ScriptPath $ComparePerfResultsPath -Arguments $compareArgs
    & $ComparePerfResultsPath @compareArgs
    $confirmExit = $LASTEXITCODE

    $confirmedCount = 0
    if (Test-Path -LiteralPath $confirmedIdsPath) {
        $confirmedCount = @(Read-PerfRegressedIds -Path $confirmedIdsPath).Count
    }
    Write-PerfLog -Message "Round 1 flagged $($flaggedIds.Count), confirmed $confirmedCount."

    if ($confirmExit -eq 0) {
        Write-PerfLog -Message "Regression not confirmed by the rerun. Treating the round 1 result as runner noise."
        return
    }
    if ($confirmExit -eq $script:PerfRegressionExitCode) {
        throw "Performance regression confirmed by the rerun (exit code $confirmExit): $confirmedCount of $($flaggedIds.Count) flagged benchmark(s) regressed in both rounds."
    }
    throw "Performance confirmation comparison failed with exit code $confirmExit."
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

    # The baseline checkout can target an older TFM than the head checkout.
    # Benchmarks run in-process, so each run would use its own TFM's runtime.
    # LatestMajor runs every phase on the same newest installed runtime.
    $previousRollForward = $env:DOTNET_ROLL_FORWARD
    $env:DOTNET_ROLL_FORWARD = 'LatestMajor'
    try {
        $RepoRoot = Get-RepoRoot
        $Temp = Join-Path $RepoRoot "artifacts"

        Invoke-PerfWarmup -RepoRoot $RepoRoot -Output $output -Ci $ci -RunPerfTestsPath $RunPerfTestsPath

        # Get baseline results
        Write-Host "Running Baseline Tests"

        $resultsOutput = Join-Path $output "baseline"
        $baselineFolder = $null

        # A cached baseline counts only when it holds a benchmark report (#1383).
        if ($useCachedBaseline -and (Test-PerfResults $resultsOutput)) {
            Write-Warning "Using cached baseline results from '$resultsOutput'. No new baseline benchmarks will be run."
        } else {
            # Clear old reports so only this run's report can pass the check below.
            if (Test-Path $resultsOutput) {
                Remove-Item -Path $resultsOutput -Recurse -Force
            }
            Ensure-Folder $resultsOutput | Out-Null

            # Checkout SHA
            $baselineFolder = Join-Path $Temp "perfBaseline"
            & git worktree add $baselineFolder $baselineSHA -f
            if ($LASTEXITCODE -ne 0) { throw "git worktree add failed with exit code $LASTEXITCODE" }

            $baselineCommandArgs = New-PerfRunArguments -PerfTestRootFolder $baselineFolder -Projects $projects -Output $resultsOutput -Filter $filter -Etl $etl -Ci $ci

            Show-Invocation -ScriptPath $RunPerfTestsPath -Arguments $baselineCommandArgs
            & $RunPerfTestsPath @baselineCommandArgs
            if ($LASTEXITCODE -ne 0) { throw "Baseline perf test run failed with exit code $LASTEXITCODE." }

            # Ensure the results exist
            $needRerun = -not (Test-PerfResults $resultsOutput)

            if ($needRerun) {
                if (-not ($filter -eq "*" -or $filter -eq "'*'")) {
                    Write-Warning "The filter '$filter' may not match any benchmarks. We're going to try again without a filter."
                    $baselineCommandArgs.filter = "*"

                    Show-Invocation -ScriptPath $RunPerfTestsPath -Arguments $baselineCommandArgs
                    & $RunPerfTestsPath @baselineCommandArgs
                    if ($LASTEXITCODE -ne 0) { throw "Baseline rerun failed with exit code $LASTEXITCODE." }
                }

                if (-not (Test-PerfResults $resultsOutput)) {
                    throw "Baseline perf run produced no benchmark report in '$resultsOutput'. Check that baseline SHA '$baselineSHA' builds and contains benchmarks."
                }
            }
        }

        Write-Host "Done with baseline run"

        # Ensure output directory has been created
        $testOutput = Join-Path $output "perfTest"
        Ensure-Folder $testOutput | Out-Null

        $commandArgs = New-PerfRunArguments -PerfTestRootFolder $RepoRoot -Projects $projects -Output $testOutput -Filter $filter -Etl $etl -Ci $ci

        Show-Invocation -ScriptPath $RunPerfTestsPath -Arguments $commandArgs

        # Get perf results
        Write-Host "Running performance tests"
        & $RunPerfTestsPath @commandArgs
        if ($LASTEXITCODE -ne 0) { throw "Performance test run failed with exit code $LASTEXITCODE." }
        Write-Host "Done with performance run"

        # Diff perf results
        $regressedIdsPath = Join-Path $output "regressed-ids.json"
        if (Test-Path -LiteralPath $regressedIdsPath) {
            Remove-Item -LiteralPath $regressedIdsPath -Force
        }

        $ComparePerfResultsArgs = @{
                baseline = $resultsOutput
                results = $testOutput
                regressedOutput = $regressedIdsPath
        }
        if ($ci) { $ComparePerfResultsArgs.ci = $True }

        Show-Invocation -ScriptPath $ComparePerfResultsPath -Arguments $ComparePerfResultsArgs
        & $ComparePerfResultsPath @ComparePerfResultsArgs
        $compareExit = $LASTEXITCODE
        if ($compareExit -eq 0) { return }

        # Only a tripped gate is worth a rerun. A comparison error would fail the same way again.
        if ($compareExit -ne $script:PerfRegressionExitCode) { throw "Performance comparison failed with exit code $compareExit." }

        Invoke-PerfConfirmation -RepoRoot $RepoRoot -Output $output -BaselineSHA $baselineSHA -BaselineWorktree $baselineFolder `
            -Projects $projects -RegressedIdsPath $regressedIdsPath -Etl $etl -Ci $ci `
            -RunPerfTestsPath $RunPerfTestsPath -ComparePerfResultsPath $ComparePerfResultsPath
    } finally {
        $env:DOTNET_ROLL_FORWARD = $previousRollForward
    }
}

Export-ModuleMember -Function Test-PerfResults, New-PerfRunArguments, Invoke-PerfWarmup, ConvertTo-PerfBenchmarkFilter, Read-PerfRegressedIds, Invoke-PerfConfirmation, Invoke-PerfBaselineComparison
