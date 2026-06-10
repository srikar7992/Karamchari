// -----------------------------------------------------------------------
// <copyright file="TenantCapability.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Capability.Domain.Entitlements;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum CapabilityStatus
{
    /// <inheritdoc/>
    Inactive,
    /// <inheritdoc/>
    Active,
    /// <inheritdoc/>
    Suspended,
    /// <inheritdoc/>
    Trial
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed class TenantCapability : AggregateRoot<Guid>, ITenantOwned
{
    private TenantCapability() { /* EF */ }

    private TenantCapability(Guid id, string tenantId, string capabilityId, CapabilityStatus status)
        : base(id)
    {
        TenantId = tenantId;
        CapabilityId = capabilityId;
        Status = status;
        EnabledAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string CapabilityId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public CapabilityStatus Status { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset EnabledAtUtc { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static TenantCapability Enable(string tenantId, string capabilityId, CapabilityStatus status = CapabilityStatus.Active)
    {
        return new TenantCapability(Guid.NewGuid(), tenantId, capabilityId, status);
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void UpdateStatus(CapabilityStatus newStatus)
    {
        Status = newStatus;
    }
}
