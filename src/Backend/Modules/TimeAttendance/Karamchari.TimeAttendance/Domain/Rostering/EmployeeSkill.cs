// -----------------------------------------------------------------------
// <copyright file="EmployeeSkill.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.TimeAttendance.Domain.Rostering;

public sealed class EmployeeSkill : Entity<Guid>, ITenantOwned
{
    private EmployeeSkill() { TenantId = string.Empty; }

    public string TenantId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public Guid SkillId { get; private set; }
    public string SkillName { get; private set; } = string.Empty;
    public int Level { get; private set; }
    public DateOnly? ExpiryDate { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public bool IsValid(DateOnly asOf) =>
        IsActive && (ExpiryDate == null || ExpiryDate >= asOf);

    public static EmployeeSkill Grant(
        string tenantId,
        Guid employeeId,
        Guid skillId,
        string skillName,
        int level,
        DateOnly? expiryDate = null)
    {
        if (level < 1 || level > 5)
            throw new ArgumentOutOfRangeException(nameof(level), "Skill level must be 1–5.");

        return new EmployeeSkill
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            SkillId = skillId,
            SkillName = skillName.Trim(),
            Level = level,
            ExpiryDate = expiryDate,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    public void Revoke() => IsActive = false;

    public void UpdateExpiry(DateOnly expiryDate) => ExpiryDate = expiryDate;
}
