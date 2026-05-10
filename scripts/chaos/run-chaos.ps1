[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "../..")

Write-Host "============================================================" -ForegroundColor Red
Write-Host " Karamchari Local Chaos Execution" -ForegroundColor Red
Write-Host "============================================================" -ForegroundColor Red

# 1. Inject Chaos and Run Tests
Write-Host "Executing Chaos Engineering Suite..."
dotnet test src/Backend/Karamchari.sln --filter "FullyQualifiedName~Karamchari.TenantIsolationCertification.Chaos" --logger "console;verbosity=normal"

Write-Host "============================================================" -ForegroundColor Red
Write-Host " Chaos Execution Complete " -ForegroundColor Red
Write-Host " Check Seq (http://localhost:8081) for failure recovery traces."
Write-Host "============================================================" -ForegroundColor Red
