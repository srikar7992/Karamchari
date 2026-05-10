[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "../..")

Write-Host "Starting Karamchari Local Runtime..." -ForegroundColor Cyan

# 1. Start Infrastructure
Write-Host "Starting infrastructure..."
docker compose -f infrastructure/local/docker-compose.yml up -d
Write-Host "[OK] Infrastructure running." -ForegroundColor Green

# 2. Run API
Write-Host "Starting API (Hot Reload)..."
Write-Host "API will be available at http://localhost:8080" -ForegroundColor Yellow
Write-Host "Dashboards:"
Write-Host " - Seq (Logs): http://localhost:8081"
Write-Host " - Grafana:    http://localhost:3000"
Write-Host " - Prometheus: http://localhost:9090"
Write-Host " - Mailpit:    http://localhost:8025"
Write-Host " - RabbitMQ:   http://localhost:15672"

dotnet watch run --project src/Backend/Karamchari.Api --non-interactive
