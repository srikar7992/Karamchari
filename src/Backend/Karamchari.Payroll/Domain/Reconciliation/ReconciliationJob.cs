using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Payroll.Domain.Reconciliation;

/// <summary>
/// Aggregate for a nightly payroll reconciliation run.
/// Detects anomalies across disbursement, deductions, compliance, and ledger state.
/// </summary>
public sealed class ReconciliationJob : AggregateRoot<Guid>
{
    private readonly List<PayrollAnomaly> _anomalies = [];

    public string TenantId { get; private set; } = string.Empty;
    public string PeriodName { get; private set; } = string.Empty;
    public ReconciliationJobStatus Status { get; private set; }

    public int EmployeesScanned { get; private set; }
    public int TotalAnomalies { get; private set; }
    public int CriticalCount { get; private set; }
    public int WarningCount { get; private set; }
    public decimal AnomalyScore { get; private set; }  // 0-100, higher = more issues

    public string? ErrorMessage { get; private set; }
    public DateTimeOffset StartedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public IReadOnlyCollection<PayrollAnomaly> Anomalies => _anomalies.AsReadOnly();

    private ReconciliationJob() { }

    public static ReconciliationJob Start(string tenantId, string periodName)
    {
        return new ReconciliationJob
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PeriodName = periodName,
            Status = ReconciliationJobStatus.Running,
            StartedAtUtc = DateTimeOffset.UtcNow
        };
    }

    public void AddAnomaly(PayrollAnomaly anomaly)
    {
        if (Status != ReconciliationJobStatus.Running)
            throw new InvalidOperationException("Cannot add anomaly to completed reconciliation job.");

        _anomalies.Add(anomaly);
    }

    public void Complete(int employeesScanned)
    {
        if (Status != ReconciliationJobStatus.Running)
            throw new InvalidOperationException($"Cannot complete reconciliation in status {Status}.");

        EmployeesScanned = employeesScanned;
        TotalAnomalies = _anomalies.Count;
        CriticalCount = _anomalies.Count(a => a.Severity == AnomalySeverity.Critical);
        WarningCount = _anomalies.Count(a => a.Severity == AnomalySeverity.Warning);

        // Score: weighted by severity — critical=10, warning=3, info=1 — normalized per 100 employees
        var rawScore = CriticalCount * 10 + WarningCount * 3 + (_anomalies.Count - CriticalCount - WarningCount);
        AnomalyScore = employeesScanned > 0
            ? Math.Min(100, rawScore / (decimal)employeesScanned * 10)
            : 0;

        Status = TotalAnomalies > 0
            ? ReconciliationJobStatus.CompletedWithAnomalies
            : ReconciliationJobStatus.Completed;

        CompletedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Fail(string error)
    {
        Status = ReconciliationJobStatus.Failed;
        ErrorMessage = error;
        CompletedAtUtc = DateTimeOffset.UtcNow;
    }
}
