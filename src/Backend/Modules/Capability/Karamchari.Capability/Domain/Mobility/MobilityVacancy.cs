// -----------------------------------------------------------------------
// <copyright file="MobilityVacancy.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Capability.Domain.Mobility;

/// <summary>
/// Lightweight read model tracking open vacancies that have a linked role skill requirement.
/// Used by the Capability module to find open vacancies without cross-context DB reads.
/// Row exists while vacancy is open; deleted when vacancy closes.
/// </summary>
public sealed class MobilityVacancy
{
    private MobilityVacancy() { }

    public string TenantId { get; private set; } = string.Empty;
    public Guid VacancyId { get; private set; }
    public Guid PositionId { get; private set; }
    public Guid RoleRequirementId { get; private set; }
    public DateTimeOffset OpenedUtc { get; private set; }

    public static MobilityVacancy Open(
        string tenantId,
        Guid vacancyId,
        Guid positionId,
        Guid roleRequirementId,
        DateTimeOffset openedUtc)
    {
        return new MobilityVacancy
        {
            TenantId = tenantId,
            VacancyId = vacancyId,
            PositionId = positionId,
            RoleRequirementId = roleRequirementId,
            OpenedUtc = openedUtc,
        };
    }
}
