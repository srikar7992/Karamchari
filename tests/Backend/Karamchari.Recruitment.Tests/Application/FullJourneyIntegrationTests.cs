using FluentAssertions;
using Karamchari.Recruitment.Application.Commands;
using Karamchari.Recruitment.Domain;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace Karamchari.Recruitment.Tests.Application;

#pragma warning disable CA1707 // Underscores in test names

public sealed class FullJourneyIntegrationTests : RecruitmentIntegrationTestBase
{
    [Fact]
    public async Task VerticalSliceRequisitionToHireShouldSucceed()
    {
        // 1. Create & Publish Requisition
        var createReqHandler = new CreateRequisitionHandler(DbContext);
        var reqId = await createReqHandler.Handle(new CreateRequisitionCommand("Software Engineer", Guid.NewGuid(), Guid.NewGuid()), default);

        var publishHandler = new PublishRequisitionHandler(DbContext);
        await publishHandler.Handle(new PublishRequisitionCommand(reqId), default);

        var req = await DbContext.Requisitions.FindAsync(reqId);
        req!.Status.Should().Be(RequisitionStatus.Published);

        // 2. Create Candidate & Apply
        var createCandidateHandler = new CreateCandidateHandler(DbContext);
        var candidateId = await createCandidateHandler.Handle(new CreateCandidateCommand("Jane", "Doe", "jane.doe@example.com", "555-1234"), default);

        var applyHandler = new ApplyCandidateHandler(DbContext);
        var applicationId = await applyHandler.Handle(new ApplyCandidateCommand(candidateId, reqId), default);

        var app = await DbContext.Applications.FirstOrDefaultAsync(a => a.Id == applicationId);
        app!.Status.Should().Be(ApplicationStatus.New);
        app.CandidateSnapshot.FirstName.Should().Be("Jane");

        // 2.5 Advance to Screening
        app.AdvanceToScreening();
        await DbContext.SaveChangesAsync();

        // 3. Schedule Interview
        var scheduleHandler = new ScheduleInterviewHandler(DbContext);
        var interviewId = await scheduleHandler.Handle(new ScheduleInterviewCommand(applicationId, DateTimeOffset.UtcNow.AddDays(1), 60, [Guid.NewGuid()]), default);

        var interview = await DbContext.Interviews.FindAsync(interviewId);
        interview!.Status.Should().Be(InterviewStatus.Scheduled);
        
        var appAfterInterview = await DbContext.Applications.FindAsync(applicationId);
        appAfterInterview!.Status.Should().Be(ApplicationStatus.Interviewing);

        // 4. Submit Feedback & Complete Interview
        var feedbackHandler = new SubmitFeedbackHandler(DbContext);
        await feedbackHandler.Handle(new SubmitFeedbackCommand(interviewId, interview.InterviewerIds[0], 5, "Excellent candidate"), default);

        var interviewAfterFeedback = await DbContext.Interviews.FindAsync(interviewId);
        interviewAfterFeedback!.Status.Should().Be(InterviewStatus.Completed);

        // 5. Draft, Approve, Issue Offer
        var createOfferHandler = new CreateOfferHandler(DbContext);
        var offerId = await createOfferHandler.Handle(new CreateOfferCommand(applicationId, 120000, "USD"), default);

        var workflowHandler = new OfferWorkflowHandler(DbContext);
        await workflowHandler.Handle(new ApproveOfferCommand(offerId), default);
        await workflowHandler.Handle(new IssueOfferCommand(offerId, DateTimeOffset.UtcNow.AddDays(7)), default);

        var offer = await DbContext.Offers.FindAsync(offerId);
        offer!.Status.Should().Be(OfferStatus.Issued);
        
        var appAfterOffer = await DbContext.Applications.FindAsync(applicationId);
        appAfterOffer!.Status.Should().Be(ApplicationStatus.Offered);

        // 6. Accept Offer
        var acceptHandler = new AcceptOfferHandler(DbContext);
        await acceptHandler.Handle(new AcceptOfferCommand(offerId), default);

        var offerAfterAccept = await DbContext.Offers.FindAsync(offerId);
        offerAfterAccept!.Status.Should().Be(OfferStatus.Accepted);

        // 7. Hire Candidate
        var hireHandler = new HireCandidateHandler(DbContext);
        await hireHandler.Handle(new HireCandidateCommand(applicationId, "recruiter-1"), default);

        var finalApp = await DbContext.Applications.FindAsync(applicationId);
        finalApp!.Status.Should().Be(ApplicationStatus.Hired);
        finalApp.HiredBy.Should().Be("recruiter-1");

        // 8. AUDIT STREAM CERTIFICATION: Verify entries created in audit log
        var auditEntries = await DbContext.AuditStream.ToListAsync();
        auditEntries.Should().HaveCountGreaterOrEqualTo(8, "Every major workflow step should generate an audit entry.");
        auditEntries.Should().Contain(a => a.EntityType == "Application" && a.Action == "Hired");
    }

    [Fact]
    public async Task MultiTenantCollisionShouldBeIsolated()
    {
        // 1. Arrange: Tenant A creates a requisition with same Title as Tenant B
        var reqIdA = await new CreateRequisitionHandler(DbContext).Handle(new CreateRequisitionCommand("Engineer", Guid.NewGuid(), Guid.NewGuid()), default);
        
        TenantProvider.GetCurrentTenantId().Returns("tenant-b");
        var reqIdB = await new CreateRequisitionHandler(DbContext).Handle(new CreateRequisitionCommand("Engineer", Guid.NewGuid(), Guid.NewGuid()), default);

        // 2. Act & Assert: Tenant B should not see Tenant A's requisition
        var reqsB = await DbContext.Requisitions.ToListAsync();
        reqsB.Should().HaveCount(1);
        reqsB[0].Id.Should().Be(reqIdB);
        reqsB[0].Id.Should().NotBe(reqIdA);
    }

    [Fact]
    public async Task CandidateApplyShouldBeIdempotent()
    {
        // Arrange
        var reqHandler = new CreateRequisitionHandler(DbContext);
        var reqId = await reqHandler.Handle(new CreateRequisitionCommand("Dev", Guid.NewGuid(), Guid.NewGuid()), default);
        await new PublishRequisitionHandler(DbContext).Handle(new PublishRequisitionCommand(reqId), default);

        var candHandler = new CreateCandidateHandler(DbContext);
        var candId = await candHandler.Handle(new CreateCandidateCommand("J", "D", "j@d.com", null), default);

        var applyHandler = new ApplyCandidateHandler(DbContext);
        await applyHandler.Handle(new ApplyCandidateCommand(candId, reqId), default);

        // Act
        var act = () => applyHandler.Handle(new ApplyCandidateCommand(candId, reqId), default);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Candidate has already applied for this requisition.");
    }
}
