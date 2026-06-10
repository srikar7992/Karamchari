// -----------------------------------------------------------------------
// <copyright file="CareerProgressionEdge.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace Karamchari.Capability.Domain.Pathing;

/// <summary>
/// A directed career progression edge between two role skill requirements.
/// Models "employees in role A commonly progress to role B."
/// Used by <see cref="Karamchari.Capability.Services.CareerPathService"/> for graph traversal.
/// PK: (TenantId, FromRoleRequirementId, ToRoleRequirementId).
/// </summary>
public sealed class CareerProgressionEdge
{
    private CareerProgressionEdge() { }

    public string TenantId { get; private set; } = string.Empty;
    public Guid FromRoleRequirementId { get; private set; }
    public Guid ToRoleRequirementId { get; private set; }

    /// <summary>
    /// Edge weight for Dijkstra-based shortest-path queries.
    /// Lower weight = more direct/common progression. Default 1.0.
    /// </summary>
    public decimal Weight { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static CareerProgressionEdge Add(
        string tenantId,
        Guid fromRoleRequirementId,
        Guid toRoleRequirementId,
        decimal weight = 1.0m)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        if (fromRoleRequirementId == toRoleRequirementId)
            throw new ArgumentException("Edge cannot be self-referential.");
        if (weight <= 0m)
            throw new ArgumentOutOfRangeException(nameof(weight), "Weight must be positive.");

        return new CareerProgressionEdge
        {
            TenantId = tenantId,
            FromRoleRequirementId = fromRoleRequirementId,
            ToRoleRequirementId = toRoleRequirementId,
            Weight = weight,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
    }
}
