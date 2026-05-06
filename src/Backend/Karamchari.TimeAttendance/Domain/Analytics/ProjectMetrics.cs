using Karamchari.Core.Multitenancy;

namespace Karamchari.TimeAttendance.Domain.Analytics;

/// <summary>
/// Pre-aggregated daily metrics per project.
/// Written by event consumers; queried by the CFO dashboard.
/// A clustered columnstore index on this table delivers sub-second time-series
/// queries at 100k+ rows without a data warehouse.
/// </summary>
public sealed class ProjectMetrics : ITenantOwned
{
    public string TenantId { get; set; } = string.Empty;
    public Guid ProjectId { get; set; }

    /// <summary>Calendar date (daily grain).</summary>
    public DateOnly Date { get; set; }

    public decimal BillableHours { get; set; }
    public decimal TotalHours { get; set; }
    public decimal Revenue { get; set; }
    public decimal Cost { get; set; }
    public decimal CollectedAmount { get; set; }
    public decimal Profit => Revenue - Cost;
    public decimal Utilization => TotalHours > 0 ? BillableHours / TotalHours : 0;

    /// <summary>EventId of the last event that updated this row. Used for DB-level idempotency.</summary>
    public Guid? LastEventId { get; set; }

    /// <summary>OccurredAt of the last processed event. Used for last-write-wins ordering guard.</summary>
    public DateTimeOffset? LastProcessedOccurredAt { get; set; }

    public DateTimeOffset LastUpdatedAt { get; set; }
}

/// <summary>
/// Pre-aggregated daily metrics per employee.
/// </summary>
public sealed class EmployeeMetrics : ITenantOwned
{
    public string TenantId { get; set; } = string.Empty;
    public Guid EmployeeId { get; set; }
    public DateOnly Date { get; set; }

    public decimal BillableHours { get; set; }
    public decimal TotalHours { get; set; }
    public decimal UtilizationRate => TotalHours > 0 ? BillableHours / TotalHours : 0;

    /// <summary>EventId of the last event that updated this row. Used for DB-level idempotency.</summary>
    public Guid? LastEventId { get; set; }

    /// <summary>OccurredAt of the last processed event. Used for last-write-wins ordering guard.</summary>
    public DateTimeOffset? LastProcessedOccurredAt { get; set; }

    public DateTimeOffset LastUpdatedAt { get; set; }
}
