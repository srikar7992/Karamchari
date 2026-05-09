namespace Karamchari.Payroll.Domain.Reconciliation;

public sealed class PayrollAnomaly
{
    public Guid Id { get; private set; }
    public AnomalyType Type { get; private set; }
    public AnomalySeverity Severity { get; private set; }
    public AnomalyResolutionStatus ResolutionStatus { get; private set; }

    public Guid? EmployeeId { get; private set; }
    public string? EmployeeName { get; private set; }
    public string? PeriodName { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal? AnomalyScore { get; private set; }

    // Evidence: structured JSON describing what was found
    public string Evidence { get; private set; } = string.Empty;

    public string? ResolvedBy { get; private set; }
    public DateTimeOffset? ResolvedAtUtc { get; private set; }
    public string? ResolutionNote { get; private set; }

    private PayrollAnomaly() { }

    public static PayrollAnomaly Detect(
        AnomalyType type,
        AnomalySeverity severity,
        string description,
        string evidence,
        Guid? employeeId = null,
        string? employeeName = null,
        string? periodName = null,
        decimal? anomalyScore = null)
    {
        return new PayrollAnomaly
        {
            Id = Guid.NewGuid(),
            Type = type,
            Severity = severity,
            Description = description,
            Evidence = evidence,
            EmployeeId = employeeId,
            EmployeeName = employeeName,
            PeriodName = periodName,
            AnomalyScore = anomalyScore,
            ResolutionStatus = AnomalyResolutionStatus.Open
        };
    }

    public void Resolve(string resolvedBy, string note)
    {
        ResolutionStatus = AnomalyResolutionStatus.Resolved;
        ResolvedBy = resolvedBy;
        ResolutionNote = note;
        ResolvedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Dismiss(string resolvedBy, string note)
    {
        ResolutionStatus = AnomalyResolutionStatus.Dismissed;
        ResolvedBy = resolvedBy;
        ResolutionNote = note;
        ResolvedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Acknowledge(string acknowledgedBy)
    {
        ResolutionStatus = AnomalyResolutionStatus.Acknowledged;
        ResolvedBy = acknowledgedBy;
    }
}
