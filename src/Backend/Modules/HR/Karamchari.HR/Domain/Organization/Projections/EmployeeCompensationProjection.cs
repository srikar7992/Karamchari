using System;

namespace Karamchari.HR.Domain.Organization.Projections;

/// <summary>
/// Read model representing a cache of employee compensation metrics populated by Compensation integration events.
/// Used directly as input into the retention risk calculator.
/// </summary>
public sealed class EmployeeCompensationProjection
{
    /// <summary>Gets or sets the tenant identifier associated with the employee.</summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>Gets or sets the unique identifier of the employee.</summary>
    public Guid EmployeeId { get; set; }

    /// <summary>Gets or sets the current annual CTC (Cost to Company) of the employee.</summary>
    public decimal AnnualCTC { get; set; }

    /// <summary>Gets or sets the previous annual CTC before the most recent revision.</summary>
    public decimal PreviousAnnualCTC { get; set; }

    /// <summary>Gets or sets the increment percentage achieved in the recent revision.</summary>
    public decimal IncrementPercent { get; set; }

    /// <summary>Gets or sets the effective date of the latest compensation revision.</summary>
    public DateOnly EffectiveDate { get; set; }

    /// <summary>Gets or sets the timestamp of when this projection was last updated in UTC.</summary>
    public DateTimeOffset LastUpdatedAtUtc { get; set; }
}

