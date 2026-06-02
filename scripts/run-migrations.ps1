#Requires -Version 5.1
<#
.SYNOPSIS
    Applies all pending EF Core migrations for every Karamchari module.

.DESCRIPTION
    Iterates over each module infrastructure project that owns EF Core migrations
    and runs `dotnet ef database update` against the target SQL Server instance.

    Prerequisites:
    - SQL Server reachable at the specified connection string (e.g. docker-compose up)
    - dotnet-ef tool installed globally: dotnet tool install --global dotnet-ef

.PARAMETER ConnectionString
    ADO.NET connection string for the target SQL Server instance.
    Defaults to the local Docker Compose database.

.PARAMETER DryRun
    When set, prints the dotnet-ef commands that would be executed without running them.

.EXAMPLE
    .\run-migrations.ps1
    .\run-migrations.ps1 -DryRun
    .\run-migrations.ps1 -ConnectionString "Server=prod-sql;Database=Karamchari;..."
#>
param(
    [string]$ConnectionString = "Server=localhost,1433;Database=Karamchari;User Id=sa;Password=YourStrong!Password;TrustServerCertificate=True;",
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent $PSScriptRoot

# Each entry: @{ Project = relative path to the infrastructure .csproj; Startup = relative path to startup host }
# The startup project provides the IDesignTimeDbContextFactory and appsettings
$ApiHost    = "src\Backend\Hosts\Karamchari.Api\Karamchari.Api.csproj"

$Migrations = @(
    @{ Module = "Identity";      Project = "src\Backend\Platform\Karamchari.Identity.Infrastructure\Karamchari.Identity.Infrastructure.csproj";                   Startup = $ApiHost }
    @{ Module = "HR";            Project = "src\Backend\Modules\HR\Karamchari.HR\Karamchari.HR.csproj";                                                           Startup = $ApiHost }
    @{ Module = "Recruitment";   Project = "src\Backend\Modules\Recruitment\Karamchari.Recruitment.Infrastructure\Karamchari.Recruitment.Infrastructure.csproj";  Startup = $ApiHost }
    @{ Module = "Billing";       Project = "src\Backend\Modules\Billing\Karamchari.Billing\Karamchari.Billing.csproj";                                            Startup = $ApiHost }
    @{ Module = "Capability";    Project = "src\Backend\Modules\Capability\Karamchari.Capability\Karamchari.Capability.csproj";                                    Startup = $ApiHost }
    @{ Module = "Compensation";  Project = "src\Backend\Modules\Compensation\Karamchari.Compensation\Karamchari.Compensation.csproj";                              Startup = $ApiHost }
    @{ Module = "DataMigration"; Project = "src\Backend\Modules\DataMigration\Karamchari.DataMigration\Karamchari.DataMigration.csproj";                          Startup = $ApiHost }
    @{ Module = "FinancialOps";  Project = "src\Backend\Modules\FinancialOps\Karamchari.FinancialOps\Karamchari.FinancialOps.csproj";                              Startup = $ApiHost }
    @{ Module = "Forecasting";   Project = "src\Backend\Modules\Forecasting\Karamchari.Forecasting\Karamchari.Forecasting.csproj";                                Startup = $ApiHost }
    @{ Module = "Governance";    Project = "src\Backend\Modules\Governance\Karamchari.Governance\Karamchari.Governance.csproj";                                    Startup = $ApiHost }
    @{ Module = "Intelligence";  Project = "src\Backend\Modules\Intelligence\Karamchari.Intelligence\Karamchari.Intelligence.csproj";                              Startup = $ApiHost }
    @{ Module = "Notifications"; Project = "src\Backend\Modules\Notifications\Karamchari.Notifications\Karamchari.Notifications.csproj";                          Startup = $ApiHost }
    @{ Module = "Payroll";       Project = "src\Backend\Modules\Payroll\Karamchari.Payroll\Karamchari.Payroll.csproj";                                            Startup = $ApiHost }
    @{ Module = "Performance";   Project = "src\Backend\Modules\Performance\Karamchari.Performance\Karamchari.Performance.csproj";                                Startup = $ApiHost }
    @{ Module = "PSA";           Project = "src\Backend\Modules\PSA\Karamchari.PSA\Karamchari.PSA.csproj";                                                        Startup = $ApiHost }
    @{ Module = "TimeAttendance";Project = "src\Backend\Modules\TimeAttendance\Karamchari.TimeAttendance\Karamchari.TimeAttendance.csproj";                        Startup = $ApiHost }
    @{ Module = "Workflow";      Project = "src\Backend\Modules\Workflow\Karamchari.Workflow\Karamchari.Workflow.csproj";                                          Startup = $ApiHost }
)

$Results = @()

foreach ($entry in $Migrations) {
    $projPath    = Join-Path $RepoRoot $entry.Project
    $startupPath = Join-Path $RepoRoot $entry.Startup

    $cmd = "dotnet ef database update " +
           "--project `"$projPath`" " +
           "--startup-project `"$startupPath`" " +
           "--connection `"$ConnectionString`""

    if ($DryRun) {
        Write-Host "[DRY RUN] $($entry.Module): $cmd"
        $Results += [pscustomobject]@{ Module = $entry.Module; Status = "DryRun"; Error = "" }
        continue
    }

    Write-Host "Applying migrations for module: $($entry.Module) ..."
    try {
        Invoke-Expression $cmd
        Write-Host "  OK: $($entry.Module)" -ForegroundColor Green
        $Results += [pscustomobject]@{ Module = $entry.Module; Status = "OK"; Error = "" }
    }
    catch {
        Write-Warning "  FAILED: $($entry.Module) - $_"
        $Results += [pscustomobject]@{ Module = $entry.Module; Status = "FAILED"; Error = $_.ToString() }
    }
}

Write-Host ""
Write-Host "=== Migration Results ==="
$Results | Format-Table -AutoSize

$failed = @($Results | Where-Object { $_.Status -eq "FAILED" })
if ($failed.Count -gt 0) {
    Write-Error "One or more migrations failed. See table above."
    exit 1
}

Write-Host "All migrations applied successfully." -ForegroundColor Green
