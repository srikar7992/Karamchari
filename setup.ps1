[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$scriptPath = Join-Path $PSScriptRoot "scripts/setup/setup.ps1"

if (Test-Path $scriptPath) {
    & $scriptPath
} else {
    throw "Setup script not found at $scriptPath"
}
