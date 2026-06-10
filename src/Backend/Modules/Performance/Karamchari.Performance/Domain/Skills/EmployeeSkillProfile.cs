// -----------------------------------------------------------------------
// <copyright file="EmployeeSkillProfile.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Performance.Domain.Skills;

/// <summary>
/// All skill assessments for one employee across all skills.
/// Seeded by EmployeeOnboardedPerformanceConsumer. One profile per employee per tenant.
/// </summary>
public sealed class EmployeeSkillProfile : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<EmployeeSkillAssessment> _assessments = [];

    private EmployeeSkillProfile() { /* EF materialization */ }

    private EmployeeSkillProfile(Guid id, string tenantId, Guid employeeId) : base(id)
    {
        TenantId = tenantId;
        EmployeeId = employeeId;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid EmployeeId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public IReadOnlyList<EmployeeSkillAssessment> Assessments => _assessments.AsReadOnly();

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static EmployeeSkillProfile Seed(string tenantId, Guid employeeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        return new EmployeeSkillProfile(Guid.NewGuid(), tenantId, employeeId);
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void RecordAssessment(
        Guid skillId,
        ProficiencyLevel level,
        AssessmentSource source,
        Guid? assessedByEmployeeId = null,
        string? evidence = null)
    {
        _assessments.Add(new EmployeeSkillAssessment(
            Guid.NewGuid(), Id, skillId, level, source, assessedByEmployeeId, evidence?.Trim()));
    }

    /// <summary>Returns the most recent assessment for a skill, or null if never assessed.</summary>
    public EmployeeSkillAssessment? CurrentAssessment(Guid skillId)
        => _assessments
            .Where(a => a.SkillId == skillId)
            .MaxBy(a => a.AssessedOnUtc);
}
