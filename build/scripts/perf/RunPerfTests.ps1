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
Write-Host "Running performance tests in folder: $perftestRootFolder"

try {
    # Check if running on Windows and warn about ETL on non-Windows platforms
    $isWindowsPlatform = $PSVersionTable.PSVersion.Major -le 5 -or $IsWindows
    if ($etl -and -not $isWindowsPlatform) {
        Write-Warning "ETL tracing is only supported on Windows. Disabling ETL for this run." -ForegroundColor Yellow
        $etl = $false
    }

    $projectsList = $projects -split ";"
    foreach ($project in $projectsList) {
        $projectFullPath = Join-Path $perftestRootFolder $project
        & dotnet restore $projectFullPath
        & dotnet build -c Release --no-incremental $projectFullPath
        $commandArguments = "run -c Release --no-build --project $projectFullPath -- --outliers DontRemove --memory --threading --exceptions --exporters JSON --artifacts $output"
        if ($ci) {
            $commandArguments = "$commandArguments --stopOnFirstError --keepFiles"
        }
        if ($etl) {
            $commandArguments = "$commandArguments --profiler ETW"
        }

        $commandArguments = "$commandArguments --filter '$filter'"

        Write-Host "Invoking: dotnet $commandArguments"

        if ($IsWindows) {
            # Note: Using Start-Process with -Verb RunAs to ensure it runs with elevated permissions for 
            # 1. ETL, if it's enabled
            # 2. To allow BenchmarkDotNet to set the power profile for the CPU
            # The `-Verb RunAs` is only supported on Windows
            Start-Process -Wait -FilePath "dotnet" -Verb RunAs -ArgumentList "$commandArguments"
        } else {
            # On non-Windows platforms, we can run without elevation
            Invoke-Expression "dotnet $commandArguments"
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
