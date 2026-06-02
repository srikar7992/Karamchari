#Requires -Version 5.1
<#
.SYNOPSIS
    Queries __EFMigrationsHistory for each module and reports applied vs pending migrations.

.DESCRIPTION
    For each module that owns EF Core migrations, this script runs
    `dotnet ef migrations list` and reports which migrations are applied
    (marked with an asterisk by dotnet-ef) and which are pending.

    Prerequisites:
    - SQL Server reachable at the specified connection string
    - dotnet-ef tool installed globally: dotnet tool install --global dotnet-ef

.PARAMETER ConnectionString
    ADO.NET connection string for the target SQL Server instance.

.EXAMPLE
    .\verify-migrations.ps1
    .\verify-migrations.ps1 -ConnectionString "Server=prod-sql;Database=Karamchari;..."
#>
param(
    [string]$ConnectionString = "Server=localhost,1433;Database=Karamchari;User Id=sa;Password=YourStrong!Password;TrustServerCertificate=True;"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent $PSScriptRoot
$ApiHost  = Join-Path $RepoRoot "src\Backend\Hosts\Karamchari.Api\Karamchari.Api.csproj"

$Modules = @(
    @{ Module = "HR";            Project = "src\Backend\Modules\HR\Karamchari.HR\Karamchari.HR.csproj" }
    @{ Module = "Recruitment";   Project = "src\Backend\Modules\Recruitment\Karamchari.Recruitment.Infrastructure\Karamchari.Recruitment.Infrastructure.csproj" }
    @{ Module = "Billing";       Project = "src\Backend\Modules\Billing\Karamchari.Billing\Karamchari.Billing.csproj" }
    @{ Module = "Capability";    Project = "src\Backend\Modules\Capability\Karamchari.Capability\Karamchari.Capability.csproj" }
    @{ Module = "Compensation";  Project = "src\Backend\Modules\Compensation\Karamchari.Compensation\Karamchari.Compensation.csproj" }
    @{ Module = "DataMigration"; Project = "src\Backend\Modules\DataMigration\Karamchari.DataMigration\Karamchari.DataMigration.csproj" }
    @{ Module = "FinancialOps";  Project = "src\Backend\Modules\FinancialOps\Karamchari.FinancialOps\Karamchari.FinancialOps.csproj" }
    @{ Module = "Forecasting";   Project = "src\Backend\Modules\Forecasting\Karamchari.Forecasting\Karamchari.Forecasting.csproj" }
    @{ Module = "Governance";    Project = "src\Backend\Modules\Governance\Karamchari.Governance\Karamchari.Governance.csproj" }
    @{ Module = "Intelligence";  Project = "src\Backend\Modules\Intelligence\Karamchari.Intelligence\Karamchari.Intelligence.csproj" }
    @{ Module = "Notifications"; Project = "src\Backend\Modules\Notifications\Karamchari.Notifications\Karamchari.Notifications.csproj" }
    @{ Module = "Payroll";       Project = "src\Backend\Modules\Payroll\Karamchari.Payroll\Karamchari.Payroll.csproj" }
    @{ Module = "Performance";   Project = "src\Backend\Modules\Performance\Karamchari.Performance\Karamchari.Performance.csproj" }
    @{ Module = "PSA";           Project = "src\Backend\Modules\PSA\Karamchari.PSA\Karamchari.PSA.csproj" }
    @{ Module = "TimeAttendance";Project = "src\Backend\Modules\TimeAttendance\Karamchari.TimeAttendance\Karamchari.TimeAttendance.csproj" }
    @{ Module = "Workflow";      Project = "src\Backend\Modules\Workflow\Karamchari.Workflow\Karamchari.Workflow.csproj" }
)

$Summary = @()

foreach ($entry in $Modules) {
    $projPath = Join-Path $RepoRoot $entry.Project

    Write-Host "Checking module: $($entry.Module) ..."
    try {
        $output = & dotnet ef migrations list `
            --project "$projPath" `
            --startup-project "$ApiHost" `
            --connection "$ConnectionString" `
            --no-connect 2>&1

        # dotnet ef marks applied migrations with (no pending changes detected) or asterisks
        # Lines without a leading space/bracket are migration names; pending ones end without [Applied]
        $lines   = $output -split "`n" | Where-Object { $_ -match "\S" }
        $applied = ($lines | Where-Object { $_ -match "\[Applied\]" } | Measure-Object).Count
        $pending = ($lines | Where-Object { $_ -notmatch "\[Applied\]" -and $_ -match "^\d{14}_" } | Measure-Object).Count

        $status = if ($pending -eq 0) { "UP TO DATE" } else { "PENDING ($pending)" }
        Write-Host "  $($entry.Module): $status (applied: $applied)" -ForegroundColor $(if ($pending -eq 0) { "Green" } else { "Yellow" })

        $Summary += [pscustomobject]@{
            Module  = $entry.Module
            Applied = $applied
            Pending = $pending
            Status  = $status
        }
    }
    catch {
        Write-Warning "  ERROR checking $($entry.Module): $_"
        $Summary += [pscustomobject]@{
            Module  = $entry.Module
            Applied = -1
            Pending = -1
            Status  = "ERROR"
        }
    }
}

Write-Host ""
Write-Host "=== Migration Status Summary ==="
$Summary | Format-Table -AutoSize

$hasPending = $Summary | Where-Object { $_.Pending -gt 0 }
if ($hasPending.Count -gt 0) {
    Write-Warning "Some modules have pending migrations. Run .\run-migrations.ps1 to apply them."
    exit 1
}

$hasErrors = $Summary | Where-Object { $_.Status -eq "ERROR" }
if ($hasErrors.Count -gt 0) {
    Write-Error "Failed to query migration state for one or more modules. Check SQL Server connectivity."
    exit 2
}

Write-Host "All modules are up to date." -ForegroundColor Green
