[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$scriptRoot = $PSScriptRoot
$repoRoot = Resolve-Path (Join-Path $scriptRoot "../..")

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host " Karamchari Full Local Environment Setup" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan

# 1. Validate Environment
& (Join-Path $scriptRoot "EnvironmentValidator.ps1")

# 2. Configure Git
Write-Host "Configuring Git hooks..."
git config core.hooksPath .githooks
Write-Host "[OK] Git hooks configured." -ForegroundColor Green

# 3. Restore Packages
Write-Host "Restoring .NET packages..."
dotnet restore src/Backend/Karamchari.sln --locked-mode
Write-Host "[OK] .NET packages restored." -ForegroundColor Green

if (Get-Command npm -ErrorAction SilentlyContinue) {
    Write-Host "Restoring npm packages..."
    npm ci --prefix src/Frontend/portal --ignore-scripts --loglevel=error
    Write-Host "[OK] npm packages restored." -ForegroundColor Green
}

# 4. Pull Infrastructure
Write-Host "Pulling infrastructure images..."
docker compose -f infrastructure/local/docker-compose.yml pull
Write-Host "[OK] Images pulled." -ForegroundColor Green

# 5. Start Infrastructure (Core)
Write-Host "Starting core infrastructure (SQL, Redis, RabbitMQ)..."
docker compose -f infrastructure/local/docker-compose.yml up -d sqlserver redis rabbitmq otel-collector seq
Write-Host "Waiting for SQL Server to be ready..."
# Simple wait loop for SQL
$retryCount = 0
while ($retryCount -lt 30) {
    try {
        docker exec (docker compose -f infrastructure/local/docker-compose.yml ps -q sqlserver) /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'Karamchari@123' -Q "SELECT 1" -C -Q "SELECT 1" -t 1 -b > $null 2>&1
        if ($LASTEXITCODE -eq 0) { break }
    } catch {}
    Start-Sleep -Seconds 2
    $retryCount++
}
Write-Host "[OK] Infrastructure is up." -ForegroundColor Green

# 6. Provision Tenants
Write-Host "Provisioning database and tenants..."
dotnet run --project src/Backend/Karamchari.Api --provision-dev-tenants
Write-Host "[OK] Tenants provisioned." -ForegroundColor Green

# 7. Seed Test Data
Write-Host "Seeding additional test data..."
# Run the local-dev-seed.sql using docker exec sqlcmd
$sqlFile = "docs/seed/local-dev-seed.sql"
docker exec -i (docker compose -f infrastructure/local/docker-compose.yml ps -q sqlserver) /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'Karamchari@123' -d Karamchari -C < $sqlFile
Write-Host "[OK] Data seeded." -ForegroundColor Green

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host " Setup Complete! " -ForegroundColor Green
Write-Host " To start the application, run: ./scripts/runtime/run-local.ps1"
Write-Host " To run validation, run: ./scripts/validation/validate-runtime.ps1"
Write-Host "============================================================" -ForegroundColor Cyan
