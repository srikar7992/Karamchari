// -----------------------------------------------------------------------
// <copyright file="ImportIdempotencyTests.cs" company="Karamchari">
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
/// Verifies that import operations behave correctly under duplicate or retry scenarios.
/// This certification is required for enterprise-grade bulk import systems.
/// </summary>
[Collection(nameof(ImportIntegrationCollection))]
public sealed class ImportIdempotencyTests(ImportIntegrationFixture fixture)
{
    // ── Same file hash ───────────────────────────────────────────────────────

    [DockerRequiredFact]
    public async Task TwoJobsWithSameFileHash_CanCoexist_NoConstraintViolation()
    {
        // The platform allows the same file to be re-imported (user decision, not a constraint violation).
        // The unique-per-tenant constraint is on (TenantId, FileHash) — only one unique record per combo.
        // Here we verify two separate jobs for the SAME tenant with the SAME hash can be tracked distinctly.
        await using var db = fixture.CreateDbContext(ImportIntegrationFixture.TenantDev);

        var job1 = ImportJob.Create("EmployeeImport", "same.csv", "file-A", "IDENTHASH001", "admin");
        var job2 = ImportJob.Create("EmployeeImport", "same.csv", "file-B", "IDENTHASH001", "admin");

        db.ImportJobs.Add(job1);
        db.ImportJobs.Add(job2);

        // Should succeed — same hash tracked for audit, user chose to re-import
        var act = async () => await db.SaveChangesAsync();
        await act.Should().NotThrowAsync("duplicate-hash imports are allowed and tracked for audit");

        var jobs = await db.ImportJobs.AsNoTracking()
            .Where(j => j.FileHash == "IDENTHASH001")
            .ToListAsync();
        jobs.Should().HaveCount(2);
    }

    // ── Job status is terminal after completion ───────────────────────────────

    [DockerRequiredFact]
    public async Task CompletedJob_CannotBeReExecuted_StatusGuard()
    {
        await using var db = fixture.CreateDbContext(ImportIntegrationFixture.TenantDev);

        var job = ImportJob.Create("HistoricalPayrollImport", "done.csv", "file-done", "hash-done", "admin");
        job.TransitionTo(ImportJobStatus.Validated);
        job.TransitionTo(ImportJobStatus.Queued);
        job.TransitionTo(ImportJobStatus.Processing);
        job.TransitionTo(ImportJobStatus.Completed);
        db.ImportJobs.Add(job);
        await db.SaveChangesAsync();

        // Reload and attempt to re-queue
        var loaded = await db.ImportJobs.FindAsync(job.Id);
        var act = () => loaded!.TransitionTo(ImportJobStatus.Queued);
        act.Should().Throw<InvalidOperationException>("completed jobs must be terminal");
    }

    [DockerRequiredFact]
    public async Task FailedJob_CannotBeReExecuted_StatusGuard()
    {
        await using var db = fixture.CreateDbContext(ImportIntegrationFixture.TenantDev);

        var job = ImportJob.Create("EmployeeImport", "bad.csv", "file-bad", "hash-bad", "admin");
        job.TransitionTo(ImportJobStatus.Validated);
        job.TransitionTo(ImportJobStatus.Queued);
        job.TransitionTo(ImportJobStatus.Processing);
        job.TransitionTo(ImportJobStatus.Failed);
        db.ImportJobs.Add(job);
        await db.SaveChangesAsync();

        var loaded = await db.ImportJobs.FindAsync(job.Id);
        var act = () => loaded!.TransitionTo(ImportJobStatus.Processing);
        act.Should().Throw<InvalidOperationException>();
    }

    [DockerRequiredFact]
    public async Task CancelledJob_CannotBeReQueued_StatusGuard()
    {
        await using var db = fixture.CreateDbContext(ImportIntegrationFixture.TenantDev);

        var job = ImportJob.Create("LeaveBalanceImport", "cancel.csv", "file-cancel", "hash-cancel", "admin");
        job.TransitionTo(ImportJobStatus.Validated);
        job.TransitionTo(ImportJobStatus.Cancelled);
        db.ImportJobs.Add(job);
        await db.SaveChangesAsync();

        var loaded = await db.ImportJobs.FindAsync(job.Id);
        var act = () => loaded!.TransitionTo(ImportJobStatus.Queued);
        act.Should().Throw<InvalidOperationException>();
    }

    // ── HistoricalPayrollSummary unique constraint ────────────────────────────

    [DockerRequiredFact]
    public async Task HistoricalPayrollSummary_DuplicateEmployeeMonthYear_ViolatesUniqueConstraint()
    {
        await using var db = fixture.CreateDbContext(ImportIntegrationFixture.TenantAcme);

        var job = ImportJob.Create("HistoricalPayrollImport", "payroll.csv", "file-pay-dup", "hash-pay-dup", "admin");
        db.ImportJobs.Add(job);
        await db.SaveChangesAsync();

        var summary1 = HistoricalPayrollSummary.Create(job.Id, "EMP001", 3, 2024, 50000m, 8000m, 42000m);
        var summary2 = HistoricalPayrollSummary.Create(job.Id, "EMP001", 3, 2024, 50000m, 8000m, 42000m);

        db.HistoricalPayrollSummaries.Add(summary1);
        db.HistoricalPayrollSummaries.Add(summary2);

        var act = async () => await db.SaveChangesAsync();
        await act.Should().ThrowAsync<Exception>("duplicate employee/month/year combination violates unique index");
    }

    // ── Progress tracking survives partial retries ────────────────────────────

    [DockerRequiredFact]
    public async Task UpdateProgress_CalledMultipleTimes_ReflectsLastWrite()
    {
        await using var db = fixture.CreateDbContext(ImportIntegrationFixture.TenantDev);

        var job = ImportJob.Create("SalaryComponentImport", "sal.csv", "file-sal-prog", "hash-sal-prog", "admin");
        db.ImportJobs.Add(job);
        await db.SaveChangesAsync();

        // Simulate two progress updates (e.g., after each batch)
        job.UpdateProgress(500, 2, 0);
        await db.SaveChangesAsync();

        job.UpdateProgress(950, 10, 0);
        await db.SaveChangesAsync();

        var loaded = await db.ImportJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == job.Id);
        loaded!.SuccessfulRows.Should().Be(950);
        loaded.FailedRows.Should().Be(10);
        loaded.TotalRows.Should().Be(960);
    }

    // ── Import records for failed rows persist across restarts ───────────────

    [DockerRequiredFact]
    public async Task ImportRecords_PersistedForFailedRows_SurviveDbRoundTrip()
    {
        await using var db = fixture.CreateDbContext(ImportIntegrationFixture.TenantDev);

        var job = ImportJob.Create("EmployeeImport", "emp.csv", "file-emp-rec", "hash-emp-rec", "admin");
        db.ImportJobs.Add(job);
        await db.SaveChangesAsync();

        for (var row = 1; row <= 5; row++)
        {
            var rec = ImportRecord.Create(job.Id, row, $"{{\"row\":{row}}}");
            rec.MarkFailed($"Validation error on row {row}");
            db.ImportRecords.Add(rec);
        }
        await db.SaveChangesAsync();

        // Re-open context to simulate worker restart
        await using var db2 = fixture.CreateDbContext(ImportIntegrationFixture.TenantDev);
        var records = await db2.ImportRecords.AsNoTracking()
            .Where(r => r.ImportJobId == job.Id)
            .OrderBy(r => r.RowNumber)
            .ToListAsync();

        records.Should().HaveCount(5);
        records.All(r => r.Status == ImportRecordStatus.Failed).Should().BeTrue();
        records[2].ErrorMessage.Should().Be("Validation error on row 3");
    }

    // ── Cross-tenant idempotency ──────────────────────────────────────────────

    [DockerRequiredFact]
    public async Task SameFileHash_ForDifferentTenants_StoredIndependently()
    {
        // The (TenantId, FileHash) index is not globally unique — different tenants can import the same file.
        const string sharedHash = "SHAREDHASH9999";

        await using (var dbDev = fixture.CreateDbContext(ImportIntegrationFixture.TenantDev))
        {
            var job = ImportJob.Create("EmployeeImport", "shared.csv", "fdev", sharedHash, "admin");
            dbDev.ImportJobs.Add(job);
            await dbDev.SaveChangesAsync();
        }

        await using (var dbAcme = fixture.CreateDbContext(ImportIntegrationFixture.TenantAcme))
        {
            var job = ImportJob.Create("EmployeeImport", "shared.csv", "facme", sharedHash, "admin");
            dbAcme.ImportJobs.Add(job);
            await dbAcme.SaveChangesAsync();
        }

        await using (var dbDev = fixture.CreateDbContext(ImportIntegrationFixture.TenantDev))
        {
            var count = await dbDev.ImportJobs.CountAsync(j => j.FileHash == sharedHash);
            count.Should().Be(1, "dev should see only its own job");
        }

        await using (var dbAcme = fixture.CreateDbContext(ImportIntegrationFixture.TenantAcme))
        {
            var count = await dbAcme.ImportJobs.CountAsync(j => j.FileHash == sharedHash);
            count.Should().Be(1, "acme should see only its own job");
        }
    }
}
