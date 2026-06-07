using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.TimeAttendance.Domain.WorkforcePlanning;

public enum DemandPeriodType
{
    Daily = 1,
    Weekly = 2,
    Monthly = 3,
    Shift = 4,
}

public enum CoverageRiskLevel
{
    Low = 1,
    Moderate = 2,
    High = 3,
    Critical = 4,
}

/// <summary>
/// Staffing demand definition for a workforce group (Plant, Team, Department, Location) over a period.
/// Example: Plant A needs 150 operators on the morning shift on 2026-06-10.
/// Compared against WorkforceSupply to compute CapacityGap and CoverageRisk.
/// </summary>
public sealed class WorkforceDemand : AggregateRoot<Guid>, ITenantOwned
{
    private WorkforceDemand() { TenantId = string.Empty; }

    public string TenantId { get; private set; }
    public Guid GroupId { get; private set; }
    public string GroupType { get; private set; } = string.Empty;
    public string GroupName { get; private set; } = string.Empty;
    public DemandPeriodType PeriodType { get; private set; }
    public DateOnly PeriodStart { get; private set; }
    public DateOnly PeriodEnd { get; private set; }
    /// <summary>Minimum headcount required to operate at baseline.</summary>
    public int RequiredHeadcount { get; private set; }
    /// <summary>Headcount required for peak/optimal operation.</summary>
    public int OptimalHeadcount { get; private set; }
    /// <summary>Absolute minimum below which operations cannot continue (breach = CoverageRisk.Critical).</summary>
    public int CriticalMinimumHeadcount { get; private set; }
    public string? Shift { get; private set; }
    public string? Role { get; private set; }
    public bool IsPublished { get; private set; }

    public static WorkforceDemand Create(
        string tenantId,
        Guid groupId,
        string groupType,
        string groupName,
        DemandPeriodType periodType,
        DateOnly periodStart,
        DateOnly periodEnd,
        int requiredHeadcount,
        int optimalHeadcount,
        int criticalMinimumHeadcount,
        string? shift = null,
        string? role = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(groupName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(requiredHeadcount);

        return new WorkforceDemand
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            GroupId = groupId,
            GroupType = groupType,
            GroupName = groupName,
            PeriodType = periodType,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            RequiredHeadcount = requiredHeadcount,
            OptimalHeadcount = Math.Max(requiredHeadcount, optimalHeadcount),
            CriticalMinimumHeadcount = Math.Min(criticalMinimumHeadcount, requiredHeadcount),
            Shift = shift,
            Role = role,
            IsPublished = false,
        };
    }

    public void Publish() => IsPublished = true;
    public void Revise(int required, int optimal, int criticalMin)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(required);
        RequiredHeadcount = required;
        OptimalHeadcount = Math.Max(required, optimal);
        CriticalMinimumHeadcount = Math.Min(criticalMin, required);
    }
}

/// <summary>
/// Computed actual supply (expected available headcount) for a demand period.
/// Derived from WorkforceAvailabilityIndex + approved leave + roster assignments.
/// </summary>
public sealed class WorkforceSupply : ITenantOwned
{
    private WorkforceSupply() { TenantId = string.Empty; }

    public Guid Id { get; private set; }
    public string TenantId { get; private set; }
    public Guid DemandId { get; private set; }
    public Guid GroupId { get; private set; }
    public DateOnly PeriodStart { get; private set; }
    public DateOnly PeriodEnd { get; private set; }
    public int TotalHeadcount { get; private set; }
    public int OnLeave { get; private set; }
    public int OnRoster { get; private set; }
    public int ExpectedAvailable { get; private set; }
    public DateTimeOffset ComputedAt { get; private set; }

    public static WorkforceSupply Compute(
        string tenantId,
        Guid demandId,
        Guid groupId,
        DateOnly periodStart,
        DateOnly periodEnd,
        int totalHeadcount,
        int onLeave,
        int onRoster)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        int available = Math.Max(0, totalHeadcount - onLeave - (totalHeadcount - onRoster));

        return new WorkforceSupply
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            DemandId = demandId,
            GroupId = groupId,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            TotalHeadcount = totalHeadcount,
            OnLeave = onLeave,
            OnRoster = onRoster,
            ExpectedAvailable = available,
            ComputedAt = DateTimeOffset.UtcNow,
        };
    }
}

/// <summary>
/// Computed gap between demand and supply. Negative gap = shortfall.
/// Drives CoverageRisk classification and manager alerts.
/// </summary>
public sealed class CapacityGap : ITenantOwned
{
    private CapacityGap() { TenantId = string.Empty; }

    public Guid Id { get; private set; }
    public string TenantId { get; private set; }
    public Guid DemandId { get; private set; }
    public Guid SupplyId { get; private set; }
    public Guid GroupId { get; private set; }
    public DateOnly PeriodStart { get; private set; }
    public DateOnly PeriodEnd { get; private set; }
    public int RequiredHeadcount { get; private set; }
    public int ExpectedAvailable { get; private set; }
    /// <summary>Positive = surplus. Negative = shortfall. The number managers care about.</summary>
    public int GapHeadcount { get; private set; }
    public decimal GapPercent { get; private set; }
    public CoverageRiskLevel RiskLevel { get; private set; }
    public DateTimeOffset ComputedAt { get; private set; }

    public static CapacityGap Compute(
        string tenantId,
        Guid demandId,
        Guid supplyId,
        Guid groupId,
        DateOnly periodStart,
        DateOnly periodEnd,
        int requiredHeadcount,
        int criticalMinimum,
        int expectedAvailable)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        int gap = expectedAvailable - requiredHeadcount;
        decimal gapPct = requiredHeadcount == 0
            ? 0m
            : Math.Round((decimal)gap / requiredHeadcount * 100m, 1);

        CoverageRiskLevel riskLevel = expectedAvailable <= criticalMinimum
            ? CoverageRiskLevel.Critical
            : gap < 0 && gapPct < -20m
                ? CoverageRiskLevel.High
                : gap < 0
                    ? CoverageRiskLevel.Moderate
                    : CoverageRiskLevel.Low;

        return new CapacityGap
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            DemandId = demandId,
            SupplyId = supplyId,
            GroupId = groupId,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            RequiredHeadcount = requiredHeadcount,
            ExpectedAvailable = expectedAvailable,
            GapHeadcount = gap,
            GapPercent = gapPct,
            RiskLevel = riskLevel,
            ComputedAt = DateTimeOffset.UtcNow,
        };
    }
}

/// <summary>
/// Forward-looking coverage risk alert for operations and HR.
/// Example: "Support Team — forecasted leave 22% next week — SLA breach risk HIGH"
/// </summary>
public sealed class CoverageRisk : ITenantOwned
{
    private CoverageRisk() { TenantId = string.Empty; }

    public Guid Id { get; private set; }
    public string TenantId { get; private set; }
    public Guid GroupId { get; private set; }
    public string GroupType { get; private set; } = string.Empty;
    public string GroupName { get; private set; } = string.Empty;
    public DateOnly RiskDate { get; private set; }
    public CoverageRiskLevel RiskLevel { get; private set; }
    public string RiskReason { get; private set; } = string.Empty;
    public decimal ForecastedLeavePercent { get; private set; }
    public int ForecastedShortfall { get; private set; }
    public bool SlaBreachRisk { get; private set; }
    public bool AlertSent { get; private set; }
    public bool Acknowledged { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static CoverageRisk Raise(
        string tenantId,
        Guid groupId,
        string groupType,
        string groupName,
        DateOnly riskDate,
        CoverageRiskLevel riskLevel,
        string riskReason,
        decimal forecastedLeavePercent,
        int forecastedShortfall,
        bool slaBreachRisk)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(riskReason);

        return new CoverageRisk
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            GroupId = groupId,
            GroupType = groupType,
            GroupName = groupName,
            RiskDate = riskDate,
            RiskLevel = riskLevel,
            RiskReason = riskReason,
            ForecastedLeavePercent = forecastedLeavePercent,
            ForecastedShortfall = forecastedShortfall,
            SlaBreachRisk = slaBreachRisk,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void MarkAlertSent() => AlertSent = true;
    public void Acknowledge() => Acknowledged = true;
}
