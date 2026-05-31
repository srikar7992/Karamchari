using System.Text;
using FluentAssertions;
using Karamchari.DataMigration.Consumers;
using Karamchari.DataMigration.Contracts.Events;
using Karamchari.DataMigration.Domain.Importing;
using Karamchari.DataMigration.Features.Employees;
using Karamchari.DataMigration.Services;
using Karamchari.DataMigration.Tests.Integration.Fixtures;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Karamchari.DataMigration.Tests.Integration;

/// <summary>
/// Tests the async import chain: queued event → consumer → pipeline → database.
/// Tests the consumer directly with a mock ConsumeContext to verify the message-driven
/// processing chain without requiring a live broker.
///
/// Full broker integration (RabbitMQ end-to-end) requires docker-compose up
/// and is a separate deployment-level test.
/// </summary>
[Collection(nameof(ImportIntegrationCollection))]
public sealed class AsyncChainTests(ImportIntegrationFixture fixture)
{
    // ── Consumer dispatches via pipeline ─────────────────────────────────────

    [DockerRequiredFact]
    public async Task Consumer_ValidJob_TransitionsToProcessingThenCompleted()
    {
        await using var db = fixture.CreateDbContext(ImportIntegrationFixture.TenantDev);

        // Set up a job with a real CSV stored in the file store
        var fileStore = new InMemoryFileStore();
        var csv = "Employee Number,First Name,Last Name,Email,Department,Designation,Manager Employee Number,Date Of Joining,Employment Type\n" +
                  "EMP001,Alice,Johnson,alice@dev.local,,,,,Full-Time\n";
        var fileId = await fileStore.SaveFileAsync(new MemoryStream(Encoding.UTF8.GetBytes(csv)), "employees.csv");

        var job = ImportJob.Create("EmployeeImport", "employees.csv", fileId, "hash-async-001", "admin");
        job.TransitionTo(ImportJobStatus.Validated);
        job.TransitionTo(ImportJobStatus.Queued);
        db.ImportJobs.Add(job);
        await db.SaveChangesAsync();

        // Build the pipeline with a mocked publish endpoint
        var publishEndpoint = Substitute.For<IPublishEndpoint>();
        var resolver = Substitute.For<IReferenceResolver>();
        resolver.ResolveEmployeeIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Guid.NewGuid());
        resolver.ResolveDepartmentIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Guid?)null);

        var pipeline = new ImportPipeline<EmployeeImportRecord>(
            new EmployeeImportMapper(),
            new EmployeeImportValidator(resolver),
            new EmployeeImportProcessor(publishEndpoint),
            NullLogger<ImportPipeline<EmployeeImportRecord>>.Instance);

        var consumer = new ImportJobQueuedConsumer(
            db,
            fileStore,
            new IImportFileParser[] { new CsvImportFileParser() },
            new IImportPipeline[] { pipeline },
            NullLogger<ImportJobQueuedConsumer>.Instance);

        var ctx = BuildConsumeContext(new ImportJobQueued(job.Id, "dev", "EmployeeImport"));
        await consumer.Consume(ctx);

        var loaded = await db.ImportJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == job.Id);
        loaded!.Status.Should().Be(ImportJobStatus.Completed);
        loaded.SuccessfulRows.Should().Be(1);
    }

    [DockerRequiredFact]
    public async Task Consumer_UnknownImportType_TransitionsToFailed()
    {
        await using var db = fixture.CreateDbContext(ImportIntegrationFixture.TenantDev);

        var fileStore = new InMemoryFileStore();
        var fileId = await fileStore.SaveFileAsync(new MemoryStream("header\nrow"u8.ToArray()), "x.csv");

        var job = ImportJob.Create("UnknownType", "x.csv", fileId, "hash-unknown", "admin");
        job.TransitionTo(ImportJobStatus.Validated);
        job.TransitionTo(ImportJobStatus.Queued);
        db.ImportJobs.Add(job);
        await db.SaveChangesAsync();

        var consumer = new ImportJobQueuedConsumer(
            db,
            fileStore,
            new IImportFileParser[] { new CsvImportFileParser() },
            Array.Empty<IImportPipeline>(), // no pipelines registered
            NullLogger<ImportJobQueuedConsumer>.Instance);

        await consumer.Consume(BuildConsumeContext(new ImportJobQueued(job.Id, "dev", "UnknownType")));

        var loaded = await db.ImportJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == job.Id);
        loaded!.Status.Should().Be(ImportJobStatus.Failed, "no pipeline found → Failed");
    }

    [DockerRequiredFact]
    public async Task Consumer_JobNotFound_DoesNotThrow()
    {
        await using var db = fixture.CreateDbContext(ImportIntegrationFixture.TenantDev);

        var consumer = new ImportJobQueuedConsumer(
            db,
            new InMemoryFileStore(),
            Array.Empty<IImportFileParser>(),
            Array.Empty<IImportPipeline>(),
            NullLogger<ImportJobQueuedConsumer>.Instance);

        var act = async () => await consumer.Consume(
            BuildConsumeContext(new ImportJobQueued(Guid.NewGuid(), "dev", "EmployeeImport")));

        await act.Should().NotThrowAsync("missing job should be silently skipped");
    }

    [DockerRequiredFact]
    public async Task Consumer_MissingParser_TransitionsToFailed()
    {
        await using var db = fixture.CreateDbContext(ImportIntegrationFixture.TenantDev);

        // Store a .xlsx file (no parser registered for that extension)
        var fileStore = new InMemoryFileStore();
        var fileId = await fileStore.SaveFileAsync(new MemoryStream("data"u8.ToArray()), "employees.xlsx");

        var job = ImportJob.Create("EmployeeImport", "employees.xlsx", fileId, "hash-xlsx", "admin");
        job.TransitionTo(ImportJobStatus.Validated);
        job.TransitionTo(ImportJobStatus.Queued);
        db.ImportJobs.Add(job);
        await db.SaveChangesAsync();

        var consumer = new ImportJobQueuedConsumer(
            db,
            fileStore,
            Array.Empty<IImportFileParser>(), // no CSV parser
            new IImportPipeline[] { BuildNoOpPipeline("EmployeeImport") },
            NullLogger<ImportJobQueuedConsumer>.Instance);

        await consumer.Consume(BuildConsumeContext(new ImportJobQueued(job.Id, "dev", "EmployeeImport")));

        var loaded = await db.ImportJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == job.Id);
        loaded!.Status.Should().Be(ImportJobStatus.Failed);
    }

    [DockerRequiredFact]
    public async Task Consumer_AllRowsFailValidation_CompletedWithErrors()
    {
        await using var db = fixture.CreateDbContext(ImportIntegrationFixture.TenantDev);

        var fileStore = new InMemoryFileStore();
        var csv = "Employee Number,First Name,Last Name,Email\n" +
                  ",Alice,Johnson,alice@dev.local\n" +  // no emp number
                  ",Bob,Smith,bob@dev.local\n";         // no emp number
        var fileId = await fileStore.SaveFileAsync(new MemoryStream(Encoding.UTF8.GetBytes(csv)), "bad.csv");

        var job = ImportJob.Create("EmployeeImport", "bad.csv", fileId, "hash-all-bad", "admin");
        job.TransitionTo(ImportJobStatus.Validated);
        job.TransitionTo(ImportJobStatus.Queued);
        db.ImportJobs.Add(job);
        await db.SaveChangesAsync();

        var resolver = Substitute.For<IReferenceResolver>();
        resolver.ResolveEmployeeIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Guid?)null);

        var pipeline = new ImportPipeline<EmployeeImportRecord>(
            new EmployeeImportMapper(),
            new EmployeeImportValidator(resolver),
            new EmployeeImportProcessor(Substitute.For<IPublishEndpoint>()),
            NullLogger<ImportPipeline<EmployeeImportRecord>>.Instance);

        var consumer = new ImportJobQueuedConsumer(
            db, fileStore,
            new IImportFileParser[] { new CsvImportFileParser() },
            new IImportPipeline[] { pipeline },
            NullLogger<ImportJobQueuedConsumer>.Instance);

        await consumer.Consume(BuildConsumeContext(new ImportJobQueued(job.Id, "dev", "EmployeeImport")));

        var loaded = await db.ImportJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == job.Id);
        loaded!.Status.Should().Be(ImportJobStatus.CompletedWithErrors);
        loaded.FailedRows.Should().Be(2);
        loaded.SuccessfulRows.Should().Be(0);
    }

    [DockerRequiredFact]
    public async Task Consumer_TenantIsolation_JobForDevNotAccessedByAcme()
    {
        // Verify that the consumer, when running under acme's db context,
        // cannot process dev's jobs (tenant isolation is enforced at the DB level)
        var devJob = ImportJob.Create("EmployeeImport", "dev.csv", "dev-file", "hash-dev-isolation", "admin@dev");
        devJob.TransitionTo(ImportJobStatus.Validated);
        devJob.TransitionTo(ImportJobStatus.Queued);

        await using (var dbDev = fixture.CreateDbContext(ImportIntegrationFixture.TenantDev))
        {
            dbDev.ImportJobs.Add(devJob);
            await dbDev.SaveChangesAsync();
        }

        // Consumer running under acme context cannot see dev's job
        await using var dbAcme = fixture.CreateDbContext(ImportIntegrationFixture.TenantAcme);
        var consumer = new ImportJobQueuedConsumer(
            dbAcme,
            new InMemoryFileStore(),
            Array.Empty<IImportFileParser>(),
            Array.Empty<IImportPipeline>(),
            NullLogger<ImportJobQueuedConsumer>.Instance);

        // Should silently skip — the job is not found under acme's context
        await consumer.Consume(BuildConsumeContext(new ImportJobQueued(devJob.Id, "dev", "EmployeeImport")));

        // Dev job status unchanged (consumer didn't touch it)
        await using var dbDev2 = fixture.CreateDbContext(ImportIntegrationFixture.TenantDev);
        var devJobAfter = await dbDev2.ImportJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == devJob.Id);
        devJobAfter!.Status.Should().Be(ImportJobStatus.Queued, "acme consumer must not process dev's job");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static ConsumeContext<ImportJobQueued> BuildConsumeContext(ImportJobQueued message)
    {
        var ctx = Substitute.For<ConsumeContext<ImportJobQueued>>();
        ctx.Message.Returns(message);
        return ctx;
    }

    private static IImportPipeline BuildNoOpPipeline(string importType)
    {
        var p = Substitute.For<IImportPipeline>();
        p.ImportType.Returns(importType);
        return p;
    }
}

/// <summary>
/// In-memory file store for tests that need a file to exist without a real file system.
/// </summary>
internal sealed class InMemoryFileStore : IImportFileStore
{
    private readonly Dictionary<string, byte[]> _files = new();

    public Task<string> SaveFileAsync(Stream stream, string fileName, CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        var id = Guid.NewGuid().ToString("N") + System.IO.Path.GetExtension(fileName);
        _files[id] = ms.ToArray();
        return Task.FromResult(id);
    }

    public Task<Stream> GetFileAsync(string fileId, CancellationToken ct = default)
    {
        if (!_files.TryGetValue(fileId, out var bytes))
            throw new FileNotFoundException($"File '{fileId}' not found in in-memory store.");
        return Task.FromResult<Stream>(new MemoryStream(bytes));
    }

    public Task DeleteFileAsync(string fileId, CancellationToken ct = default)
    {
        _files.Remove(fileId);
        return Task.CompletedTask;
    }
}
