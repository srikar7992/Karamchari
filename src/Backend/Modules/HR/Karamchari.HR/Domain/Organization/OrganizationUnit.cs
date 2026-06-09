namespace Karamchari.HR.Domain.Organization;

using System;
using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

/// <summary>
/// Represents an organizational unit (e.g. department, division, team) in the company structure.
/// </summary>
public sealed class OrganizationUnit : AggregateRoot<Guid>, ITenantOwned
{
    private OrganizationUnit() { TenantId = string.Empty; Name = string.Empty; Code = string.Empty; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrganizationUnit"/> class.
    /// </summary>
    /// <param name="id">The unique identifier of the organization unit.</param>
    /// <param name="tenantId">The unique identifier of the tenant context.</param>
    /// <param name="name">The name of the organization unit.</param>
    /// <param name="code">The unique code identifier of the organization unit.</param>
    /// <param name="type">The category type of the organization unit.</param>
    /// <param name="parentUnitId">The parent unit ID, if defined.</param>
    /// <param name="leadEmployeeId">The ID of the employee who leads this unit, if defined.</param>
    /// <param name="costCenterId">The associated cost center ID, if defined.</param>
    public OrganizationUnit(
        Guid id,
        string tenantId,
        string name,
        string code,
        OrganizationUnitType type,
        Guid? parentUnitId,
        Guid? leadEmployeeId,
        Guid? costCenterId) : base(id)
    {
        TenantId = tenantId;
        Name = name;
        Code = code;
        Type = type;
        ParentUnitId = parentUnitId;
        LeadEmployeeId = leadEmployeeId;
        CostCenterId = costCenterId;
        IsActive = true;
    }

    /// <summary>Gets the tenant ID for multi-tenant isolation.</summary>
    public string TenantId { get; private set; }
    /// <summary>Gets the name of the organization unit.</summary>
    public string Name { get; private set; }
    /// <summary>Gets the unique code of the organization unit.</summary>
    public string Code { get; private set; }
    /// <summary>Gets the category type of the organization unit.</summary>
    public OrganizationUnitType Type { get; private set; }
    /// <summary>Gets the parent unit ID, if defined.</summary>
    public Guid? ParentUnitId { get; private set; }
    /// <summary>Gets the ID of the employee who leads this unit, if defined.</summary>
    public Guid? LeadEmployeeId { get; private set; }
    /// <summary>Gets the associated cost center ID, if defined.</summary>
    public Guid? CostCenterId { get; private set; }
    /// <summary>Gets a value indicating whether the organization unit is active.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Deactivates this organization unit.</summary>
    public void Deactivate() => IsActive = false;
}
