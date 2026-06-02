[CmdletBinding()]
param(
    [string]$BackupPath = ".\backups\$(Get-Date -Format 'yyyyMMdd-HHmmss')",
    [string]$SqlPassword = "Karamchari@123",
    [string]$ContainerName = "local-sqlserver-1"
)

$ErrorActionPreference = "Stop"

# Resolve absolute path so docker exec paths are unambiguous.
$BackupPath = [System.IO.Path]::GetFullPath($BackupPath)

Write-Host "============================================================"
Write-Host " Karamchari Database Backup"
Write-Host " Target: $BackupPath"
Write-Host "============================================================"

# The docker-compose project name defaults to the directory name.
# Probe both the compose default and an explicit fallback.
function Get-ContainerName {
    $candidates = @("local-sqlserver-1", "karamchari-sqlserver-1", "sqlserver")
    foreach ($name in $candidates) {
        $running = docker inspect --format "{{.State.Running}}" $name 2>$null
        if ($running -eq "true") { return $name }
    }
    throw "SQL Server container not found. Ensure docker compose is running."
}

$container = Get-ContainerName

# The single database name used by all Karamchari services (from docker-compose.yml).
$databases = @("Karamchari")

# Create local backup directory.
if (-not (Test-Path $BackupPath)) {
    New-Item -ItemType Directory -Path $BackupPath | Out-Null
    Write-Host "[OK] Created backup directory: $BackupPath"
}

# Create the backup directory inside the container.
$containerBackupDir = "/var/opt/mssql/backup"
docker exec $container bash -c "mkdir -p $containerBackupDir" | Out-Null

foreach ($db in $databases) {
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $containerFile = "$containerBackupDir/${db}_${timestamp}.bak"
    $localFile = Join-Path $BackupPath "${db}_${timestamp}.bak"

    Write-Host "Backing up database: $db ..."

    $backupSql = @"
BACKUP DATABASE [$db]
TO DISK = N'$containerFile'
WITH FORMAT, INIT, COMPRESSION, STATS = 10;
"@

    docker exec $container /opt/mssql-tools18/bin/sqlcmd `
        -S localhost `
        -U sa `
        -P "$SqlPassword" `
        -C `
        -Q $backupSql

    if ($LASTEXITCODE -ne 0) {
        throw "Backup failed for database '$db'. sqlcmd exit code: $LASTEXITCODE"
    }

    # Copy the .bak file from container to host.
    docker cp "${container}:${containerFile}" $localFile

    if ($LASTEXITCODE -ne 0) {
        throw "docker cp failed for '$db' backup file."
    }

    $sizeMb = [math]::Round((Get-Item $localFile).Length / 1MB, 2)
    Write-Host "[OK] $db -> $localFile ($sizeMb MB)" -ForegroundColor Green
}

Write-Host ""
Write-Host "[DONE] Backup complete. Files in: $BackupPath" -ForegroundColor Green
