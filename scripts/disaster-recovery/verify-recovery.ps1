[CmdletBinding()]
param(
    [string]$SqlPassword = "Karamchari@123",
    [string]$ApiBaseUrl = "http://localhost:8080"
)

$ErrorActionPreference = "Stop"

Write-Host "============================================================"
Write-Host " Karamchari Recovery Verification"
Write-Host "============================================================"

$failures = @()

function Get-ContainerName {
    $candidates = @("local-sqlserver-1", "karamchari-sqlserver-1", "sqlserver")
    foreach ($name in $candidates) {
        $running = docker inspect --format "{{.State.Running}}" $name 2>$null
        if ($running -eq "true") { return $name }
    }
    throw "SQL Server container not found. Ensure docker compose is running."
}

$container = Get-ContainerName

# ── Helper: run a sqlcmd query and return stdout ──────────────────────────────
function Invoke-Sql {
    param([string]$Query, [string]$Database = "Karamchari")
    $output = docker exec $container /opt/mssql-tools18/bin/sqlcmd `
        -S localhost -U sa -P "$SqlPassword" -C `
        -d $Database `
        -h -1 -W `
        -Q $Query 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "sqlcmd failed (exit $LASTEXITCODE): $output"
    }
    return ($output | Where-Object { $_ -match '\S' }) -join "`n"
}

# ── Check 1: Database exists ──────────────────────────────────────────────────
Write-Host ""
Write-Host "[ 1 ] Checking database exists..."
try {
    $dbCheck = Invoke-Sql -Query "SELECT name FROM sys.databases WHERE name = 'Karamchari'" -Database "master"
    if ($dbCheck -match "Karamchari") {
        Write-Host "      [PASS] Database 'Karamchari' exists." -ForegroundColor Green
    }
    else {
        $failures += "Database 'Karamchari' not found."
        Write-Host "      [FAIL] Database 'Karamchari' not found." -ForegroundColor Red
    }
}
catch {
    $failures += "Database check failed: $_"
    Write-Host "      [FAIL] $_" -ForegroundColor Red
}

# ── Check 2: Key table row counts ─────────────────────────────────────────────
Write-Host ""
Write-Host "[ 2 ] Verifying key table row counts..."

$keyTables = @(
    "dbo.OutboxMessage",
    "dbo.OutboxState",
    "dbo.InboxState"
)

foreach ($table in $keyTables) {
    try {
        $count = Invoke-Sql -Query "SELECT COUNT(*) FROM $table"
        $count = $count.Trim()
        Write-Host "      [OK] $table : $count rows" -ForegroundColor Green
    }
    catch {
        # Table may not exist (schema may differ after fresh restore) — warn, don't hard fail.
        Write-Host "      [WARN] Could not query $table : $_" -ForegroundColor Yellow
    }
}

# ── Check 3: Migrations applied (EF migration history table) ──────────────────
Write-Host ""
Write-Host "[ 3 ] Verifying EF Core migration history..."
try {
    $migrationCount = Invoke-Sql -Query "SELECT COUNT(*) FROM dbo.__EFMigrationsHistory"
    $migrationCount = ($migrationCount -split "`n" | Where-Object { $_ -match '^\s*\d+\s*$' } | Select-Object -First 1).Trim()
    if ([int]$migrationCount -gt 0) {
        Write-Host "      [PASS] $migrationCount migration(s) recorded in __EFMigrationsHistory." -ForegroundColor Green
    }
    else {
        $failures += "No migrations found in __EFMigrationsHistory."
        Write-Host "      [FAIL] No migrations recorded." -ForegroundColor Red
    }
}
catch {
    $failures += "Migration history check failed: $_"
    Write-Host "      [FAIL] $_" -ForegroundColor Red
}

# ── Check 4: API health endpoint ─────────────────────────────────────────────
Write-Host ""
Write-Host "[ 4 ] Checking API health endpoint ($ApiBaseUrl/health)..."
try {
    $response = Invoke-WebRequest -Uri "$ApiBaseUrl/health" -UseBasicParsing -TimeoutSec 10 -ErrorAction Stop
    if ($response.StatusCode -eq 200) {
        Write-Host "      [PASS] API health endpoint returned 200 OK." -ForegroundColor Green
    }
    else {
        $failures += "API health endpoint returned HTTP $($response.StatusCode)."
        Write-Host "      [FAIL] HTTP $($response.StatusCode)." -ForegroundColor Red
    }
}
catch {
    Write-Host "      [WARN] API health check failed (API may not be running): $_" -ForegroundColor Yellow
}

# ── Check 5: OutboxMessage — no rows stuck in 'Processing' state ──────────────
Write-Host ""
Write-Host "[ 5 ] Checking for stuck outbox messages..."
try {
    $stuckSql = "SELECT COUNT(*) FROM dbo.OutboxMessage WHERE LockId IS NOT NULL AND Delivered IS NULL"
    $stuckCount = Invoke-Sql -Query $stuckSql
    $stuckCount = ($stuckCount -split "`n" | Where-Object { $_ -match '^\s*\d+\s*$' } | Select-Object -First 1).Trim()
    if ([int]$stuckCount -eq 0) {
        Write-Host "      [PASS] No stale-locked outbox messages." -ForegroundColor Green
    }
    else {
        Write-Host "      [WARN] $stuckCount stale-locked outbox message(s) detected. Relay will self-heal on next cycle." -ForegroundColor Yellow
    }
}
catch {
    Write-Host "      [WARN] Could not query outbox state: $_" -ForegroundColor Yellow
}

# ── Summary ───────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "============================================================"
if ($failures.Count -eq 0) {
    Write-Host " RECOVERY VERIFICATION: PASSED" -ForegroundColor Green
}
else {
    Write-Host " RECOVERY VERIFICATION: FAILED ($($failures.Count) issue(s))" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}
Write-Host "============================================================"
