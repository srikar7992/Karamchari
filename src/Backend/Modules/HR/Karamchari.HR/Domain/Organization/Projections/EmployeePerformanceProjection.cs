using System;

namespace Karamchari.HR.Domain.Organization.Projections;

/// <summary>
/// Read model representing a cache of employee performance metrics populated by Performance integration events.
/// Used as input in flight risk and promotion readiness calculations.
/// </summary>
public sealed class EmployeePerformanceProjection
{
    /// <summary>Gets or sets the tenant identifier associated with the employee.</summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>Gets or sets the unique identifier of the employee.</summary>
    public Guid EmployeeId { get; set; }

    /// <summary>Gets or sets the composite score achieved by the employee in the recent review cycle.</summary>
    public decimal CompositeScore { get; set; }

    /// <summary>Gets or sets the name of the performance rating bucket (e.g. Meets Expectations).</summary>
    public string PerformanceBucket { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the employee is calibrated as a high performer.</summary>
    public bool IsHighPerformer { get; set; }

    /// <summary>Gets or sets the timestamp of when this projection was last updated in UTC.</summary>
    public DateTimeOffset LastUpdatedAtUtc { get; set; }
}

