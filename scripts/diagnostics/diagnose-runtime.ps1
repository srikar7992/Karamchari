[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "../..")

Write-Host "Karamchari Runtime Diagnostics..." -ForegroundColor Cyan

# 1. Container Status
Write-Host "`n--- Container Status ---"
docker compose -f infrastructure/local/docker-compose.yml ps

# 2. Check Port Bindings
Write-Host "`n--- Port Bindings ---"
Get-NetTCPConnection -LocalPort 8080, 1433, 6379, 5672, 15672, 5341, 8081 -ErrorAction SilentlyContinue | Select-Object LocalPort, State, OwningProcess

# 3. Check SQL Server Connection
Write-Host "`n--- SQL Server Connectivity ---"
try {
    docker exec (docker compose -f infrastructure/local/docker-compose.yml ps -q sqlserver) /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'Karamchari@123' -Q "SELECT name FROM sys.databases" -C
} catch {
    Write-Warning "Failed to connect to SQL Server."
}

# 4. Check Redis Connection
Write-Host "`n--- Redis Connectivity ---"
try {
    docker exec (docker compose -f infrastructure/local/docker-compose.yml ps -q redis) redis-cli ping
} catch {
    Write-Warning "Failed to connect to Redis."
}

# 5. Check RabbitMQ Connection
Write-Host "`n--- RabbitMQ Connectivity ---"
try {
    docker exec (docker compose -f infrastructure/local/docker-compose.yml ps -q rabbitmq) rabbitmqctl status | Select-Object -First 10
} catch {
    Write-Warning "Failed to connect to RabbitMQ."
}

Write-Host "`nDiagnostics complete." -ForegroundColor Cyan
