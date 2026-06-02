[CmdletBinding()]
param(
    [string]$BackupPath = "",
    [string]$SqlPassword = "Karamchari@123",
    [int]$SqlReadyTimeoutSeconds = 120,
    [switch]$SkipMigrations
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "../../")
$composeFile = Join-Path $repoRoot "infrastructure/local/docker-compose.yml"
$slnFile = Join-Path $repoRoot "src/Backend/Karamchari.sln"

Write-Host "============================================================"
Write-Host " Karamchari Full Environment Rebuild"
Write-Host " Repo root: $repoRoot"
Write-Host "============================================================"

# Step 1: Destroy containers and volumes.
Write-Host ""
Write-Host "[Step 1/5] Tearing down containers and volumes..."
docker compose -f $composeFile down -v --remove-orphans
if ($LASTEXITCODE -ne 0) { throw "docker compose down failed." }
Write-Host "[OK] Environment destroyed." -ForegroundColor Green

# Step 2: Rebuild and start all services.
Write-Host ""
Write-Host "[Step 2/5] Building images and starting all services..."
docker compose -f $composeFile up -d --build
if ($LASTEXITCODE -ne 0) { throw "docker compose up failed." }
Write-Host "[OK] Services started." -ForegroundColor Green

# Step 3: Wait for SQL Server to be ready.
Write-Host ""
Write-Host "[Step 3/5] Waiting for SQL Server (timeout: ${SqlReadyTimeoutSeconds}s)..."
$elapsed = 0
$ready = $false

while ($elapsed -lt $SqlReadyTimeoutSeconds) {
    $result = docker exec local-sqlserver-1 /opt/mssql-tools18/bin/sqlcmd `
        -S localhost -U sa -P "$SqlPassword" -C `
        -Q "SELECT 1" 2>$null

    if ($LASTEXITCODE -eq 0) {
        $ready = $true
        break
    }

    Start-Sleep -Seconds 5
    $elapsed += 5
    Write-Host "  ... still waiting ($elapsed/$SqlReadyTimeoutSeconds s)"
}

if (-not $ready) {
    throw "SQL Server did not become ready within ${SqlReadyTimeoutSeconds}s."
}
Write-Host "[OK] SQL Server is ready." -ForegroundColor Green

# Step 4: Run EF Core migrations.
if (-not $SkipMigrations) {
    Write-Host ""
    Write-Host "[Step 4/5] Running database migrations..."

    # The API startup applies migrations via MigrationRunner on boot.
    # Allow additional time for the API container to complete migrations.
    $apiReady = $false
    $apiElapsed = 0
    $apiTimeout = 60

    while ($apiElapsed -lt $apiTimeout) {
        try {
            $response = Invoke-WebRequest -Uri "http://localhost:8080/health" -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
            if ($response.StatusCode -eq 200) {
                $apiReady = $true
                break
            }
        }
        catch { }

        Start-Sleep -Seconds 5
        $apiElapsed += 5
        Write-Host "  ... waiting for API health check ($apiElapsed/$apiTimeout s)"
    }

    if ($apiReady) {
        Write-Host "[OK] API healthy — migrations applied." -ForegroundColor Green
    }
    else {
        Write-Host "[WARN] API health check did not respond within ${apiTimeout}s. Migrations may still be running." -ForegroundColor Yellow
    }
}
else {
    Write-Host ""
    Write-Host "[Step 4/5] Skipping migrations (SkipMigrations flag set)." -ForegroundColor Yellow
}

# Step 5: Optionally restore from backup.
if ($BackupPath -ne "") {
    Write-Host ""
    Write-Host "[Step 5/5] Restoring from backup: $BackupPath ..."
    & (Join-Path $PSScriptRoot "restore.ps1") -BackupPath $BackupPath -SqlPassword $SqlPassword
    Write-Host "[OK] Backup restored." -ForegroundColor Green
}
else {
    Write-Host ""
    Write-Host "[Step 5/5] No backup path provided — environment starts with fresh data." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "[DONE] Environment rebuild complete." -ForegroundColor Green
Write-Host "       Run verify-recovery.ps1 to confirm data integrity." -ForegroundColor Cyan
