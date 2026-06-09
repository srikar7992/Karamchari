namespace Karamchari.HR.Domain.Organization;

using System;
using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

/// <summary>
/// Stores point-in-time frozen snapshots of the organizational structure.
/// <para>WARNING: The HierarchyDataJson property is intended for read-only archiving or external reporting export, not active querying.</para>
/// </summary>
public sealed class OrganizationSnapshot : AggregateRoot<Guid>, ITenantOwned
{
    private OrganizationSnapshot() { TenantId = string.Empty; Name = string.Empty; HierarchyDataJson = string.Empty; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrganizationSnapshot"/> class.
    /// </summary>
    /// <param name="id">The unique identifier of the snapshot.</param>
    /// <param name="tenantId">The unique identifier of the tenant context.</param>
    /// <param name="snapshotDate">The date and time when the snapshot was captured.</param>
    /// <param name="name">The name/label of the snapshot.</param>
    /// <param name="hierarchyDataJson">The JSON serialized organizational hierarchy structure representation.</param>
    public OrganizationSnapshot(
        Guid id,
        string tenantId,
        DateTimeOffset snapshotDate,
        string name,
        string hierarchyDataJson) : base(id)
    {
        TenantId = tenantId;
        SnapshotDate = snapshotDate;
        Name = name;
        HierarchyDataJson = hierarchyDataJson;
    }

    /// <summary>Gets the tenant ID for multi-tenant isolation.</summary>
    public string TenantId { get; private set; }
    /// <summary>Gets the date and time when the snapshot was captured.</summary>
    public DateTimeOffset SnapshotDate { get; private set; }
    /// <summary>Gets the name/label of the snapshot.</summary>
    public string Name { get; private set; }
    /// <summary>Gets the JSON serialized organizational hierarchy structure representation.</summary>
    public string HierarchyDataJson { get; private set; }
}
