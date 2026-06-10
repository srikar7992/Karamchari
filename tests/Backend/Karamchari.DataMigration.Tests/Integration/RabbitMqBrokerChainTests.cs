// -----------------------------------------------------------------------
// <copyright file="RabbitMqBrokerChainTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Multitenancy.Execution;
using Karamchari.DataMigration.Contracts.Events;
using Karamchari.DataMigration.Domain.Importing;
using Karamchari.DataMigration.Tests.Integration.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Karamchari.DataMigration.Tests.Integration;

/// <summary>
/// Full broker async-chain certification tests.
///
/// These tests verify the complete path:
///   1. Test publishes ImportJobQueued to a REAL RabbitMQ broker
///   2. ImportJobQueuedConsumer receives the message from the broker
///   3. Consumer processes the file via the import pipeline
///   4. Consumer persists the terminal job status to SQL Server
///   5. Test reads the final status back from the database and asserts
///
/// This is the transport-level certification that the existing AsyncChainTests
/// (which use a mock ConsumeContext) cannot provide.
/// </summary>
[Collection(nameof(RabbitMqBrokerCollection))]
public sealed class RabbitMqBrokerChainTests(RabbitMqBrokerFixture fixture)
{
    // ── Test CSV payloads ────────────────────────────────────────────────────

    private const string ValidEmployeeCsv = """
        Employee Number,First Name,Last Name,Email,Department,Designation,Manager Employee Number,Date Of Joining,Employment Type
        BRKR001,Alice,Smith,alice@broker.test,,,,2024-01-01,Full-Time
        BRKR002,Bob,Jones,bob@broker.test,,,,2024-02-01,Full-Time
        """;

    private const string InvalidEmployeeCsv = """
        Employee Number,First Name,Last Name,Email,Department,Designation,Manager Employee Number,Date Of Joining,Employment Type
        ,Alice,Smith,alice@broker.test,,,,2024-01-01,Full-Time
        BRKR003,,Jones,,,,,2024-01-01,Full-Time
        """;

    // ── C1: Happy path — valid CSV → Completed ────────────────────────────────

    [DockerRequiredFact]
    public async Task BrokerChain_ValidEmployeeImport_TransitionsToCompleted()
    {
        // 1. Pre-load the CSV into the shared file store
        var fileId = await fixture.SaveFileAsync(ValidEmployeeCsv, "employees.csv");

        // 2. Create the import job in the DB (Pending → Queued)
        Guid jobId;
        await using (var db = fixture.CreateDbContext())
        {
            var job = ImportJob.Create("EmployeeImport", "employees.csv", fileId,
                fileHash: "broker-chain-happy-path",
                createdBy: "broker-chain-test");

            // Validate first so the job can be executed (Pending → Validated → Queued)
            job.TransitionTo(ImportJobStatus.Validated);
            job.TransitionTo(ImportJobStatus.Queued);
            db.ImportJobs.Add(job);
            await db.SaveChangesAsync();
            jobId = job.Id;
        }

        // 3. Publish ImportJobQueued to the real RabbitMQ broker with the correct
        //    tenant execution context so TenantPublishFilter adds MT-Tenant-Id headers.
        var envelope = RabbitMqBrokerFixture.TenantDev;
        var ctx = new TenantExecutionContext(envelope);
        using (ctx.Establish())
        {
            await fixture.PublishEndpoint.Publish(
                new ImportJobQueued(jobId, "dev", "EmployeeImport"));
        }

        // 4. Poll until the consumer transitions the job to a terminal state (max 30s)
        var finalStatus = await fixture.WaitForTerminalStatusAsync(jobId);

        // 5. Assert: both employees had no validation errors → Completed (not CompletedWithErrors)
        finalStatus.Should().Be(ImportJobStatus.Completed,
            "both CSV rows are valid and employee onboarding commands are published successfully");
    }

    // ── C2: Invalid rows → CompletedWithErrors ────────────────────────────────

    [DockerRequiredFact]
    public async Task BrokerChain_CsvWithInvalidRows_TransitionsToCompletedWithErrors()
    {
        var fileId = await fixture.SaveFileAsync(InvalidEmployeeCsv, "invalid-employees.csv");

        Guid jobId;
        await using (var db = fixture.CreateDbContext())
        {
            var job = ImportJob.Create("EmployeeImport", "invalid-employees.csv", fileId,
                fileHash: "broker-chain-invalid-rows",
                createdBy: "broker-chain-test");
            job.TransitionTo(ImportJobStatus.Validated);
            job.TransitionTo(ImportJobStatus.Queued);
            db.ImportJobs.Add(job);
            await db.SaveChangesAsync();
            jobId = job.Id;
        }

        var ctx = new TenantExecutionContext(RabbitMqBrokerFixture.TenantDev);
        using (ctx.Establish())
        {
            await fixture.PublishEndpoint.Publish(
                new ImportJobQueued(jobId, "dev", "EmployeeImport"));
        }

        var finalStatus = await fixture.WaitForTerminalStatusAsync(jobId);

        finalStatus.Should().Be(ImportJobStatus.CompletedWithErrors,
            "both CSV rows are invalid (missing employee number / first name) so success=0, failed=2");
    }

    // ── C3: Unknown import type → Failed ────────────────────────────────────

    [DockerRequiredFact]
    public async Task BrokerChain_UnknownImportType_TransitionsToFailed()
    {
        var fileId = await fixture.SaveFileAsync(ValidEmployeeCsv, "data.csv");

        Guid jobId;
        await using (var db = fixture.CreateDbContext())
        {
            var job = ImportJob.Create("UnknownImportType", "data.csv", fileId,
                fileHash: "broker-chain-unknown-type",
                createdBy: "broker-chain-test");
            job.TransitionTo(ImportJobStatus.Validated);
            job.TransitionTo(ImportJobStatus.Queued);
            db.ImportJobs.Add(job);
            await db.SaveChangesAsync();
            jobId = job.Id;
        }

        var ctx = new TenantExecutionContext(RabbitMqBrokerFixture.TenantDev);
        using (ctx.Establish())
        {
            await fixture.PublishEndpoint.Publish(
                new ImportJobQueued(jobId, "dev", "UnknownImportType"));
        }

        var finalStatus = await fixture.WaitForTerminalStatusAsync(jobId);

        finalStatus.Should().Be(ImportJobStatus.Failed,
            "no pipeline is registered for 'UnknownImportType' so consumer transitions to Failed");
    }

    // ── C4: Progress counters reflect actual row outcomes ─────────────────────

    [DockerRequiredFact]
    public async Task BrokerChain_ValidImport_ProgressCountersAccurate()
    {
        var fileId = await fixture.SaveFileAsync(ValidEmployeeCsv, "employees-prog.csv");

        Guid jobId;
        await using (var db = fixture.CreateDbContext())
        {
            var job = ImportJob.Create("EmployeeImport", "employees-prog.csv", fileId,
                fileHash: "broker-chain-progress-check",
                createdBy: "broker-chain-test");
            job.TransitionTo(ImportJobStatus.Validated);
            job.TransitionTo(ImportJobStatus.Queued);
            db.ImportJobs.Add(job);
            await db.SaveChangesAsync();
            jobId = job.Id;
        }

        var ctx = new TenantExecutionContext(RabbitMqBrokerFixture.TenantDev);
        using (ctx.Establish())
        {
            await fixture.PublishEndpoint.Publish(
                new ImportJobQueued(jobId, "dev", "EmployeeImport"));
        }

        await fixture.WaitForTerminalStatusAsync(jobId);

        await using var readDb = fixture.CreateDbContext();
        var saved = await readDb.ImportJobs
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstAsync(j => j.Id == jobId);

        saved.SuccessfulRows.Should().Be(2, "both valid rows were processed");
        saved.FailedRows.Should().Be(0, "no rows had validation errors");
    }
}
