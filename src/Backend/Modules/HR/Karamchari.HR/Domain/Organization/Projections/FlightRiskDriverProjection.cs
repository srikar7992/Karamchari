using System;

namespace Karamchari.HR.Domain.Organization.Projections;

/// <summary>
/// Read model representing a specific risk factor/driver contributing to an employee's flight risk.
/// </summary>
public sealed class FlightRiskDriverProjection
{
    /// <summary>Gets or sets the tenant identifier associated with the employee.</summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>Gets or sets the unique identifier of the employee.</summary>
    public Guid EmployeeId { get; set; }

    /// <summary>Gets or sets the type of risk driver factor.</summary>
    public FlightRiskDriverType DriverType { get; set; }

    /// <summary>Gets or sets the weight/score allocated to this driver.</summary>
    public decimal FactorScore { get; set; }

    /// <summary>Gets or sets the descriptive explanation explaining why this factor contributed to the score.</summary>
    public string Explanation { get; set; } = string.Empty;
}

