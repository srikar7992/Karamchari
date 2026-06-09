using System;

namespace Karamchari.HR.Domain.Organization.Projections;

public sealed class VacancyPipelineProjection
{
    public string TenantId { get; set; } = string.Empty;
    public Guid VacancyId { get; set; }
    public Guid PositionId { get; set; }
    public string PositionCode { get; set; } = string.Empty;
    public DateTimeOffset OpenedUtc { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public Guid? RecruitmentRequisitionId { get; set; }
    public string? ExternalReferenceId { get; set; }
    public DateTimeOffset? ClosedUtc { get; set; }
    public string? ClosedReason { get; set; }
    public Guid? FilledByEmployeeId { get; set; }
    public Guid? FilledPositionAssignmentId { get; set; }
    public DateTimeOffset LastUpdatedAtUtc { get; set; }
}
