[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$BackupPath,

    [string]$SqlPassword = "Karamchari@123"
)

$ErrorActionPreference = "Stop"

$BackupPath = [System.IO.Path]::GetFullPath($BackupPath)

Write-Host "============================================================"
Write-Host " Karamchari Database Restore"
Write-Host " Source: $BackupPath"
Write-Host "============================================================"

if (-not (Test-Path $BackupPath)) {
    throw "Backup path not found: $BackupPath"
}

function Get-ContainerName {
    $candidates = @("local-sqlserver-1", "karamchari-sqlserver-1", "sqlserver")
    foreach ($name in $candidates) {
        $running = docker inspect --format "{{.State.Running}}" $name 2>$null
        if ($running -eq "true") { return $name }
    }
    throw "SQL Server container not found. Ensure docker compose is running."
}

$container = Get-ContainerName
$containerRestoreDir = "/var/opt/mssql/restore"

docker exec $container bash -c "mkdir -p $containerRestoreDir" | Out-Null

# Find all .bak files in the backup directory.
$bakFiles = Get-ChildItem -Path $BackupPath -Filter "*.bak"
if ($bakFiles.Count -eq 0) {
    throw "No .bak files found in $BackupPath"
}

Write-Host "Found $($bakFiles.Count) backup file(s):"
$bakFiles | ForEach-Object { Write-Host "  - $($_.Name)" }
Write-Host ""

foreach ($bakFile in $bakFiles) {
    # Derive database name from file name: Karamchari_20260602_120000.bak -> Karamchari
    $dbName = ($bakFile.BaseName -split "_")[0]
    $containerFile = "$containerRestoreDir/$($bakFile.Name)"

    Write-Host "Restoring database: $dbName from $($bakFile.Name) ..."

    # Copy backup file into container.
    docker cp $bakFile.FullName "${container}:${containerFile}"
    if ($LASTEXITCODE -ne 0) {
        throw "docker cp failed for '$($bakFile.Name)'."
    }

    # Set the database to single-user mode and restore.
    $restoreSql = @"
IF DB_ID('$dbName') IS NOT NULL
BEGIN
    ALTER DATABASE [$dbName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
END;

RESTORE DATABASE [$dbName]
FROM DISK = N'$containerFile'
WITH REPLACE, RECOVERY, STATS = 10;

ALTER DATABASE [$dbName] SET MULTI_USER;
"@

    docker exec $container /opt/mssql-tools18/bin/sqlcmd `
        -S localhost `
        -U sa `
        -P "$SqlPassword" `
        -C `
        -Q $restoreSql

    if ($LASTEXITCODE -ne 0) {
        throw "Restore failed for database '$dbName'. sqlcmd exit code: $LASTEXITCODE"
    }

    Write-Host "[OK] $dbName restored successfully." -ForegroundColor Green
}

Write-Host ""
Write-Host "[DONE] All databases restored. Run verify-recovery.ps1 to confirm integrity." -ForegroundColor Green
