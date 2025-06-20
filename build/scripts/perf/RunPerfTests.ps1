[CmdletBinding(PositionalBinding=$false)]
Param(
    [string] $projects,           # semicolon separated list of relative paths to benchmark projects to run
    [string] $filter,             # filter for tests to run (supports wildcards)
    [String] $perftestRootFolder, # root folder all of the  benchmark projects share
    [String] $output,             # folder to write the benchmark results to
    [bool]   $etl=$false,         # capture etl traces for performance tests
    [bool]   $ci=$false           # run in ci mode (fail fast an keep all partial artifacts)
  )

Push-Location $perftestRootFolder
try {
    # Check if running on Windows and warn about ETL on non-Windows platforms
    $isWindowsPlatform = $PSVersionTable.PSVersion.Major -le 5 -or $IsWindows
    if ($etl -and -not $isWindowsPlatform) {
        Write-Host "Warning: ETL tracing is only supported on Windows. Disabling ETL for this run." -ForegroundColor Yellow
        $etl = $false
    }

    $projectsList = $projects -split ";"
    foreach ($project in $projectsList){
        $projectFullPath = Join-Path $perftestRootFolder $project
        & dotnet restore $projectFullPath -verbosity detailed
        & dotnet build -c Release --no-incremental $projectFullPath
        $comandArguments = "run -c Release --no-build --project $projectFullPath -- --warmupCount  2 --invocationCount 1 --runOncePerIteration --memory --exporters JSON --artifacts $output"
        if ($ci) {
            $comandArguments = "$comandArguments --stopOnFirstError --keepFiles"
        }
        if ($etl) {
            Write-Host "Running tests in project '$projectFullPath'"
            Start-Process -Wait -FilePath "dotnet" -Verb RunAs -ArgumentList "$comandArguments --profiler ETW --filter $filter"
        }
        else {
            Write-Host "Running tests in project '$projectFullPath'"
            Write-Host "dotnet $comandArguments --filter $filter"
            Invoke-Expression "dotnet $comandArguments --filter $filter"
        }
    }
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    Write-Host $_.ScriptStackTrace
    $host.SetShouldExit(1)
    exit 1
}
finally {
    Pop-Location
}
