function Get-PerfDefaultProjects {
    "tests\Moq.Analyzers.Benchmarks\Moq.Analyzers.Benchmarks.csproj"
}

function Normalize-PerfProjects {
    param(
        [string]$Projects
    )

    if ([string]::IsNullOrWhiteSpace($Projects)) {
        return Get-PerfDefaultProjects
    }

    return $Projects
}

function Normalize-PerfFilter {
    param(
        [string]$Filter
    )

    if ([string]::IsNullOrWhiteSpace($Filter)) {
        return "'*'"
    }

    return $Filter
}

function Test-PerfWindowsPlatform {
    param(
        [int]$PowerShellMajorVersion = $PSVersionTable.PSVersion.Major,
        [AllowNull()]
        [Nullable[bool]]$IsWindowsPlatform = $null
    )

    if ($PowerShellMajorVersion -le 5) {
        return $true
    }

    if ($null -ne $IsWindowsPlatform) {
        return [bool]$IsWindowsPlatform
    }

    $isWindowsVariable = Get-Variable -Name IsWindows -ErrorAction SilentlyContinue
    if ($null -ne $isWindowsVariable) {
        return [bool]$isWindowsVariable.Value
    }

    return $false
}

function Resolve-PerfEtl {
    param(
        [bool]$Etl,
        [bool]$IsWindowsPlatform = (Test-PerfWindowsPlatform)
    )

    if ($Etl -and -not $IsWindowsPlatform) {
        Write-Warning "ETL tracing is only supported on Windows. Disabling ETL for this run."
        return $false
    }

    return $Etl
}

function Test-ForcePerfBaseline {
    $env:FORCE_PERF_BASELINE -eq 'true'
}

function Get-PerfOutputRoot {
    param(
        [Parameter(Mandatory=$true)]
        [string]$RepoRoot
    )

    Join-Path $RepoRoot "artifacts\performance\perfResults"
}

function Get-PerfBaselineJsonPath {
    param(
        [Parameter(Mandatory=$true)]
        [string]$RepoRoot
    )

    Resolve-Path (Join-Path $RepoRoot "build\perf\baseline.json")
}

Export-ModuleMember -Function Get-PerfDefaultProjects, Normalize-PerfProjects, Normalize-PerfFilter, Test-PerfWindowsPlatform, Resolve-PerfEtl, Test-ForcePerfBaseline, Get-PerfOutputRoot, Get-PerfBaselineJsonPath
