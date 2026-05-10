[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "../..")

Write-Host "Resetting Karamchari Local Environment..." -ForegroundColor Yellow

# 1. Stop Infrastructure and remove volumes
Write-Host "Stopping and removing containers/volumes..."
docker compose -f infrastructure/local/docker-compose.yml down -v
Write-Host "[OK] Infrastructure reset." -ForegroundColor Green

# 2. Clear temp artifacts
Write-Host "Cleaning local artifacts..."
Remove-Item -Path "artifacts" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "**/bin", "**/obj" -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "============================================================" -ForegroundColor Yellow
Write-Host " Reset Complete. " -ForegroundColor Green
Write-Host " Run ./scripts/setup/setup.ps1 to rebuild the environment."
Write-Host "============================================================" -ForegroundColor Yellow
