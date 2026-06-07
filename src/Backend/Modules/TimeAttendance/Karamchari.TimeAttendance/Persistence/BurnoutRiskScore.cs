using Karamchari.Core.Multitenancy;

namespace Karamchari.TimeAttendance.Persistence;

public enum BurnoutRiskLevel
{
    Low = 1,
    Moderate = 2,
    High = 3,
    Critical = 4,
}

/// <summary>
/// Burnout risk score per employee computed from cross-domain signals:
/// leave absence (>120 days no leave), overtime frequency, weekend work count,
/// attendance anomalies, and roster violations.
/// Score 0-100. Computed monthly by ScheduledDomainAction.
/// </summary>
public sealed class BurnoutRiskScore : ITenantOwned
{
    private BurnoutRiskScore() { TenantId = string.Empty; }

    public Guid Id { get; private set; }
    public string TenantId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public int ComputationYear { get; private set; }
    public int ComputationMonth { get; private set; }

    // Input signals
    public int DaysSinceLastLeave { get; private set; }
    public decimal OvertimeHoursLast90Days { get; private set; }
    public int WeekendWorkDaysLast90Days { get; private set; }
    public int AnomalyFindingsLast90Days { get; private set; }
    public int RosterViolationsLast90Days { get; private set; }

    // Score
    public decimal Score { get; private set; }
    public BurnoutRiskLevel RiskLevel { get; private set; }
    public DateTimeOffset ComputedAt { get; private set; }
    public bool AlertSent { get; private set; }

    public static BurnoutRiskScore Compute(
        string tenantId,
        Guid employeeId,
        int year,
        int month,
        int daysSinceLastLeave,
        decimal overtimeHoursLast90Days,
        int weekendWorkDaysLast90Days,
        int anomalyFindingsLast90Days,
        int rosterViolationsLast90Days)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        // Weighted scoring:
        // No-leave signal: 40 pts max (>120 days → 40, >90 → 25, >60 → 10, else 0)
        // Overtime signal: 30 pts max (>60h/90d → 30, >40h → 20, >20h → 10)
        // Weekend work: 20 pts max (>8 days → 20, >4 → 10)
        // Anomaly/violations: 10 pts max

        decimal noLeaveScore = daysSinceLastLeave switch
        {
            > 120 => 40m,
            > 90 => 25m,
            > 60 => 10m,
            _ => 0m,
        };

        decimal overtimeScore = overtimeHoursLast90Days switch
        {
            > 60 => 30m,
            > 40 => 20m,
            > 20 => 10m,
            _ => 0m,
        };

        decimal weekendScore = weekendWorkDaysLast90Days switch
        {
            > 8 => 20m,
            > 4 => 10m,
            _ => 0m,
        };

        decimal anomalyScore = Math.Min(10m, (anomalyFindingsLast90Days + rosterViolationsLast90Days) * 2m);

        decimal score = noLeaveScore + overtimeScore + weekendScore + anomalyScore;

        BurnoutRiskLevel riskLevel = score switch
        {
            >= 80 => BurnoutRiskLevel.Critical,
            >= 55 => BurnoutRiskLevel.High,
            >= 30 => BurnoutRiskLevel.Moderate,
            _ => BurnoutRiskLevel.Low,
        };

        return new BurnoutRiskScore
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            ComputationYear = year,
            ComputationMonth = month,
            DaysSinceLastLeave = daysSinceLastLeave,
            OvertimeHoursLast90Days = overtimeHoursLast90Days,
            WeekendWorkDaysLast90Days = weekendWorkDaysLast90Days,
            AnomalyFindingsLast90Days = anomalyFindingsLast90Days,
            RosterViolationsLast90Days = rosterViolationsLast90Days,
            Score = score,
            RiskLevel = riskLevel,
            ComputedAt = DateTimeOffset.UtcNow,
        };
    }

    public void MarkAlertSent() => AlertSent = true;
}
