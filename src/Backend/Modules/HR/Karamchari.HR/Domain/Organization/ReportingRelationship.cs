namespace Karamchari.HR.Domain.Organization;

using System;
using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

/// <summary>
/// Represents a hierarchical reporting relationship between two positions in the organization.
/// </summary>
public sealed class ReportingRelationship : AggregateRoot<Guid>, ITenantOwned
{
    private ReportingRelationship() { TenantId = string.Empty; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ReportingRelationship"/> class.
    /// </summary>
    /// <param name="id">The unique identifier of the reporting relationship.</param>
    /// <param name="tenantId">The unique identifier of the tenant context.</param>
    /// <param name="managerPositionId">The position ID of the reporting manager.</param>
    /// <param name="directReportPositionId">The position ID of the direct report.</param>
    /// <param name="effectiveFrom">The date and time from which the reporting relationship is effective.</param>
    /// <param name="effectiveTo">The date and time until which the reporting relationship is effective, if defined.</param>
    public ReportingRelationship(
        Guid id,
        string tenantId,
        Guid managerPositionId,
        Guid directReportPositionId,
        DateTimeOffset effectiveFrom,
        DateTimeOffset? effectiveTo) : base(id)
    {
        TenantId = tenantId;
        ManagerPositionId = managerPositionId;
        DirectReportPositionId = directReportPositionId;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
    }

    /// <summary>
    /// Gets the tenant ID for multi-tenant isolation.
    /// </summary>
    public string TenantId { get; private set; }

    /// <summary>
    /// Gets the position ID of the reporting manager.
    /// </summary>
    public Guid ManagerPositionId { get; private set; }

    /// <summary>
    /// Gets the position ID of the direct report.
    /// </summary>
    public Guid DirectReportPositionId { get; private set; }

    /// <summary>
    /// Gets the date and time from which the reporting relationship is effective.
    /// </summary>
    public DateTimeOffset EffectiveFrom { get; private set; }

    /// <summary>
    /// Gets the date and time until which the reporting relationship is effective, if defined.
    /// </summary>
    public DateTimeOffset? EffectiveTo { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the reporting relationship is currently active.
    /// </summary>
    public bool IsActive => EffectiveTo == null || EffectiveTo > DateTimeOffset.UtcNow;
}
