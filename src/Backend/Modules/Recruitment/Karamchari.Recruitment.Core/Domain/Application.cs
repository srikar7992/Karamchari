using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Recruitment.Domain;

public sealed class Application : AggregateRoot<ApplicationId>, ITenantOwned, IAuditable
{
    private Application() : base(default) { CandidateSnapshot = null!; }

    private Application(ApplicationId id, CandidateId candidateId, RequisitionId requisitionId, CandidateSnapshot snapshot)
        : base(id)
    {
        CandidateId = candidateId;
        RequisitionId = requisitionId;
        CandidateSnapshot = snapshot;
        Status = ApplicationStatus.New;
        AppliedAt = DateTimeOffset.UtcNow;
    }

    public CandidateId CandidateId { get; private set; }
    public RequisitionId RequisitionId { get; private set; }
    public ApplicationStatus Status { get; private set; }
    public DateTimeOffset AppliedAt { get; private set; }
    public CandidateSnapshot CandidateSnapshot { get; private set; }

    public DateTimeOffset? HiredAt { get; private set; }
    public string? HiredBy { get; private set; }

    // ITenantOwned
    public string TenantId { get; private set; } = null!;

    // IAuditable
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public string CreatedBy { get; private set; } = null!;
    public DateTimeOffset? UpdatedOnUtc { get; private set; }
    public string? UpdatedBy { get; private set; }

    public static Application Create(CandidateId candidateId, RequisitionId requisitionId, CandidateSnapshot snapshot)
    {
        var application = new Application(ApplicationId.New(), candidateId, requisitionId, snapshot);
        application.RaiseDomainEvent(new CandidateAppliedDomainEvent(application.Id, candidateId, requisitionId));
        return application;
    }

    public void AdvanceToScreening()
    {
        if (Status != ApplicationStatus.New)
            throw new InvalidOperationException("Only new applications can be advanced to screening.");

        Status = ApplicationStatus.Screening;
        RaiseDomainEvent(new ApplicationAdvancedDomainEvent(Id, Status));
    }

    public void AdvanceToInterviewing()
    {
        if (Status != ApplicationStatus.Screening)
            throw new InvalidOperationException("Only screened applications can be advanced to interviewing.");

        Status = ApplicationStatus.Interviewing;
        RaiseDomainEvent(new ApplicationAdvancedDomainEvent(Id, Status));
    }

    public void MarkAsOffered()
    {
        if (Status != ApplicationStatus.Interviewing)
            throw new InvalidOperationException("Only applications in interviewing stage can be marked as offered.");

        Status = ApplicationStatus.Offered;
        RaiseDomainEvent(new ApplicationAdvancedDomainEvent(Id, Status));
    }

    public void Hire(string hiredBy)
    {
        if (Status != ApplicationStatus.Offered)
            throw new InvalidOperationException("Only applications with an accepted offer can be hired.");

        Status = ApplicationStatus.Hired;
        HiredAt = DateTimeOffset.UtcNow;
        HiredBy = hiredBy;
        RaiseDomainEvent(new CandidateHiredDomainEvent(Id, CandidateId));
    }

    public void Reject()
    {
        if (Status is ApplicationStatus.Hired or ApplicationStatus.Rejected)
            throw new InvalidOperationException(
                $"Cannot reject an application that is already in terminal state '{Status}'.");

        Status = ApplicationStatus.Rejected;
    }
}
