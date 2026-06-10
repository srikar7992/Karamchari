// -----------------------------------------------------------------------
// <copyright file="CriticalPosition.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Karamchari.Capability.Domain.Primitives;

namespace Karamchari.Capability.Domain.Succession;

/// <summary>
/// Designates an HR position as critical for succession planning purposes.
/// Stores the link between a position and its role skill requirement so the succession
/// projection handler can compute readiness without crossing module boundaries at runtime.
/// PK: Id (auto-generated GUID). Unique index: (TenantId, PositionId).
/// </summary>
public sealed class CriticalPosition
{
    private CriticalPosition() { }

    public Guid Id { get; private set; }
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>HR module position identifier. Not a FK — cross-module reference by Id only.</summary>
    public Guid PositionId { get; private set; }

    /// <summary>Capability module role requirement that describes the skills needed for this position.</summary>
    public Guid RoleRequirementId { get; private set; }

    public PositionCriticality Criticality { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? DeactivatedAtUtc { get; private set; }

    /// <summary>Designates a position as critical and links it to a role skill requirement.</summary>
    public static CriticalPosition Designate(
        string tenantId,
        Guid positionId,
        Guid roleRequirementId,
        PositionCriticality criticality)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        if (positionId == Guid.Empty) throw new ArgumentException("PositionId must not be empty.", nameof(positionId));
        if (roleRequirementId == Guid.Empty) throw new ArgumentException("RoleRequirementId must not be empty.", nameof(roleRequirementId));

        return new CriticalPosition
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PositionId = positionId,
            RoleRequirementId = roleRequirementId,
            Criticality = criticality,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    /// <summary>Removes this position from active succession tracking.</summary>
    public void Deactivate()
    {
        IsActive = false;
        DeactivatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>Updates the criticality level and re-activates if previously deactivated.</summary>
    public void UpdateCriticality(PositionCriticality criticality)
    {
        Criticality = criticality;
        IsActive = true;
        DeactivatedAtUtc = null;
    }
}
