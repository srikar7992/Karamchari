// -----------------------------------------------------------------------
// <copyright file="RecruitmentReadQueryTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Recruitment.Application.Commands;
using Karamchari.Recruitment.Application.Queries;
using Karamchari.Recruitment.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ApplicationId = Karamchari.Recruitment.Domain.ApplicationId;

namespace Karamchari.Recruitment.Tests.Application;

#pragma warning disable CA1707 // Underscores in test names

/// <summary>
/// Verifies the Phase 1A recruitment read model — every read query returns the
/// expected projection after a representative end-to-end recruitment journey.
/// </summary>
public sealed class RecruitmentReadQueryTests : RecruitmentIntegrationTestBase
{
    [Fact]
    public async Task GetCandidatesQuery_ShouldReturnSeededCandidates_NewestFirst()
    {
        // Arrange
        var firstId = await new CreateCandidateHandler(DbContext).Handle(new CreateCandidateCommand("Alpha", "A", "alpha@example.com", null), default);
        var secondId = await new CreateCandidateHandler(DbContext).Handle(new CreateCandidateCommand("Beta", "B", "beta@example.com", "555-0001"), default);

        var handler = new GetCandidatesHandler(DbContext);

        // Act
        var result = await handler.Handle(new GetCandidatesQuery(), default);

        // Assert
        result.Should().HaveCount(2);
        result[0].Id.Should().Be(secondId.Value);
        result[1].Id.Should().Be(firstId.Value);
        result[0].FirstName.Should().Be("Beta");
        result[0].Email.Should().Be("beta@example.com");
        result[0].PhoneNumber.Should().Be("555-0001");
        result[1].FirstName.Should().Be("Alpha");
    }

    [Fact]
    public async Task GetCandidateDetailQuery_ShouldReturnFullProjection_WhenCandidateExists()
    {
        // Arrange — full journey with one candidate
        var (candidateId, applicationId, _, offerId) = await SeedHiredCandidateAsync();

        var handler = new GetCandidateDetailHandler(DbContext);

        // Act
        var result = await handler.Handle(new GetCandidateDetailQuery(candidateId), default);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(candidateId);
        result.FirstName.Should().Be("Carol");
        result.LastName.Should().Be("Hired");
        result.Applications.Should().HaveCount(1);
        result.Applications[0].Id.Should().Be(applicationId);
        result.Applications[0].Status.Should().Be("Hired");
        result.Interviews.Should().HaveCount(1);
        result.Offers.Should().HaveCount(1);
        result.Offers[0].Id.Should().Be(offerId);
        result.Offers[0].Status.Should().Be("Accepted");
        result.Timeline.Should().NotBeEmpty();
        result.Timeline.Should().Contain(t => t.EntityId == candidateId && t.Action == "Created");
    }

    [Fact]
    public async Task GetCandidateDetailQuery_ShouldReturnNull_WhenCandidateMissing()
    {
        var handler = new GetCandidateDetailHandler(DbContext);
        var result = await handler.Handle(new GetCandidateDetailQuery(Guid.NewGuid()), default);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetApplicationsQuery_ShouldReturnAllByDefault()
    {
        // Arrange — two candidates applied to one requisition
        var (app1, _) = await SeedAppliedCandidateAsync("Cand One", "c1@example.com");
        var (app2, _) = await SeedAppliedCandidateAsync("Cand Two", "c2@example.com");

        var handler = new GetApplicationsHandler(DbContext);

        // Act
        var result = await handler.Handle(new GetApplicationsQuery(), default);

        // Assert
        result.Should().HaveCount(2);
        var ids = result.Select(a => a.Id).ToList();
        ids.Should().Contain(new[] { app1, app2 });
        result.Select(a => a.Status).Should().AllBe("New");
    }

    [Fact]
    public async Task GetApplicationsQuery_ShouldFilterByCandidateAndStatus()
    {
        // Arrange
        var (app1, candId) = await SeedAppliedCandidateAsync("Cand One", "c1@example.com");
        await SeedAppliedCandidateAsync("Cand Two", "c2@example.com");

        var handler = new GetApplicationsHandler(DbContext);

        // Act — filter by candidate
        var byCandidate = await handler.Handle(new GetApplicationsQuery(CandidateId: candId), default);
        byCandidate.Should().HaveCount(1);
        byCandidate[0].Id.Should().Be(app1);

        // Act — filter by matching status
        var byStatus = await handler.Handle(new GetApplicationsQuery(Status: "New"), default);
        byStatus.Should().HaveCount(2);

        // Act — filter by non-matching status
        var byOtherStatus = await handler.Handle(new GetApplicationsQuery(Status: "Hired"), default);
        byOtherStatus.Should().BeEmpty();
    }

    [Fact]
    public async Task GetApplicationDetailQuery_ShouldReturnInterviewsOffersTimeline()
    {
        // Arrange — full journey
        var (_, applicationId, _, offerId) = await SeedHiredCandidateAsync();

        var handler = new GetApplicationDetailHandler(DbContext);

        // Act
        var result = await handler.Handle(new GetApplicationDetailQuery(applicationId), default);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(applicationId);
        result.Status.Should().Be("Hired");
        result.HiredBy.Should().Be("recruiter-1");
        result.Candidate.Should().NotBeNull();
        result.Candidate!.FirstName.Should().Be("Carol");
        result.Interviews.Should().HaveCount(1);
        result.Interviews[0].Feedback.Should().HaveCount(1);
        result.Offers.Should().HaveCount(1);
        result.Offers[0].Id.Should().Be(offerId);
        result.Timeline.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetOffersQuery_ShouldFilterByApplicationAndStatus()
    {
        var (_, applicationId, _, offerId) = await SeedHiredCandidateAsync();

        var handler = new GetOffersHandler(DbContext);

        var byApplication = await handler.Handle(new GetOffersQuery(ApplicationId: applicationId), default);
        byApplication.Should().HaveCount(1);
        byApplication[0].Id.Should().Be(offerId);

        var byStatus = await handler.Handle(new GetOffersQuery(Status: "Accepted"), default);
        byStatus.Should().HaveCount(1);
        byStatus[0].Status.Should().Be("Accepted");

        var byMissingStatus = await handler.Handle(new GetOffersQuery(Status: "Declined"), default);
        byMissingStatus.Should().BeEmpty();
    }

    [Fact]
    public async Task GetOfferDetailQuery_ShouldReturnOffer_WhenExists()
    {
        var (_, _, _, offerId) = await SeedHiredCandidateAsync();
        var handler = new GetOfferDetailHandler(DbContext);

        var result = await handler.Handle(new GetOfferDetailQuery(offerId), default);
        result.Should().NotBeNull();
        result!.Id.Should().Be(offerId);
        result.Status.Should().Be("Accepted");
        result.Currency.Should().Be("USD");
    }

    [Fact]
    public async Task GetPipelineQuery_ShouldGroupByLiveStages_WithCounts()
    {
        // Arrange — one New, one Screening, one Hired
        await SeedAppliedCandidateAsync("Alpha New", "alpha@example.com");
        await SeedScreeningCandidateAsync("Beta Screen", "beta@example.com");
        await SeedHiredCandidateAsync();

        var handler = new GetPipelineHandler(DbContext);
        var result = await handler.Handle(new GetPipelineQuery(), default);

        result.Stages.Should().HaveCount(5);
        result.Stages.Select(s => s.Stage).Should().BeEquivalentTo(new[] { "New", "Screening", "Interviewing", "Offered", "Hired" });
        result.StageCounts["New"].Should().Be(1);
        result.StageCounts["Screening"].Should().Be(1);
        result.StageCounts["Hired"].Should().Be(1);
        result.StageCounts["Interviewing"].Should().Be(0);
        result.StageCounts["Offered"].Should().Be(0);

        var hiredStage = result.Stages.Single(s => s.Stage == "Hired");
        hiredStage.Cards.Should().HaveCount(1);
        hiredStage.Cards[0].CandidateName.Should().Be("Carol Hired");
    }

    [Fact]
    public async Task GetCandidateTimelineQuery_ShouldAggregateCandidateAndApplicationAudit()
    {
        var (candidateId, applicationId, _, _) = await SeedHiredCandidateAsync();

        var handler = new GetCandidateTimelineHandler(DbContext);
        var result = await handler.Handle(new GetCandidateTimelineQuery(candidateId), default);

        result.Should().NotBeEmpty();
        result.Should().Contain(t => t.EntityId == candidateId && t.Action == "Created");
        result.Should().Contain(t => t.EntityId == applicationId && t.Action == "Hired");
    }

    // -----------------------------------------------------------------
    // Seeding helpers
    // -----------------------------------------------------------------

    private async Task<(Guid, RequisitionId)> NewPublishedRequisitionAsync(string title)
    {
        var reqId = await new CreateRequisitionHandler(DbContext).Handle(new CreateRequisitionCommand(title, Guid.NewGuid(), Guid.NewGuid()), default);
        await new PublishRequisitionHandler(DbContext).Handle(new PublishRequisitionCommand(reqId), default);
        return (reqId.Value, reqId);
    }

    private async Task<(Guid applicationId, Guid candidateId)> SeedAppliedCandidateAsync(string first, string email)
    {
        var (_, reqId) = await NewPublishedRequisitionAsync($"Req-{email}");
        var candidateId = await NewCandidateAsync(first, "Applied", email);
        var applicationId = await new ApplyCandidateHandler(DbContext).Handle(new ApplyCandidateCommand(candidateId, reqId), default);
        return (applicationId.Value, candidateId.Value);
    }

    private async Task<Guid> SeedScreeningCandidateAsync(string first, string email)
    {
        var (applicationIdValue, _) = await SeedAppliedCandidateAsync(first, email);
        await new AdvanceApplicationHandler(DbContext).Handle(new AdvanceApplicationCommand(new ApplicationId(applicationIdValue)), default);
        return applicationIdValue;
    }

    private async Task<(Guid candidateId, Guid applicationId, Guid interviewId, Guid offerId)> SeedHiredCandidateAsync()
    {
        var (_, reqId) = await NewPublishedRequisitionAsync("Hiring Req");
        var candidateId = await NewCandidateAsync("Carol", "Hired", "carol@example.com");
        var applicationId = await new ApplyCandidateHandler(DbContext).Handle(new ApplyCandidateCommand(candidateId, reqId), default);

        await new AdvanceApplicationHandler(DbContext).Handle(new AdvanceApplicationCommand(applicationId), default);
        var interviewerId = Guid.NewGuid();
        var interviewId = await new ScheduleInterviewHandler(DbContext).Handle(new ScheduleInterviewCommand(applicationId, DateTimeOffset.UtcNow.AddDays(1), 60, [interviewerId]), default);
        await new SubmitFeedbackHandler(DbContext).Handle(new SubmitFeedbackCommand(interviewId, interviewerId, 5, "Strong hire"), default);

        var offerId = await new CreateOfferHandler(DbContext).Handle(new CreateOfferCommand(applicationId, 150000, "USD"), default);
        await new OfferWorkflowHandler(DbContext).Handle(new ApproveOfferCommand(offerId), default);
        await new OfferWorkflowHandler(DbContext).Handle(new IssueOfferCommand(offerId, DateTimeOffset.UtcNow.AddDays(7)), default);
        await new AcceptOfferHandler(DbContext).Handle(new AcceptOfferCommand(offerId), default);
        await new HireCandidateHandler(DbContext).Handle(new HireCandidateCommand(applicationId, "recruiter-1"), default);

        return (candidateId.Value, applicationId.Value, interviewId.Value, offerId.Value);
    }

    private async Task<CandidateId> NewCandidateAsync(string first, string last, string email)
    {
        var candidateId = await new CreateCandidateHandler(DbContext).Handle(new CreateCandidateCommand(first, last, email, "555-9000"), default);
        return candidateId;
    }
}
