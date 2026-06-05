using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.TimeAttendance.Domain.Intelligence;

public enum AnomalyRuleType
{
    DayOfWeekPattern = 1,
    HolidaySandwichRepeat = 2,
    NoLeaveExtendedPeriod = 3,
    ExcessiveSickLeaveSpike = 4,
    HighAbsenceRate = 5,
    FridayMondayPattern = 6,
    ConsistentShortLeaves = 7,
}

public enum AnomalyFindingStatus
{
    Open = 1,
    UnderReview = 2,
    Dismissed = 3,
    Confirmed = 4,
    Escalated = 5,
}

/// <summary>
/// Configurable rule definition for absence pattern detection.
/// Engine evaluates all active rules against rolling attendance/leave history.
/// </summary>
public sealed class AnomalyRule : Entity<Guid>, ITenantOwned
{
    private AnomalyRule() { TenantId = string.Empty; }

    public string TenantId { get; private set; }
    public AnomalyRuleType RuleType { get; private set; }
    public string RuleName { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    /// <summary>JSON threshold parameters. E.g. {"dayOfWeek":"Monday","minOccurrences":3,"lookbackDays":90}</summary>
    public string ParametersJson { get; private set; } = string.Empty;
    public ComplianceSeverityLevel Severity { get; private set; }
    public bool IsActive { get; private set; }
    public bool ApplyToAllDepartments { get; private set; }

    private AnomalyRule(Guid id) : base(id) { TenantId = string.Empty; }

    public static AnomalyRule Create(
        string tenantId,
        AnomalyRuleType ruleType,
        string ruleName,
        string description,
        string parametersJson,
        ComplianceSeverityLevel severity,
        bool applyToAllDepartments = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(ruleName);
        ArgumentException.ThrowIfNullOrWhiteSpace(parametersJson);

        return new AnomalyRule(Guid.NewGuid())
        {
            TenantId = tenantId,
            RuleType = ruleType,
            RuleName = ruleName,
            Description = description,
            ParametersJson = parametersJson,
            Severity = severity,
            IsActive = true,
            ApplyToAllDepartments = applyToAllDepartments,
        };
    }

    public void Deactivate() => IsActive = false;
    public void UpdateParameters(string parametersJson) => ParametersJson = parametersJson;
}

public enum ComplianceSeverityLevel
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4,
}

/// <summary>
/// Finding raised when an employee's leave/attendance pattern matches an AnomalyRule.
/// One finding per rule-employee-period. Linked to investigation console.
/// </summary>
public sealed class AnomalyFinding : ITenantOwned
{
    private AnomalyFinding() { TenantId = string.Empty; }

    public Guid Id { get; private set; }
    public string TenantId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public Guid AnomalyRuleId { get; private set; }
    public AnomalyRuleType RuleType { get; private set; }
    public ComplianceSeverityLevel Severity { get; private set; }
    public AnomalyFindingStatus Status { get; private set; }
    public string Summary { get; private set; } = string.Empty;
    public string EvidenceJson { get; private set; } = string.Empty;
    public DateOnly DetectionPeriodStart { get; private set; }
    public DateOnly DetectionPeriodEnd { get; private set; }
    public DateTimeOffset DetectedAt { get; private set; }
    public string? ReviewedBy { get; private set; }
    public DateTimeOffset? ReviewedAt { get; private set; }

    public static AnomalyFinding Raise(
        string tenantId,
        Guid employeeId,
        Guid anomalyRuleId,
        AnomalyRuleType ruleType,
        ComplianceSeverityLevel severity,
        string summary,
        string evidenceJson,
        DateOnly periodStart,
        DateOnly periodEnd)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(summary);

        return new AnomalyFinding
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            AnomalyRuleId = anomalyRuleId,
            RuleType = ruleType,
            Severity = severity,
            Status = AnomalyFindingStatus.Open,
            Summary = summary,
            EvidenceJson = evidenceJson,
            DetectionPeriodStart = periodStart,
            DetectionPeriodEnd = periodEnd,
            DetectedAt = DateTimeOffset.UtcNow,
        };
    }

    public void Review(string reviewedBy, AnomalyFindingStatus newStatus)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reviewedBy);
        Status = newStatus;
        ReviewedBy = reviewedBy;
        ReviewedAt = DateTimeOffset.UtcNow;
    }
}
