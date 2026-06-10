// -----------------------------------------------------------------------
// <copyright file="ImportJobPersistenceTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.DataMigration.Domain.Importing;
using Karamchari.DataMigration.Tests.Integration.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Karamchari.DataMigration.Tests.Integration;

/// <summary>
/// Verifies that ImportJob, ImportRecord, and historical entities persist correctly
/// and are isolated between tenants.
/// </summary>
[Collection(nameof(ImportIntegrationCollection))]
public sealed class ImportJobPersistenceTests(ImportIntegrationFixture fixture)
{
    // ── Basic CRUD ──────────────────────────────────────────────────────────

    [DockerRequiredFact]
    public async Task CreateImportJob_CanBeRetrievedBySameTenant()
    {
        await using var db = fixture.CreateDbContext(ImportIntegrationFixture.TenantDev);

        var job = ImportJob.Create("EmployeeImport", "employees.csv", "file-001", "hash-abc", "admin@dev.local");
        db.ImportJobs.Add(job);
        await db.SaveChangesAsync();

        var loaded = await db.ImportJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == job.Id);
        loaded.Should().NotBeNull();
        loaded!.ImportType.Should().Be("EmployeeImport");
        loaded.FileName.Should().Be("employees.csv");
        loaded.Status.Should().Be(ImportJobStatus.Created);
    }

    [DockerRequiredFact]
    public async Task ImportJob_StatusTransitions_PersistCorrectly()
    {
        await using var db = fixture.CreateDbContext(ImportIntegrationFixture.TenantDev);

        var job = ImportJob.Create("LeaveBalanceImport", "leave.csv", "file-002", "hash-def", "admin@dev.local");
        db.ImportJobs.Add(job);
        await db.SaveChangesAsync();

        job.TransitionTo(ImportJobStatus.Validated);
        job.UpdateProgress(100, 5, 0);
        await db.SaveChangesAsync();

        var loaded = await db.ImportJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == job.Id);
        loaded!.Status.Should().Be(ImportJobStatus.Validated);
        loaded.TotalRows.Should().Be(105);
        loaded.SuccessfulRows.Should().Be(100);
        loaded.FailedRows.Should().Be(5);
    }

    [DockerRequiredFact]
    public async Task CompletedJob_HasCompletedAtTimestamp()
    {
        await using var db = fixture.CreateDbContext(ImportIntegrationFixture.TenantDev);

        var job = ImportJob.Create("HistoricalPayrollImport", "payroll.csv", "file-003", "hash-ghi", "admin");
        db.ImportJobs.Add(job);
        await db.SaveChangesAsync();

        job.TransitionTo(ImportJobStatus.Validated);
        job.TransitionTo(ImportJobStatus.Queued);
        job.TransitionTo(ImportJobStatus.Processing);
        job.TransitionTo(ImportJobStatus.Completed);
        await db.SaveChangesAsync();

        var loaded = await db.ImportJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == job.Id);
        loaded!.CompletedAt.Should().NotBeNull();
        loaded.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    // ── ImportRecord ────────────────────────────────────────────────────────

    [DockerRequiredFact]
    public async Task ImportRecord_CanBeSavedAndQueried()
    {
        await using var db = fixture.CreateDbContext(ImportIntegrationFixture.TenantAcme);

        var job = ImportJob.Create("EmployeeImport", "emp.csv", "file-004", "hash-jkl", "admin@acme.local");
        db.ImportJobs.Add(job);
        await db.SaveChangesAsync();

        var record = ImportRecord.Create(job.Id, 5, "{\"email\":\"bad\"}");
        record.MarkFailed("Email is required.");
        db.ImportRecords.Add(record);
        await db.SaveChangesAsync();

        var loaded = await db.ImportRecords.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == record.Id);
        loaded.Should().NotBeNull();
        loaded!.Status.Should().Be(ImportRecordStatus.Failed);
        loaded.ErrorMessage.Should().Be("Email is required.");
        loaded.RowNumber.Should().Be(5);
    }

    // ── Historical entities ──────────────────────────────────────────────────

    [DockerRequiredFact]
    public async Task HistoricalSalaryRecord_PersistsCorrectly()
    {
        await using var db = fixture.CreateDbContext(ImportIntegrationFixture.TenantDev);

        var job = ImportJob.Create("SalaryComponentImport", "sal.csv", "file-005", "hash-mno", "admin");
        db.ImportJobs.Add(job);
        await db.SaveChangesAsync();

        var record = HistoricalSalaryRecord.Create(job.Id, "EMP001", "Basic", 30000m, "Monthly");
        db.HistoricalSalaryRecords.Add(record);
        await db.SaveChangesAsync();

        var loaded = await db.HistoricalSalaryRecords.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == record.Id);
        loaded.Should().NotBeNull();
        loaded!.ComponentName.Should().Be("Basic");
        loaded.Amount.Should().Be(30000m);
        loaded.Frequency.Should().Be("Monthly");
        loaded.EmployeeNumber.Should().Be("EMP001");
    }

    [DockerRequiredFact]
    public async Task HistoricalPayrollSummary_PersistsCorrectly()
    {
        await using var db = fixture.CreateDbContext(ImportIntegrationFixture.TenantDev);

        var job = ImportJob.Create("HistoricalPayrollImport", "payroll.csv", "file-006", "hash-pqr", "admin");
        db.ImportJobs.Add(job);
        await db.SaveChangesAsync();

        var summary = HistoricalPayrollSummary.Create(job.Id, "EMP002", 3, 2024, 60000m, 12000m, 48000m);
        db.HistoricalPayrollSummaries.Add(summary);
        await db.SaveChangesAsync();

        var loaded = await db.HistoricalPayrollSummaries.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == summary.Id);
        loaded.Should().NotBeNull();
        loaded!.Month.Should().Be(3);
        loaded.Year.Should().Be(2024);
        loaded.GrossPay.Should().Be(60000m);
        loaded.Deductions.Should().Be(12000m);
        loaded.NetPay.Should().Be(48000m);
    }

    // ── Tenant isolation ────────────────────────────────────────────────────

    [DockerRequiredFact]
    public async Task ImportJob_CreatedForTenantDev_IsInvisibleToTenantAcme()
    {
        var devJobId = Guid.NewGuid();

        // Insert directly under tenant dev
        await using (var dbDev = fixture.CreateDbContext(ImportIntegrationFixture.TenantDev))
        {
            var job = ImportJob.Create("EmployeeImport", "dev.csv", devJobId.ToString(), "hash-dev", "admin");
            dbDev.ImportJobs.Add(job);
            await dbDev.SaveChangesAsync();
            devJobId = job.Id;
        }

        // Confirm dev can see it
        await using (var dbDev = fixture.CreateDbContext(ImportIntegrationFixture.TenantDev))
        {
            var job = await dbDev.ImportJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == devJobId);
            job.Should().NotBeNull("dev tenant should see its own job");
        }

        // Confirm acme cannot see it
        await using (var dbAcme = fixture.CreateDbContext(ImportIntegrationFixture.TenantAcme))
        {
            var job = await dbAcme.ImportJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == devJobId);
            job.Should().BeNull("acme tenant must not see dev's job");
        }
    }

    [DockerRequiredFact]
    public async Task ImportJob_CreatedForTenantAcme_IsInvisibleToTenantContoso()
    {
        Guid acmeJobId;

        await using (var dbAcme = fixture.CreateDbContext(ImportIntegrationFixture.TenantAcme))
        {
            var job = ImportJob.Create("LeaveBalanceImport", "acme.csv", "acme-file", "hash-acme", "admin@acme");
            dbAcme.ImportJobs.Add(job);
            await dbAcme.SaveChangesAsync();
            acmeJobId = job.Id;
        }

        await using (var dbContoso = fixture.CreateDbContext(ImportIntegrationFixture.TenantContoso))
        {
            var job = await dbContoso.ImportJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == acmeJobId);
            job.Should().BeNull("contoso must not see acme's job");
        }
    }

    [DockerRequiredFact]
    public async Task HistoricalSalaryRecords_AreIsolatedByTenant()
    {
        Guid devJobId;
        await using (var db = fixture.CreateDbContext(ImportIntegrationFixture.TenantDev))
        {
            var job = ImportJob.Create("SalaryComponentImport", "dev-sal.csv", "dev-sal", "hash-dsal", "admin");
            db.ImportJobs.Add(job);
            db.HistoricalSalaryRecords.Add(HistoricalSalaryRecord.Create(job.Id, "DEVEMP001", "Basic", 50000m, "Monthly"));
            await db.SaveChangesAsync();
            devJobId = job.Id;
        }

        await using (var dbAcme = fixture.CreateDbContext(ImportIntegrationFixture.TenantAcme))
        {
            var records = await dbAcme.HistoricalSalaryRecords.AsNoTracking()
                .Where(r => r.ImportJobId == devJobId)
                .ToListAsync();
            records.Should().BeEmpty("acme must not see dev's salary records");
        }
    }

    [DockerRequiredFact]
    public async Task HistoricalPayrollSummaries_AreIsolatedByTenant()
    {
        Guid devJobId;
        await using (var db = fixture.CreateDbContext(ImportIntegrationFixture.TenantDev))
        {
            var job = ImportJob.Create("HistoricalPayrollImport", "dev-payroll.csv", "dev-pay", "hash-dpay", "admin");
            db.ImportJobs.Add(job);
            db.HistoricalPayrollSummaries.Add(HistoricalPayrollSummary.Create(job.Id, "DEVEMP001", 1, 2024, 50000m, 8000m, 42000m));
            await db.SaveChangesAsync();
            devJobId = job.Id;
        }

        await using (var dbAcme = fixture.CreateDbContext(ImportIntegrationFixture.TenantAcme))
        {
            var records = await dbAcme.HistoricalPayrollSummaries.AsNoTracking()
                .Where(r => r.ImportJobId == devJobId)
                .ToListAsync();
            records.Should().BeEmpty("acme must not see dev's payroll summaries");
        }
    }

    // ── Multi-tenant parallel writes ────────────────────────────────────────

    [DockerRequiredFact]
    public async Task ConcurrentImportJobs_ForDifferentTenants_StoredIsolated()
    {
        var devJob = ImportJob.Create("EmployeeImport", "a.csv", "fa", "ha", "admin@dev");
        var acmeJob = ImportJob.Create("EmployeeImport", "b.csv", "fb", "hb", "admin@acme");

        await using (var dbDev = fixture.CreateDbContext(ImportIntegrationFixture.TenantDev))
        {
            dbDev.ImportJobs.Add(devJob);
            await dbDev.SaveChangesAsync();
        }

        await using (var dbAcme = fixture.CreateDbContext(ImportIntegrationFixture.TenantAcme))
        {
            dbAcme.ImportJobs.Add(acmeJob);
            await dbAcme.SaveChangesAsync();
        }

        // Each tenant only sees one job
        await using (var dbDev = fixture.CreateDbContext(ImportIntegrationFixture.TenantDev))
        {
            var devJobs = await dbDev.ImportJobs.AsNoTracking().ToListAsync();
            devJobs.Should().NotContain(j => j.Id == acmeJob.Id);
        }

        await using (var dbAcme = fixture.CreateDbContext(ImportIntegrationFixture.TenantAcme))
        {
            var acmeJobs = await dbAcme.ImportJobs.AsNoTracking().ToListAsync();
            acmeJobs.Should().NotContain(j => j.Id == devJob.Id);
        }
    }
}
