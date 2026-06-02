using FluentAssertions;
using Karamchari.Core.Messaging;
using Karamchari.Recruitment.Application.EventHandlers.Analytics;
using Karamchari.Recruitment.Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Karamchari.Recruitment.Tests.Analytics;

#pragma warning disable CA1707 // Underscores in test names

public sealed class AnalyticsRuntimeTests : RecruitmentIntegrationTestBase
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private RecruitmentVelocityConsumer CreateVelocityConsumer()
        => new(DbContext, NullLogger<RecruitmentVelocityConsumer>.Instance);

    private TimeToHireConsumer CreateTimeToHireConsumer()
        => new(DbContext, NullLogger<TimeToHireConsumer>.Instance);

    private HiringFunnelConsumer CreateFunnelConsumer()
        => new(DbContext, NullLogger<HiringFunnelConsumer>.Instance);

    private static ConsumeContext<EnterpriseEventEnvelope<T>> MakeContext<T>(
        EnterpriseEventEnvelope<T> envelope) where T : class
    {
        var ctx = Substitute.For<ConsumeContext<EnterpriseEventEnvelope<T>>>();
        ctx.Message.Returns(envelope);
        ctx.CancellationToken.Returns(CancellationToken.None);
        return ctx;
    }

    private static EnterpriseEventEnvelope<T> MakeEnvelope<T>(T payload, string tenantId) where T : class
        => EnterpriseEventEnvelope<T>.Wrap(payload, tenantId, typeof(T).Name);

    // -------------------------------------------------------------------------
    // Full journey
    // -------------------------------------------------------------------------

    [Fact]
    public async Task FullRecruitmentJourney_MaterializesAnalyticsRows()
    {
        // Arrange — shared IDs
        var requisitionId = Guid.NewGuid();
        var candidateId = Guid.NewGuid();
        var applicationId = Guid.NewGuid();
        var offerId = Guid.NewGuid();
        var interviewId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var velocityConsumer = CreateVelocityConsumer();
        var timeToHireConsumer = CreateTimeToHireConsumer();
        var funnelConsumer = CreateFunnelConsumer();

        // 1. RequisitionCreated (RequisitionPublishedIntegrationEvent)
        await velocityConsumer.Consume(MakeContext(MakeEnvelope(
            new RequisitionPublishedIntegrationEvent(
                requisitionId, TestTenantId, "Software Engineer", "dept-1", now),
            TestTenantId)));

        // 2. ApplicationSubmitted (CandidateAppliedIntegrationEvent)
        await funnelConsumer.Consume(MakeContext(MakeEnvelope(
            new CandidateAppliedIntegrationEvent(
                applicationId, candidateId, requisitionId,
                TestTenantId, "Jane Doe", "Software Engineer", now.AddMinutes(10)),
            TestTenantId)));

        // 3. InterviewCompleted (InterviewCompletedIntegrationEvent)
        await funnelConsumer.Consume(MakeContext(MakeEnvelope(
            new InterviewCompletedIntegrationEvent(
                interviewId, applicationId, TestTenantId, now.AddDays(7)),
            TestTenantId)));

        // 4. OfferAccepted — both TimeToHireConsumer and HiringFunnelConsumer listen to this.
        //    Each consumer guards its own idempotency key, so each writes exactly 1 row.
        //    The OfferAccepted deduplication key in TimeToHireConsumer is ApplicationId.
        //    The OfferAccepted deduplication key in HiringFunnelConsumer is also ApplicationId.
        //    Both share the same eventType string "OfferAccepted", so only 1 row is written
        //    total (the second consumer finds it already present).
        await timeToHireConsumer.Consume(MakeContext(MakeEnvelope(
            new OfferAcceptedIntegrationEvent(
                offerId, applicationId, TestTenantId, now.AddDays(14)),
            TestTenantId)));

        await funnelConsumer.Consume(MakeContext(MakeEnvelope(
            new OfferAcceptedIntegrationEvent(
                offerId, applicationId, TestTenantId, now.AddDays(14)),
            TestTenantId)));

        // 5. CandidateHired (CandidateHiredIntegrationEvent)
        await timeToHireConsumer.Consume(MakeContext(MakeEnvelope(
            new CandidateHiredIntegrationEvent(
                candidateId, applicationId, requisitionId, TestTenantId,
                "Jane", "Doe", "jane@example.com", null,
                now.AddDays(15), "recruiter-1", 120000m, "USD"),
            TestTenantId)));

        // Assert — 5 distinct event types, 5 rows
        var rows = await DbContext.AnalyticsReadModels.ToListAsync();
        rows.Should().HaveCount(5, "one row per distinct event type in the journey");

        var eventTypes = rows.Select(r => r.EventType).ToHashSet();
        eventTypes.Should().Contain("RequisitionCreated");
        eventTypes.Should().Contain("ApplicationSubmitted");
        eventTypes.Should().Contain("InterviewCompleted");
        eventTypes.Should().Contain("OfferAccepted");
        eventTypes.Should().Contain("CandidateHired");

        rows.Should().OnlyContain(r => r.TenantId == TestTenantId,
            "all rows must carry the correct TenantId");

        // Idempotency guard: dispatch the same OfferAccepted again — row count must not grow
        await timeToHireConsumer.Consume(MakeContext(MakeEnvelope(
            new OfferAcceptedIntegrationEvent(
                offerId, applicationId, TestTenantId, now.AddDays(14)),
            TestTenantId)));

        var rowsAfterDuplicate = await DbContext.AnalyticsReadModels.ToListAsync();
        rowsAfterDuplicate.Should().HaveCount(5, "duplicate delivery must not create extra rows");
    }

    // -------------------------------------------------------------------------
    // Duplicate event suppression
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DuplicateEventDelivery_Suppressed()
    {
        var candidateId = Guid.NewGuid();
        var applicationId = Guid.NewGuid();
        var requisitionId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var consumer = CreateTimeToHireConsumer();

        var envelope = MakeEnvelope(
            new CandidateHiredIntegrationEvent(
                candidateId, applicationId, requisitionId, TestTenantId,
                "John", "Smith", "john@example.com", null,
                now, "recruiter-2", 90000m, "USD"),
            TestTenantId);

        // First delivery
        await consumer.Consume(MakeContext(envelope));

        // Second delivery — same payload
        await consumer.Consume(MakeContext(envelope));

        var rows = await DbContext.AnalyticsReadModels.ToListAsync();
        rows.Should().HaveCount(1, "duplicate CandidateHired must be suppressed to exactly 1 row");
    }

    // -------------------------------------------------------------------------
    // Replay behaviour
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ReplayBehavior_CorrectRowCount()
    {
        var requisitionId = Guid.NewGuid();
        var candidateId = Guid.NewGuid();
        var applicationId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var velocityConsumer = CreateVelocityConsumer();
        var funnelConsumer = CreateFunnelConsumer();

        var reqEnvelope = MakeEnvelope(
            new RequisitionPublishedIntegrationEvent(
                requisitionId, TestTenantId, "QA Engineer", "dept-2", now),
            TestTenantId);

        var appliedEnvelope = MakeEnvelope(
            new CandidateAppliedIntegrationEvent(
                applicationId, candidateId, requisitionId,
                TestTenantId, "Bob Builder", "QA Engineer", now.AddMinutes(5)),
            TestTenantId);

        var interviewEnvelope = MakeEnvelope(
            new InterviewCompletedIntegrationEvent(
                Guid.NewGuid(), applicationId, TestTenantId, now.AddDays(3)),
            TestTenantId);

        // First pass — 3 distinct events
        await velocityConsumer.Consume(MakeContext(reqEnvelope));
        await funnelConsumer.Consume(MakeContext(appliedEnvelope));
        await funnelConsumer.Consume(MakeContext(interviewEnvelope));

        var rowsAfterFirst = await DbContext.AnalyticsReadModels.ToListAsync();
        rowsAfterFirst.Should().HaveCount(3, "3 events in first pass must produce 3 rows");

        // Replay — same 3 events
        await velocityConsumer.Consume(MakeContext(reqEnvelope));
        await funnelConsumer.Consume(MakeContext(appliedEnvelope));
        await funnelConsumer.Consume(MakeContext(interviewEnvelope));

        var rowsAfterReplay = await DbContext.AnalyticsReadModels.ToListAsync();
        rowsAfterReplay.Should().HaveCount(3, "replay must not add duplicate rows");
    }
}
