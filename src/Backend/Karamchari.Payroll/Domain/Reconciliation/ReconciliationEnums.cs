namespace Karamchari.Payroll.Domain.Reconciliation;

public enum ReconciliationJobStatus
{
    Running,
    Completed,
    CompletedWithAnomalies,
    Failed
}

public enum AnomalyType
{
    DuplicatePayout,
    MissingDeduction,
    NegativeNetPay,
    TaxMismatch,
    ArrearsMismatch,
    LoanDeductionMismatch,
    FailedDisbursement,
    OrphanedPayrollState,
    ComplianceMismatch,
    GrossVarianceHigh
}

public enum AnomalySeverity
{
    Info,
    Warning,
    Critical
}

public enum AnomalyResolutionStatus
{
    Open,
    Acknowledged,
    Resolved,
    Dismissed
}
