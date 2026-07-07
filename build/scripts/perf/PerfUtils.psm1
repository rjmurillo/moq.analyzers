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

function Get-RepoRoot {
    param(
        [string]$ScriptRoot = $PSScriptRoot
    )

    Resolve-Path (Join-Path $ScriptRoot '..\..\..')
}

function Ensure-Folder {
    param (
        [String] $path
    )

    If(!(test-path $path))
    {
        New-Item -ItemType Directory -Force -Path $path
    }
}

function Write-PerfLog {
    param(
        [Parameter(Mandatory=$true)]
        [string]$Message,

        [ValidateSet('Host', 'Warning', 'Error')]
        [string]$Level = 'Host'
    )

    switch ($Level) {
        'Warning' { Write-Warning $Message }
        'Error' { Write-Error $Message }
        default { Write-Host $Message }
    }
}

function Invoke-PerfCommand {
    param(
        [Parameter(Mandatory=$true)]
        [scriptblock]$Command,

        [Parameter(Mandatory=$true)]
        [string]$FailureMessage
    )

    & $Command
    if ($LASTEXITCODE -ne 0) { throw "$FailureMessage with exit code $LASTEXITCODE" }
}

Export-ModuleMember -Function Show-Invocation, Get-RepoRoot, Ensure-Folder, Write-PerfLog, Invoke-PerfCommand
