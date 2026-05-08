using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.Performance.Domain.Skills;

namespace Karamchari.Performance.Domain.ReadModels;

/// <summary>
/// Denormalized skill snapshot per employee. One row per (employee, skill).
/// Used for talent discovery queries (ADR-0010) — find employees with target skills.
/// Full-text search indexed on SkillName and SkillCategoryName.
/// </summary>
public sealed class EmployeeSkillInventoryItem : Entity<Guid>, ITenantOwned
{
    private EmployeeSkillInventoryItem() { /* EF materialization */ }

    public string TenantId { get; private set; } = string.Empty;
    public Guid EmployeeId { get; private set; }
    public string EmployeeDisplayName { get; private set; } = string.Empty;
    public string Department { get; private set; } = string.Empty;
    public string CareerLevel { get; private set; } = string.Empty;
    public Guid SkillId { get; private set; }
    public string SkillName { get; private set; } = string.Empty;
    public string SkillCategoryName { get; private set; } = string.Empty;
    public SkillDomain Domain { get; private set; }
    public ProficiencyLevel CurrentProficiency { get; private set; }
    public ProficiencyLevel? TargetProficiency { get; private set; }

    /// <summary>Gap between target and current. Null if no target set or already met.</summary>
    public int? ProficiencyGap { get; private set; }

    public AssessmentSource LastAssessmentSource { get; private set; }
    public DateTimeOffset LastAssessedOnUtc { get; private set; }
    public DateTimeOffset LastRefreshedUtc { get; private set; }

    public static EmployeeSkillInventoryItem Create(
        string tenantId,
        Guid employeeId,
        string employeeDisplayName,
        string department,
        string careerLevel,
        Guid skillId,
        string skillName,
        string skillCategoryName,
        SkillDomain domain,
        ProficiencyLevel currentProficiency,
        ProficiencyLevel? targetProficiency,
        AssessmentSource lastAssessmentSource,
        DateTimeOffset lastAssessedOnUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(employeeDisplayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(skillName);
        ArgumentException.ThrowIfNullOrWhiteSpace(skillCategoryName);

        int? gap = null;
        if (targetProficiency.HasValue && (int)targetProficiency.Value > (int)currentProficiency)
            gap = (int)targetProficiency.Value - (int)currentProficiency;

        return new EmployeeSkillInventoryItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            EmployeeDisplayName = employeeDisplayName,
            Department = department,
            CareerLevel = careerLevel,
            SkillId = skillId,
            SkillName = skillName,
            SkillCategoryName = skillCategoryName,
            Domain = domain,
            CurrentProficiency = currentProficiency,
            TargetProficiency = targetProficiency,
            ProficiencyGap = gap,
            LastAssessmentSource = lastAssessmentSource,
            LastAssessedOnUtc = lastAssessedOnUtc,
            LastRefreshedUtc = DateTimeOffset.UtcNow,
        };
    }

    public void UpdateProficiency(
        ProficiencyLevel current,
        ProficiencyLevel? target,
        AssessmentSource source,
        DateTimeOffset assessedOn)
    {
        CurrentProficiency = current;
        TargetProficiency = target;
        ProficiencyGap = target.HasValue && (int)target.Value > (int)current
            ? (int)target.Value - (int)current
            : null;
        LastAssessmentSource = source;
        LastAssessedOnUtc = assessedOn;
        LastRefreshedUtc = DateTimeOffset.UtcNow;
    }
}
