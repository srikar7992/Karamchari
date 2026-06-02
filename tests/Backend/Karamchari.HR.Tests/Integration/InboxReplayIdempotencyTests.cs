using FluentAssertions;
using Karamchari.HR.Consumers;
using Karamchari.Recruitment.Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace Karamchari.HR.Tests.Integration;

/// <summary>
/// Proves that <see cref="CreateEmployeeOnCandidateHiredConsumer"/> is idempotent
/// under duplicate / replayed message delivery.
///
/// Prior to the idempotency guard fix, the consumer blindly called
/// <c>dbContext.Employees.Add(employee)</c> on every invocation. Delivering the same
/// <see cref="CandidateHiredIntegrationEvent"/> N times would create N employee rows,
/// violating the single-employee-per-hire contract.
///
/// After the fix: an <c>AnyAsync</c> check on <c>EmployeeNumber</c> (deterministically
/// derived from <c>CandidateId</c>) short-circuits all duplicate deliveries. Exactly
/// one employee is materialized regardless of delivery count.
/// </summary>
public sealed class InboxReplayIdempotencyTests
{
    // ── 10× replay — exactly 1 employee created ─────────────────────────────

    [Fact]
    public async Task SameEventDeliveredTenTimes_ShouldMaterializeExactlyOneEmployee()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var candidateId = Guid.NewGuid();
        var message = BuildMessage(candidateId);

        // Act: deliver the same CandidateHiredIntegrationEvent 10 times to the same DB
        for (var i = 0; i < 10; i++)
        {
            await using var dbContext = HRDbContextFactory.Create("tenant-inbox", dbName);
            var consumer = new CreateEmployeeOnCandidateHiredConsumer(dbContext);
            var consumeCtx = BuildConsumeContext(message);
            await consumer.Consume(consumeCtx);
        }

        // Assert: exactly 1 employee in DB
        await using var readCtx = HRDbContextFactory.Create("tenant-inbox", dbName);
        var employees = await readCtx.Employees.IgnoreQueryFilters().ToListAsync();

        employees.Should().HaveCount(1,
            because: "duplicate event delivery must be idempotent — exactly-once semantics");

        var emp = employees.Single();
        var expectedNumber = $"EMP-{candidateId.ToString()[..8].ToUpperInvariant()}";
        emp.EmployeeNumber.Should().Be(expectedNumber);
        emp.LegalName.Should().Be($"{message.FirstName} {message.LastName}");
    }

    // ── 1× delivery — basic happy path still works ───────────────────────────

    [Fact]
    public async Task SingleDelivery_ShouldCreateEmployee()
    {
        // Arrange
        await using var dbContext = HRDbContextFactory.Create("tenant-single");
        var candidateId = Guid.NewGuid();
        var message = BuildMessage(candidateId);
        var consumer = new CreateEmployeeOnCandidateHiredConsumer(dbContext);
        var consumeCtx = BuildConsumeContext(message);

        // Act
        await consumer.Consume(consumeCtx);

        // Assert
        var employees = await dbContext.Employees.IgnoreQueryFilters().ToListAsync();
        employees.Should().HaveCount(1);
        employees.Single().WorkEmail.Should().Be(message.Email);
    }

    // ── 50× concurrent replay — inbox under load ────────────────────────────

    [Fact]
    public async Task FiftyConcurrentReplays_ShouldMaterializeExactlyOneEmployee()
    {
        // Arrange: all 50 tasks share the same in-memory database name.
        // In-memory EF Core is thread-safe for concurrent readers/writers within
        // a process, so this verifies application-layer idempotency (not DB constraint race).
        var dbName = Guid.NewGuid().ToString();
        var candidateId = Guid.NewGuid();
        var message = BuildMessage(candidateId);

        // Warm up: first delivery ensures the record exists before concurrent flood
        await using (var warmupCtx = HRDbContextFactory.Create("tenant-concurrent", dbName))
        {
            var warmupConsumer = new CreateEmployeeOnCandidateHiredConsumer(warmupCtx);
            await warmupConsumer.Consume(BuildConsumeContext(message));
        }

        // Act: 50 parallel consumers all try to process the same event
        var tasks = Enumerable.Range(0, 50).Select(async _ =>
        {
            await using var dbContext = HRDbContextFactory.Create("tenant-concurrent", dbName);
            var consumer = new CreateEmployeeOnCandidateHiredConsumer(dbContext);
            await consumer.Consume(BuildConsumeContext(message));
        });

        await Task.WhenAll(tasks);

        // Assert: still exactly 1 employee
        await using var readCtx = HRDbContextFactory.Create("tenant-concurrent", dbName);
        var employees = await readCtx.Employees.IgnoreQueryFilters().ToListAsync();

        employees.Should().HaveCount(1,
            because: "50 concurrent replays must not create duplicate employees");
    }

    // ── Different candidates — each gets their own employee ──────────────────

    [Fact]
    public async Task DifferentCandidates_EachGetDistinctEmployee()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var candidateIds = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToList();

        // Act: 5 distinct candidates hired, each event delivered twice (idempotent)
        foreach (var candidateId in candidateIds)
        {
            for (var i = 0; i < 2; i++)
            {
                await using var dbContext = HRDbContextFactory.Create("tenant-multi", dbName);
                var consumer = new CreateEmployeeOnCandidateHiredConsumer(dbContext);
                await consumer.Consume(BuildConsumeContext(BuildMessage(candidateId)));
            }
        }

        // Assert: exactly 5 employees (not 10)
        await using var readCtx = HRDbContextFactory.Create("tenant-multi", dbName);
        var employees = await readCtx.Employees.IgnoreQueryFilters().ToListAsync();
        employees.Should().HaveCount(5,
            because: "5 distinct candidates × 2 replays = exactly 5 employees");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static CandidateHiredIntegrationEvent BuildMessage(Guid candidateId) =>
        new(
            CandidateId: candidateId,
            ApplicationId: Guid.NewGuid(),
            RequisitionId: Guid.NewGuid(),
            TenantId: "tenant-inbox",
            FirstName: "Jane",
            LastName: "Doe",
            Email: "jane.doe@example.com",
            PhoneNumber: "+1-555-0100",
            HiredAt: DateTimeOffset.UtcNow,
            HiredBy: "admin@tenant.local",
            BaseSalary: 95_000m,
            Currency: "USD");

    private static ConsumeContext<CandidateHiredIntegrationEvent> BuildConsumeContext(
        CandidateHiredIntegrationEvent message)
    {
        var ctx = Substitute.For<ConsumeContext<CandidateHiredIntegrationEvent>>();
        ctx.Message.Returns(message);
        ctx.CancellationToken.Returns(CancellationToken.None);
        return ctx;
    }
}
