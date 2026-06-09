using System;
using Karamchari.Core.Domain.Primitives;

namespace Karamchari.HR.Domain.Organization.Events;

public sealed record PositionVacancyOpened(
    Guid VacancyId,
    string TenantId,
    Guid PositionId,
    DateTimeOffset OpenedUtc,
    VacancyStatus Status,
    string Reason,
    Guid? RecruitmentRequisitionId,
    string? ExternalReferenceId,
    Guid? RoleSkillRequirementId = null) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
