namespace Karamchari.HR.Domain.Organization;

using System;
using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

/// <summary>
/// Represents an individual change/action applied in a future organization scenario.
/// </summary>
public sealed class ScenarioChange : Entity<Guid>, ITenantOwned
{
    private ScenarioChange() { TenantId = string.Empty; TargetId = string.Empty; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScenarioChange"/> class.
    /// </summary>
    /// <param name="id">The unique identifier of the scenario change.</param>
    /// <param name="tenantId">The unique identifier of the tenant context.</param>
    /// <param name="scenarioId">The associated future org scenario ID.</param>
    /// <param name="action">The type of action to perform.</param>
    /// <param name="targetId">The string representation of the target entity ID.</param>
    /// <param name="name">An optional name parameter.</param>
    /// <param name="code">An optional code parameter.</param>
    /// <param name="unitType">An optional organization unit type parameter.</param>
    /// <param name="parentUnitId">An optional parent unit ID parameter.</param>
    /// <param name="positionId">An optional position ID parameter.</param>
    /// <param name="sourceUnitId">An optional source unit ID parameter.</param>
    /// <param name="destinationUnitId">An optional destination unit ID parameter.</param>
    public ScenarioChange(
        Guid id,
        string tenantId,
        Guid scenarioId,
        ScenarioChangeType action,
        string targetId,
        string? name = null,
        string? code = null,
        OrganizationUnitType? unitType = null,
        Guid? parentUnitId = null,
        Guid? positionId = null,
        Guid? sourceUnitId = null,
        Guid? destinationUnitId = null) : base(id)
    {
        TenantId = tenantId;
        ScenarioId = scenarioId;
        Action = action;
        TargetId = targetId;
        Name = name;
        Code = code;
        UnitType = unitType;
        ParentUnitId = parentUnitId;
        PositionId = positionId;
        SourceUnitId = sourceUnitId;
        DestinationUnitId = destinationUnitId;
    }

    /// <summary>Gets the tenant ID for multi-tenant isolation.</summary>
    public string TenantId { get; private set; }
    /// <summary>Gets the ID of the future organization scenario.</summary>
    public Guid ScenarioId { get; private set; }
    /// <summary>Gets the type of action applied in the scenario.</summary>
    public ScenarioChangeType Action { get; private set; }
    /// <summary>Gets the target entity identifier.</summary>
    public string TargetId { get; private set; }

    /// <summary>Gets the name parameter if applicable.</summary>
    public string? Name { get; private set; }
    /// <summary>Gets the code parameter if applicable.</summary>
    public string? Code { get; private set; }
    /// <summary>Gets the organization unit type parameter if applicable.</summary>
    public OrganizationUnitType? UnitType { get; private set; }
    /// <summary>Gets the parent unit ID parameter if applicable.</summary>
    public Guid? ParentUnitId { get; private set; }
    /// <summary>Gets the position ID parameter if applicable.</summary>
    public Guid? PositionId { get; private set; }
    /// <summary>Gets the source unit ID parameter if applicable.</summary>
    public Guid? SourceUnitId { get; private set; }
    /// <summary>Gets the destination unit ID parameter if applicable.</summary>
    public Guid? DestinationUnitId { get; private set; }
}
