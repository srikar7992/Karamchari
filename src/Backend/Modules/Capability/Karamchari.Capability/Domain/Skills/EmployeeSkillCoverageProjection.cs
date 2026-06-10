// -----------------------------------------------------------------------
// <copyright file="EmployeeSkillCoverageProjection.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace Karamchari.Capability.Domain.Skills;

/// <summary>
/// Pre-computed skill coverage score for one employee against one role skill requirement.
/// Supports skill gap analysis, career path readiness, promotion readiness,
/// internal mobility ranking, and succession readiness without runtime graph traversal.
/// PK: (TenantId, EmployeeId, RoleRequirementId).
/// </summary>
public sealed class EmployeeSkillCoverageProjection
{
    private EmployeeSkillCoverageProjection() { }

    /// <summary>Gets the tenant identifier.</summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Gets the employee identifier.</summary>
    public Guid EmployeeId { get; private set; }

    /// <summary>
    /// Gets the role skill requirement identifier this coverage was computed against.
    /// Maps to <c>RoleSkillRequirement.Id</c>.
    /// </summary>
    public Guid RoleRequirementId { get; private set; }

    /// <summary>
    /// Gets the role title from the requirement at the time this projection was computed.
    /// Denormalized for query convenience.
    /// </summary>
    public string RoleTitle { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the percentage of mandatory required skills met or exceeded by the employee.
    /// Range: 0.0000 – 100.0000.
    /// </summary>
    public decimal CoveragePercent { get; private set; }

    /// <summary>Gets the count of mandatory skills where the employee meets or exceeds the required level.</summary>
    public int MatchedSkillCount { get; private set; }

    /// <summary>Gets the count of mandatory skills where the employee is below the required level or missing.</summary>
    public int MissingSkillCount { get; private set; }

    /// <summary>Gets the total number of mandatory required skills in the role requirement.</summary>
    public int TotalRequiredSkillCount { get; private set; }

    /// <summary>Gets the UTC timestamp when this projection was last recomputed.</summary>
    public DateTimeOffset LastUpdatedUtc { get; private set; }

    /// <summary>Creates a new projection row from computed coverage values.</summary>
    public static EmployeeSkillCoverageProjection Compute(
        string tenantId,
        Guid employeeId,
        Guid roleRequirementId,
        string roleTitle,
        int matchedSkillCount,
        int missingSkillCount,
        int totalRequiredSkillCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        var coveragePercent = totalRequiredSkillCount > 0
            ? Math.Round((decimal)matchedSkillCount / totalRequiredSkillCount * 100m, 4)
            : 100m;

        return new EmployeeSkillCoverageProjection
        {
            TenantId = tenantId,
            EmployeeId = employeeId,
            RoleRequirementId = roleRequirementId,
            RoleTitle = roleTitle,
            CoveragePercent = coveragePercent,
            MatchedSkillCount = matchedSkillCount,
            MissingSkillCount = missingSkillCount,
            TotalRequiredSkillCount = totalRequiredSkillCount,
            LastUpdatedUtc = DateTimeOffset.UtcNow,
        };
    }

    /// <summary>Updates this projection in-place with recomputed values.</summary>
    public void Update(int matchedSkillCount, int missingSkillCount, int totalRequiredSkillCount, string roleTitle)
    {
        RoleTitle = roleTitle;
        MatchedSkillCount = matchedSkillCount;
        MissingSkillCount = missingSkillCount;
        TotalRequiredSkillCount = totalRequiredSkillCount;
        CoveragePercent = totalRequiredSkillCount > 0
            ? Math.Round((decimal)matchedSkillCount / totalRequiredSkillCount * 100m, 4)
            : 100m;
        LastUpdatedUtc = DateTimeOffset.UtcNow;
    }
}
