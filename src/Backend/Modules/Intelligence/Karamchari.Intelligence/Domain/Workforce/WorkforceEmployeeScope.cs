// -----------------------------------------------------------------------
// <copyright file="WorkforceEmployeeScope.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Multitenancy;

namespace Karamchari.Intelligence.Domain.Workforce;

/// <summary>
/// Stores the site, department, and manager assignment for an employee.
/// Used to compute scoped hotspots at site / department / manager granularity.
/// Populated by integration events from HR/WFP; updated on organisational changes.
/// Maps to Intel_EmployeeScopes.
/// </summary>
public sealed class WorkforceEmployeeScope : ITenantOwned
{
    /// <inheritdoc/>
    public Guid Id { get; private set; }

    /// <inheritdoc/>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Employee this scope record belongs to.</summary>
    public Guid EmployeeId { get; private set; }

    /// <summary>Site or location code (max 50 chars). Null if not assigned.</summary>
    public string? SiteCode { get; private set; }

    /// <summary>Department or cost-centre code (max 50 chars). Null if not assigned.</summary>
    public string? DepartmentCode { get; private set; }

    /// <summary>Manager's employee identifier (max 100 chars). Null if no direct manager.</summary>
    public string? ManagerId { get; private set; }

    /// <summary>UTC timestamp of last update.</summary>
    public DateTime UpdatedAt { get; private set; }

    private WorkforceEmployeeScope() { }

    /// <summary>Creates a new scope assignment for an employee.</summary>
    public static WorkforceEmployeeScope Assign(
        string tenantId,
        Guid employeeId,
        string? siteCode,
        string? departmentCode,
        string? managerId)
    {
        ArgumentNullException.ThrowIfNull(tenantId);

        return new WorkforceEmployeeScope
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            SiteCode = siteCode,
            DepartmentCode = departmentCode,
            ManagerId = managerId,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Updates site, department, and manager for an existing scope record.</summary>
    public void Update(string? siteCode, string? departmentCode, string? managerId)
    {
        SiteCode = siteCode;
        DepartmentCode = departmentCode;
        ManagerId = managerId;
        UpdatedAt = DateTime.UtcNow;
    }
}
