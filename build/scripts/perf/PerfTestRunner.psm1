Import-Module (Join-Path $PSScriptRoot 'PerfConfig.psm1') -Force -DisableNameChecking

function Split-PerfProjects {
    param(
        [string]$Projects
    )

    $Projects -split ";"
}

function Get-PerfProjectFullPath {
    param(
        [Parameter(Mandatory=$true)]
        [string]$PerfTestRootFolder,

        [Parameter(Mandatory=$true)]
        [string]$Project
    )

    Join-Path $PerfTestRootFolder $Project
}

function New-PerfBenchmarkArguments {
    param(
        [Parameter(Mandatory=$true)]
        [string]$ProjectFullPath,

        [Parameter(Mandatory=$true)]
        [string]$Output,

        [string]$Filter,

        [bool]$Ci = $false,

        [bool]$Etl = $false
    )

    $commandArguments = @("run", "-c", "Release", "--no-build", "--project", $ProjectFullPath, "--", "--outliers", "DontRemove", "--memory", "--threading", "--exceptions", "--exporters", "JSON", "--artifacts", $Output)
    if ($Ci) {
        $commandArguments += @("--stopOnFirstError", "--keepFiles")
    }
    if ($Etl) {
        $commandArguments += @("--profiler", "ETW")
    }

    $commandArguments += @("--filter", $Filter)
    return $commandArguments
}

function Format-PerfCommandArguments {
    param(
        [Parameter(Mandatory=$true)]
        [string[]]$Arguments
    )

    ($Arguments | ForEach-Object {
        if ($_ -match '\s') { "`"$_`"" } else { $_ }
    }) -join ' '
}

function Invoke-PerfTests {
    param(
        [string] $projects,
        [string] $filter,
        [String] $perftestRootFolder,
        [String] $output,
        [bool]   $etl=$false,
        [bool]   $ci=$false
    )

    Push-Location $perftestRootFolder
    Write-Host "Running performance tests in folder: $perftestRootFolder"

    try {
        $etl = Resolve-PerfEtl -Etl $etl

        $anyFailed = $false
        $projectsList = Split-PerfProjects -Projects $projects
        foreach ($project in $projectsList) {
            $projectFullPath = Get-PerfProjectFullPath -PerfTestRootFolder $perftestRootFolder -Project $project
            & dotnet restore $projectFullPath
            if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed with exit code $LASTEXITCODE for project $project" }
            & dotnet build -c Release --no-incremental $projectFullPath
            if ($LASTEXITCODE -ne 0) { throw "dotnet build failed with exit code $LASTEXITCODE for project $project" }
            $commandArguments = New-PerfBenchmarkArguments -ProjectFullPath $projectFullPath -Output $output -Filter $filter -Ci $ci -Etl $etl
            $formattedArgs = Format-PerfCommandArguments -Arguments $commandArguments
            Write-Host "Invoking: dotnet $formattedArgs"

            if ($etl -and (Test-PerfWindowsPlatform)) {
                # Note: Using Start-Process with -Verb RunAs to ensure it runs with elevated permissions for
                # 1. ETL, if it's enabled
                # 2. To allow BenchmarkDotNet to set the power profile for the CPU
                # The `-Verb RunAs` is only supported on Windows
                Write-Warning "Running with elevated permissions will no longer capture stdout"

                # Start-Process -ArgumentList joins array elements with spaces without quoting,
                # so we must explicitly quote arguments that may contain spaces.
                $quotedArgs = $commandArguments | ForEach-Object {
                    if ($_ -match '\s') { "`"$_`"" } else { $_ }
                }
                $proc = Start-Process -Wait -FilePath "dotnet" -Verb RunAs -ArgumentList $quotedArgs -PassThru
                if ($proc.ExitCode -ne 0) {
                    Write-Warning "dotnet exited with code $($proc.ExitCode) for project $project"
                    $anyFailed = $true
                }
            } else {
                # Use ProcessStartInfo to invoke dotnet directly.
                # PowerShell glob-expands * in splatted arguments to native commands
                # even with $PSNativeCommandArgumentPassing = 'Standard' (PowerShell bug).
                # ProcessStartInfo bypasses PowerShell's argument handling entirely.
                # Use Arguments (string) instead of ArgumentList (collection) for PS 5.1 compatibility.
                $psi = [System.Diagnostics.ProcessStartInfo]::new("dotnet")
                $psi.UseShellExecute = $false
                $psi.Arguments = $formattedArgs
                $proc = [System.Diagnostics.Process]::Start($psi)
                $proc.WaitForExit()
                if ($proc.ExitCode -ne 0) {
                    Write-Warning "dotnet exited with code $($proc.ExitCode) for project $project"
                    $anyFailed = $true
                }
            }
        }

        if ($anyFailed) {
            throw "One or more benchmark projects failed"
        }
    }
    finally {
        Pop-Location
    }
}

Export-ModuleMember -Function Split-PerfProjects, Get-PerfProjectFullPath, New-PerfBenchmarkArguments, Format-PerfCommandArguments, Invoke-PerfTests
