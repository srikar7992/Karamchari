// -----------------------------------------------------------------------
// <copyright file="ConsumerReplayTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Core.Contracts.IntegrationEvents;
using Karamchari.Core.Contracts.IntegrationEvents.V1;
using Karamchari.Forecasting.Domain.Workforce;
using Karamchari.Forecasting.IntegrationTests.Infrastructure;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace Karamchari.Forecasting.IntegrationTests.Consumers;

/// <summary>
/// Operational resilience tests for WorkforcePlanningConsumer.
/// Validates the four failure modes that distinguish "works in CI" from "survives production":
///
///   1. Duplicate delivery — same event delivered twice → projection deducts/increments once
///   2. Replay (rebuild-from-scratch) — re-processing events after consumer restart → same result
///   3. Out-of-order delivery — SkillExpired arrives before SkillAssigned → no negative headcount
///   4. Idempotent HireOfferAccepted — same offer delivered twice → one WfpFutureHire row
///
/// Each test uses a unique TenantId and EmployeeId to prevent cross-test contamination
/// within the shared SQL Server container.
/// </summary>
[Collection(nameof(ForecastingIntegrationCollection))]
public sealed class ConsumerReplayTests(ForecastingSqlServerFixture fixture)
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ConsumeContext<T> FakeContext<T>(T message) where T : class
    {
        var ctx = Substitute.For<ConsumeContext<T>>();
        ctx.Message.Returns(message);
        ctx.CancellationToken.Returns(CancellationToken.None);
        return ctx;
    }

    private static EmployeeOnboardedIntegrationEventV1 OnboardedEvent(
        Guid employeeId, string tenantId) =>
        new(employeeId, tenantId,
            EmployeeNumber: "EMP-001",
            LegalName: "Test Employee",
            WorkEmail: null,
            HiredOn: new DateOnly(2025, 1, 1),
            DateOfBirth: new DateOnly(1985, 1, 1),
            ContractEndDate: null);

    private static EmployeeTerminatedIntegrationEventV1 TerminatedEvent(
        Guid employeeId, string tenantId) =>
        new(employeeId, tenantId,
            EmployeeNumber: "EMP-001",
            TerminatedOn: DateOnly.FromDateTime(DateTime.UtcNow),
            TerminationReason: "Resignation");

    private static SkillAssignedIntegrationEventV1 SkillAssignedEvent(
        Guid employeeId, string tenantId, string skillCode, DateOnly? expiryDate = null) =>
        new(employeeId, tenantId,
            SkillId: Guid.NewGuid(),
            SkillCode: skillCode,
            SkillName: skillCode,
            Level: 1,
            ExpiryDate: expiryDate);

    private static SkillExpiredIntegrationEventV1 SkillExpiredEvent(
        Guid employeeId, string tenantId, string skillCode) =>
        new(employeeId, tenantId,
            SkillId: Guid.NewGuid(),
            SkillCode: skillCode);

    private static HireOfferAcceptedIntegrationEventV1 OfferAcceptedEvent(
        Guid offerId, string tenantId, string skillCode,
        DateOnly startDate, decimal joiningProbability = 1.0m) =>
        new(offerId, tenantId, "DEFAULT", skillCode, startDate, joiningProbability);

    // ── Scenario 1: Duplicate EmployeeTerminated ──────────────────────────────

    [DockerRequiredFact]
    public async Task DuplicateTermination_ProjectionDecrementedOnce_NotTwice()
    {
        // Arrange: onboard employee → projection total = 1
        var tenantId = $"test-dup-term-{Guid.NewGuid():N}";
        var employeeId = Guid.NewGuid();

        await using var db1 = fixture.CreateDbContext();
        var consumer1 = fixture.CreateConsumer(db1);
        await consumer1.Consume(FakeContext(OnboardedEvent(employeeId, tenantId)));

        // Verify: total = 1
        await using var dbCheck1 = fixture.CreateDbContext();
        var proj1 = await dbCheck1.WorkforcePlanningProjections
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.SkillCode == "GENERAL");
        proj1.Should().NotBeNull();
        proj1!.TotalEmployees.Should().Be(1);

        // Act: terminate once
        await using var db2 = fixture.CreateDbContext();
        var consumer2 = fixture.CreateConsumer(db2);
        await consumer2.Consume(FakeContext(TerminatedEvent(employeeId, tenantId)));

        // Act: terminate AGAIN (duplicate delivery — consumer restart scenario)
        await using var db3 = fixture.CreateDbContext();
        var consumer3 = fixture.CreateConsumer(db3);
        await consumer3.Consume(FakeContext(TerminatedEvent(employeeId, tenantId)));

        // Assert: projection total = 0, not -1
        await using var dbCheck2 = fixture.CreateDbContext();
        var proj2 = await dbCheck2.WorkforcePlanningProjections
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.SkillCode == "GENERAL");
        proj2!.TotalEmployees.Should().Be(0,
            "second termination is a no-op because index.IsActive is already false");

        // Assert: employee index is inactive
        var index = await dbCheck2.WorkforceEmployeeIndex
            .FindAsync([employeeId]);
        index!.IsActive.Should().BeFalse();
    }

    // ── Scenario 2: Duplicate EmployeeOnboarded (replay after crash) ──────────

    [DockerRequiredFact]
    public async Task DuplicateOnboarded_IndexCreatedOnce_HeadcountNotDoubled()
    {
        var tenantId = $"test-dup-onb-{Guid.NewGuid():N}";
        var employeeId = Guid.NewGuid();

        // Act: deliver EmployeeOnboarded twice
        await using var db1 = fixture.CreateDbContext();
        var consumer1 = fixture.CreateConsumer(db1);
        await consumer1.Consume(FakeContext(OnboardedEvent(employeeId, tenantId)));

        await using var db2 = fixture.CreateDbContext();
        var consumer2 = fixture.CreateConsumer(db2);
        await consumer2.Consume(FakeContext(OnboardedEvent(employeeId, tenantId)));

        // Assert: exactly one index row
        await using var dbCheck = fixture.CreateDbContext();
        var indexRows = await dbCheck.WorkforceEmployeeIndex
            .Where(e => e.Id == employeeId)
            .CountAsync();
        indexRows.Should().Be(1, "idempotent guard: `if (existing != null) return` prevents double insert");

        // Assert: projection total = 1, not 2
        var proj = await dbCheck.WorkforcePlanningProjections
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.SkillCode == "GENERAL");
        proj!.TotalEmployees.Should().Be(1,
            "second EmployeeOnboarded exits early before incrementing projection");
    }

    // ── Scenario 3: Out-of-order SkillExpired before SkillAssigned ───────────

    [DockerRequiredFact]
    public async Task OutOfOrder_SkillExpiredBeforeAssigned_NoNegativeHeadcount()
    {
        var tenantId = $"test-ooo-skill-{Guid.NewGuid():N}";
        var employeeId = Guid.NewGuid();
        const string skillCode = "FORKLIFT";

        // Arrange: onboard employee (projection total = 1 for GENERAL)
        await using var db1 = fixture.CreateDbContext();
        var consumer1 = fixture.CreateConsumer(db1);
        await consumer1.Consume(FakeContext(OnboardedEvent(employeeId, tenantId)));

        // Act: deliver SkillExpired BEFORE SkillAssigned (out-of-order delivery)
        // Consumer guard: index.SkillCodes.Contains(skillCode) → false → hadSkill = false → no decrement
        await using var db2 = fixture.CreateDbContext();
        var consumer2 = fixture.CreateConsumer(db2);
        await consumer2.Consume(FakeContext(SkillExpiredEvent(employeeId, tenantId, skillCode)));

        // Verify: FORKLIFT projection doesn't exist yet or has total = 0
        await using var dbCheck1 = fixture.CreateDbContext();
        var projBeforeAssign = await dbCheck1.WorkforcePlanningProjections
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.SkillCode == skillCode);
        // Either doesn't exist or has 0 — not negative
        var totalBefore = projBeforeAssign?.TotalEmployees ?? 0;
        totalBefore.Should().BeGreaterThanOrEqualTo(0,
            "SkillExpired before SkillAssigned must not produce negative headcount");

        // Act: now deliver SkillAssigned (the event that should have come first)
        await using var db3 = fixture.CreateDbContext();
        var consumer3 = fixture.CreateConsumer(db3);
        await consumer3.Consume(FakeContext(SkillAssignedEvent(employeeId, tenantId, skillCode)));

        // Assert: FORKLIFT projection now has total = 1
        await using var dbCheck2 = fixture.CreateDbContext();
        var projAfterAssign = await dbCheck2.WorkforcePlanningProjections
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.SkillCode == skillCode);
        projAfterAssign.Should().NotBeNull();
        projAfterAssign!.TotalEmployees.Should().Be(1,
            "SkillAssigned increments projection correctly regardless of earlier out-of-order SkillExpired");
    }

    // ── Scenario 4: Replay SkillAssigned (duplicate skill assignment) ─────────

    [DockerRequiredFact]
    public async Task DuplicateSkillAssigned_ProjectionIncrementedOnce()
    {
        var tenantId = $"test-dup-skill-{Guid.NewGuid():N}";
        var employeeId = Guid.NewGuid();
        const string skillCode = "WELDER";

        // Arrange: onboard
        await using var db1 = fixture.CreateDbContext();
        await fixture.CreateConsumer(db1).Consume(FakeContext(OnboardedEvent(employeeId, tenantId)));

        // Act: assign WELDER twice (duplicate delivery)
        await using var db2 = fixture.CreateDbContext();
        await fixture.CreateConsumer(db2).Consume(FakeContext(SkillAssignedEvent(employeeId, tenantId, skillCode)));

        await using var db3 = fixture.CreateDbContext();
        await fixture.CreateConsumer(db3).Consume(FakeContext(SkillAssignedEvent(employeeId, tenantId, skillCode)));

        // Assert: WELDER projection total = 1 (not 2)
        await using var dbCheck = fixture.CreateDbContext();
        var proj = await dbCheck.WorkforcePlanningProjections
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.SkillCode == skillCode);
        proj!.TotalEmployees.Should().Be(1,
            "`alreadyHadSkill = true` on second SkillAssigned prevents double increment");

        // Assert: employee index has WELDER exactly once
        var index = await dbCheck.WorkforceEmployeeIndex.FindAsync([employeeId]);
        index!.SkillCodes.Should().ContainSingle(s => s == skillCode);
    }

    // ── Scenario 5: Idempotent HireOfferAccepted ──────────────────────────────

    [DockerRequiredFact]
    public async Task DuplicateHireOfferAccepted_OnlyOneFutureHireRow()
    {
        var tenantId = $"test-dup-offer-{Guid.NewGuid():N}";
        var offerId = Guid.NewGuid();
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(60));

        // Act: deliver same offer twice
        await using var db1 = fixture.CreateDbContext();
        await fixture.CreateConsumer(db1).Consume(
            FakeContext(OfferAcceptedEvent(offerId, tenantId, "ENGINEER", startDate, 0.80m)));

        await using var db2 = fixture.CreateDbContext();
        await fixture.CreateConsumer(db2).Consume(
            FakeContext(OfferAcceptedEvent(offerId, tenantId, "ENGINEER", startDate, 0.80m)));

        // Assert: one row, correct probability
        await using var dbCheck = fixture.CreateDbContext();
        var hires = await dbCheck.WfpFutureHires
            .Where(h => h.OfferId == offerId)
            .ToListAsync();
        hires.Should().HaveCount(1, "idempotent guard: `if (existing) return` prevents duplicate");
        hires[0].JoiningProbability.Should().Be(0.80m);
    }

    // ── Scenario 6: Full lifecycle rebuild produces consistent projection ──────

    [DockerRequiredFact]
    public async Task ProjectionRebuild_TwoEmployeesTwoTerminations_FinalTotalMatchesExpected()
    {
        // Simulates rebuild-from-scratch: process a complete sequence of events.
        // Verifies the projection is in the expected state after full replay.
        var tenantId = $"test-rebuild-{Guid.NewGuid():N}";
        var emp1 = Guid.NewGuid();
        var emp2 = Guid.NewGuid();

        // Onboard 2 employees
        await using (var db = fixture.CreateDbContext())
        {
            var c = fixture.CreateConsumer(db);
            await c.Consume(FakeContext(OnboardedEvent(emp1, tenantId)));
            await c.Consume(FakeContext(OnboardedEvent(emp2, tenantId)));
        }

        // Terminate 1
        await using (var db = fixture.CreateDbContext())
        {
            await fixture.CreateConsumer(db)
                .Consume(FakeContext(TerminatedEvent(emp1, tenantId)));
        }

        // Assert: GENERAL projection total = 1
        await using var dbCheck = fixture.CreateDbContext();
        var proj = await dbCheck.WorkforcePlanningProjections
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.SkillCode == "GENERAL");
        proj!.TotalEmployees.Should().Be(1,
            "2 onboarded − 1 terminated = 1 remaining");

        // Also verify both index rows reflect correct state
        var indices = await dbCheck.WorkforceEmployeeIndex
            .Where(e => e.Id == emp1 || e.Id == emp2)
            .ToListAsync();
        indices.Single(i => i.Id == emp1).IsActive.Should().BeFalse();
        indices.Single(i => i.Id == emp2).IsActive.Should().BeTrue();
    }
}
