Import-Module (Join-Path $PSScriptRoot 'PerfUtils.psm1') -Force -DisableNameChecking
Import-Module (Join-Path $PSScriptRoot 'PerfConfig.psm1') -Force -DisableNameChecking

# The first benchmark process in a CI job runs faster than every later one (#1382).
# One short discarded run keeps both measured phases out of that first slot.
$script:PerfWarmupFilter = '*Moq1000SealedClassBenchmarks*'

# Records which SHA produced the baseline reports in a results folder.
$script:PerfBaselineShaFile = 'baseline-sha.txt'

# PerfDiff confirm exit codes. See src/tools/PerfDiff/ConfirmCommand.cs.
$script:ConfirmNotConfirmedExitCode = 0
$script:ConfirmContinueExitCode = 3
$script:ConfirmConfirmedExitCode = 4

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

        [bool] $Ci = $false,

        [bool] $SkipBuild = $false
    )

    $commandArgs = @{
        perftestRootFolder = $PerfTestRootFolder
        projects = $Projects
        output = $Output
        filter = $Filter
    }
    if ($Etl) { $commandArgs.etl = $True }
    if ($Ci) { $commandArgs.ci =  $True}
    if ($SkipBuild) { $commandArgs.skipBuild = $True }

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

function Invoke-PerfConfirmation {
    <#
    .SYNOPSIS
    Reruns flagged benchmarks in paired rounds and decides whether they regressed.

    .DESCRIPTION
    The screen run measures each build once. Its iterations share one machine
    state, so a slower runner looks like slower code. Each round here runs the
    baseline build and the current build as separate processes. Odd rounds run
    the baseline first and even rounds run it second, so drift that grows over
    the job cancels out.

    From MinRounds on, PerfDiff confirm reads the rounds so far. It asks for
    another round until its confidence margin is small enough, the same way
    BenchmarkDotNet sizes iterations. MaxRounds and TimeBudgetMinutes force a
    decision. Neither limit looks at the results, so neither biases the test.

    .PARAMETER FlaggedPath
    The flagged benchmark file that the screen comparison wrote.

    .PARAMETER BaselineRoot
    Root of the baseline checkout. It must hold a build of the benchmarks.

    .PARAMETER CurrentRoot
    Root of the current checkout.

    .PARAMETER Output
    Perf results root. Rounds go to its 'confirm' subfolder.

    .OUTPUTS
    $true when PerfDiff confirms a regression, $false when it does not.
    #>
    param(
        [Parameter(Mandatory=$true)]
        [String] $FlaggedPath,

        [Parameter(Mandatory=$true)]
        [String] $BaselineRoot,

        [Parameter(Mandatory=$true)]
        [String] $CurrentRoot,

        [string] $Projects,

        [Parameter(Mandatory=$true)]
        [String] $Output,

        [bool] $Ci = $false,

        [ValidateRange(2, 100)]
        [int] $MinRounds = 3,

        [ValidateRange(2, 100)]
        [int] $MaxRounds = 10,

        [ValidateRange(0, 1440)]
        [int] $TimeBudgetMinutes = 40,

        [String] $RunPerfTestsPath = (Join-Path $PSScriptRoot "RunPerfTests.ps1"),

        [String] $ConfirmPerfResultsPath = (Join-Path $PSScriptRoot "ConfirmPerfResults.ps1")
    )

    if ($MaxRounds -lt $MinRounds) {
        throw "MaxRounds ($MaxRounds) must be at least MinRounds ($MinRounds)."
    }

    # BenchmarkDotNet filters match the full benchmark name, parameters included.
    $filters = @(Get-Content -Raw -Path $FlaggedPath | ConvertFrom-Json | ForEach-Object { $_.BenchmarkId } | Sort-Object -Unique)
    if ($filters.Count -eq 0) {
        throw "Flagged benchmark file '$FlaggedPath' lists no benchmarks."
    }

    $roundsRoot = Join-Path $Output "confirm"
    if (Test-Path $roundsRoot) {
        Remove-Item -Path $roundsRoot -Recurse -Force
    }

    $sides = @{
        baseline = $BaselineRoot
        current = $CurrentRoot
    }

    Write-PerfLog -Message "Confirming $($filters.Count) flagged benchmark(s) in $MinRounds to $MaxRounds paired rounds: $($filters -join ', ')"
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $previousMinutes = 0
    for ($round = 1; $round -le $MaxRounds; $round++) {
        $order = if ($round % 2 -eq 1) { @('baseline', 'current') } else { @('current', 'baseline') }
        foreach ($side in $order) {
            $roundOutput = Join-Path (Join-Path $roundsRoot $side) "r$round"
            # Round 1 builds each checkout. Later rounds reuse that build.
            $runArgs = New-PerfRunArguments -PerfTestRootFolder $sides[$side] -Projects $Projects -Output $roundOutput -Filter $filters -Etl $false -Ci $Ci -SkipBuild ($round -gt 1)

            Write-PerfLog -Message "Confirmation round $round of at most ${MaxRounds}: $side build."
            Show-Invocation -ScriptPath $RunPerfTestsPath -Arguments $runArgs
            # Out-Host keeps child output out of this function's return value.
            & $RunPerfTestsPath @runArgs | Out-Host
            if ($LASTEXITCODE -ne 0) { throw "Confirmation round $round ($side) failed with exit code $LASTEXITCODE." }
        }

        $elapsedMinutes = Get-PerfElapsedMinutes -Stopwatch $stopwatch
        $roundMinutes = $elapsedMinutes - $previousMinutes
        $previousMinutes = $elapsedMinutes

        if ($round -lt $MinRounds) {
            # The test needs MinRounds rounds. When they cannot fit the budget, confirmation cannot decide in time.
            # Round 1 also builds both checkouts, so only later rounds predict the length of a round.
            $projectedMinutes = $elapsedMinutes + ($roundMinutes * ($MinRounds - $round))
            if ($round -gt 1 -and $projectedMinutes -gt $TimeBudgetMinutes) {
                throw "Confirmation needs about $([math]::Ceiling($projectedMinutes)) minutes for $MinRounds rounds, over its $TimeBudgetMinutes minute budget. The screen result stands."
            }

            continue
        }

        # When another round would pass the time budget, the rounds so far must decide.
        $decisionMaxRounds = $MaxRounds
        if (($elapsedMinutes + $roundMinutes) -gt $TimeBudgetMinutes) {
            Write-Warning "Another confirmation round would pass the $TimeBudgetMinutes minute budget. Deciding on $round rounds."
            $decisionMaxRounds = $round
        }

        $confirmArgs = @{
            flagged = $FlaggedPath
            rounds = $roundsRoot
            minRounds = $MinRounds
            maxRounds = $decisionMaxRounds
        }
        Show-Invocation -ScriptPath $ConfirmPerfResultsPath -Arguments $confirmArgs
        & $ConfirmPerfResultsPath @confirmArgs | Out-Host
        $decision = $LASTEXITCODE
        if ($decision -eq $script:ConfirmContinueExitCode -and $round -lt $decisionMaxRounds) {
            continue
        }

        if ($decision -eq $script:ConfirmNotConfirmedExitCode) { return $false }
        if ($decision -eq $script:ConfirmConfirmedExitCode) { return $true }
        throw "Confirmation analysis failed with exit code $decision after round $round."
    }

    throw "Confirmation analysis did not decide within $MaxRounds rounds."
}

function Get-PerfElapsedMinutes {
    param([System.Diagnostics.Stopwatch] $Stopwatch)

    return $Stopwatch.Elapsed.TotalMinutes
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
        [String] $ComparePerfResultsPath = (Join-Path $PSScriptRoot "ComparePerfResults.ps1"),
        [String] $ConfirmPerfResultsPath = (Join-Path $PSScriptRoot "ConfirmPerfResults.ps1")
    )

    # The baseline checkout can target an older TFM than the head checkout.
    # Benchmarks run in-process, so each run would use its own TFM's runtime.
    # LatestMajor runs every phase on the same newest installed runtime.
    $previousRollForward = $env:DOTNET_ROLL_FORWARD
    $env:DOTNET_ROLL_FORWARD = 'LatestMajor'
    try {
        $RepoRoot = Get-RepoRoot
        $Temp = Join-Path $RepoRoot "artifacts"
        $baselineFolder = Join-Path $Temp "perfBaseline"

        Invoke-PerfWarmup -RepoRoot $RepoRoot -Output $output -Ci $ci -RunPerfTestsPath $RunPerfTestsPath

        # Get baseline results
        Write-Host "Running Baseline Tests"

        $resultsOutput = Join-Path $output "baseline"

        # A cached baseline counts only when it holds a benchmark report (#1383)
        # from the pinned SHA. A repin must not reuse the old SHA's reports.
        $shaMarker = Join-Path $resultsOutput $script:PerfBaselineShaFile
        $cachedSha = if (Test-Path $shaMarker) { (Get-Content -Raw -Path $shaMarker).Trim() } else { $null }
        if ($useCachedBaseline -and $cachedSha -ne $baselineSHA) {
            Write-Warning "Cached baseline in '$resultsOutput' is from SHA '$cachedSha', not '$baselineSHA'. Running the baseline again."
        }

        if ($useCachedBaseline -and $cachedSha -eq $baselineSHA -and (Test-PerfResults $resultsOutput)) {
            Write-Warning "Using cached baseline results from '$resultsOutput'. No new baseline benchmarks will be run."
        } else {
            # Clear old reports so only this run's report can pass the check below.
            if (Test-Path $resultsOutput) {
                Remove-Item -Path $resultsOutput -Recurse -Force
            }
            Ensure-Folder $resultsOutput | Out-Null

            # Checkout SHA
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

            Set-Content -Path $shaMarker -Value $baselineSHA -NoNewline -ErrorAction Stop
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

        # Diff perf results. This is the screen: a tripped gate flags benchmarks
        # for confirmation rounds instead of failing the job.
        $flaggedPath = Join-Path $output "flagged.json"
        if (Test-Path $flaggedPath) {
            Remove-Item -Path $flaggedPath -Force
        }

        $ComparePerfResultsArgs = @{
                baseline = $resultsOutput
                results = $testOutput
                flaggedOutput = $flaggedPath
        }
        if ($ci) { $ComparePerfResultsArgs.ci = $True }

        Show-Invocation -ScriptPath $ComparePerfResultsPath -Arguments $ComparePerfResultsArgs
        & $ComparePerfResultsPath @ComparePerfResultsArgs
        $compareExitCode = $LASTEXITCODE
        if ($compareExitCode -eq 0) {
            return
        }

        # PerfDiff writes the flagged file only after a successful comparison.
        if (-not (Test-Path $flaggedPath)) {
            throw "Performance comparison failed with exit code $compareExitCode."
        }

        $flaggedCount = @(Get-Content -Raw -Path $flaggedPath | ConvertFrom-Json).Count
        if ($flaggedCount -eq 0) {
            throw "Performance comparison failed with exit code $compareExitCode and flagged no benchmarks."
        }

        # A cached baseline has no checkout yet. The first round builds it.
        if (-not (Test-Path $baselineFolder)) {
            & git worktree add $baselineFolder $baselineSHA -f
            if ($LASTEXITCODE -ne 0) { throw "git worktree add failed with exit code $LASTEXITCODE" }
        }

        $confirmed = Invoke-PerfConfirmation -FlaggedPath $flaggedPath -BaselineRoot $baselineFolder -CurrentRoot $RepoRoot `
            -Projects $projects -Output $output -Ci $ci -RunPerfTestsPath $RunPerfTestsPath -ConfirmPerfResultsPath $ConfirmPerfResultsPath
        if ($confirmed) {
            throw "Confirmation rounds reproduced a performance regression. See the CONFIRMED lines above."
        }

        Write-Warning "The screen flagged $flaggedCount benchmark(s), but confirmation rounds did not reproduce a regression."
    } finally {
        $env:DOTNET_ROLL_FORWARD = $previousRollForward
    }
}

Export-ModuleMember -Function Test-PerfResults, New-PerfRunArguments, Invoke-PerfWarmup, Invoke-PerfConfirmation, Invoke-PerfBaselineComparison
