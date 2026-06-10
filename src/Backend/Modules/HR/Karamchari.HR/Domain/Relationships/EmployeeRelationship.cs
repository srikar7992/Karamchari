// -----------------------------------------------------------------------
// <copyright file="EmployeeRelationship.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.HR.Domain.Relationships;

/// <summary>
/// Represents a named relationship between two employees within a tenant.
/// Models matrix organizations: direct managers, dotted-line managers, project leads,
/// mentors, acting managers, and calibration committee memberships.
///
/// Design rule: relationships are advisory â€” they inform visibility and workflow routing
/// but do NOT enforce access control by themselves. RBAC + RLS remain the authority.
/// </summary>
public sealed class EmployeeRelationship : Entity<Guid>, ITenantOwned
{
    private EmployeeRelationship() { /* EF materialization */ }

    private EmployeeRelationship(
        Guid id,
        string tenantId,
        Guid fromEmployeeId,
        Guid toEmployeeId,
        RelationshipType relationshipType,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo,
        string? context) : base(id)
    {
        TenantId = tenantId;
        FromEmployeeId = fromEmployeeId;
        ToEmployeeId = toEmployeeId;
        RelationshipType = relationshipType;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        Context = context;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>The employee receiving the role (e.g., the manager).</summary>
    public Guid FromEmployeeId { get; private set; }

    /// <summary>The employee being managed/mentored/etc.</summary>
    public Guid ToEmployeeId { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public RelationshipType RelationshipType { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateOnly EffectiveFrom { get; private set; }

    /// <summary>Null means the relationship is still active.</summary>
    public DateOnly? EffectiveTo { get; private set; }

    /// <summary>Optional free-text describing the relationship context (project name, etc.).</summary>
    public string? Context { get; private set; }

    /// <summary>True if EffectiveTo is null or in the future relative to today.</summary>
    public bool IsActive => EffectiveTo is null || EffectiveTo.Value >= DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static EmployeeRelationship Create(
        string tenantId,
        Guid fromEmployeeId,
        Guid toEmployeeId,
        RelationshipType relationshipType,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo = null,
        string? context = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        if (fromEmployeeId == toEmployeeId)
            throw new ArgumentException("An employee cannot have a relationship with themselves.");

        if (effectiveTo.HasValue && effectiveTo.Value < effectiveFrom)
            throw new ArgumentException("EffectiveTo cannot be before EffectiveFrom.");

        return new EmployeeRelationship(
            Guid.NewGuid(), tenantId, fromEmployeeId, toEmployeeId,
            relationshipType, effectiveFrom, effectiveTo, context?.Trim());
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Terminate(DateOnly terminatedOn)
    {
        if (!IsActive)
            throw new InvalidOperationException("Relationship is already terminated.");

        if (terminatedOn < EffectiveFrom)
            throw new ArgumentException("Termination date cannot be before effective-from date.");

        EffectiveTo = terminatedOn;
    }
}
