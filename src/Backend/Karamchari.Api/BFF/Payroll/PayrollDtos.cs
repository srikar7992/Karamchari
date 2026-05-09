namespace Karamchari.Api.BFF.Payroll;

// ── FnF DTOs ─────────────────────────────────────────────────────────────────

public record InitiateFnFRequest(
    Guid EmployeeId,
    string EmployeeName,
    string ExitType,
    DateOnly LastWorkingDay);

public record FnFLineItemDto(
    string Type,
    string Description,
    decimal Amount,
    bool IsDeduction,
    bool IsTaxable);

public record FnFSettlementDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    string ExitType,
    DateOnly LastWorkingDay,
    string Status,
    decimal TotalEarnings,
    decimal TotalDeductions,
    decimal NetSettlementAmount,
    IReadOnlyList<FnFLineItemDto> LineItems,
    DateTimeOffset CreatedAtUtc);

// ── Arrear DTOs ───────────────────────────────────────────────────────────────

public record ArrearCalculationDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    string TriggerType,
    string Status,
    DateOnly EffectiveFrom,
    DateOnly EffectiveTo,
    decimal TotalNetDelta,
    decimal TotalGrossDelta,
    IReadOnlyList<ArrearPeriodDiffDto> PeriodDiffs);

public record ArrearPeriodDiffDto(
    string PeriodName,
    decimal OriginalGross,
    decimal RecalculatedGross,
    decimal GrossDelta,
    decimal NetDelta,
    decimal TdsDelta);

// ── Correction DTOs ───────────────────────────────────────────────────────────

public record SubmitCorrectionRequest(
    Guid EmployeeId,
    string EmployeeName,
    string CorrectionType,
    string CorrectionScope,
    string AffectedPeriodName,
    int AffectedYear,
    int AffectedMonth,
    string ChangeDescription,
    string ChangeDetails,
    bool AfterBankDisbursement,
    bool AfterTaxFiling,
    bool AfterEmployeeExit);

public record PayrollCorrectionDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    string Type,
    string Scope,
    string Status,
    string AffectedPeriodName,
    string ChangeDescription,
    decimal? DifferentialAmount,
    DateTimeOffset CreatedAtUtc);

// ── Reimbursement DTOs ────────────────────────────────────────────────────────

public record SubmitReimbursementRequest(
    string Category,
    string Description,
    decimal ClaimedAmount,
    DateOnly ExpenseDate,
    string? AttachmentBlobPath,
    string? AttachmentFileName,
    string? AttachmentHash);

public record ReimbursementClaimDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    string Category,
    string Status,
    decimal ClaimedAmount,
    decimal ApprovedAmount,
    decimal PolicyLimit,
    string Description,
    DateOnly ExpenseDate,
    string FraudIndicator,
    bool IsClawedBack,
    DateTimeOffset CreatedAtUtc);

// ── Loan DTOs ─────────────────────────────────────────────────────────────────

public record CreateLoanRequest(
    Guid EmployeeId,
    string EmployeeName,
    string LoanType,
    string InterestType,
    decimal PrincipalAmount,
    decimal InterestRatePercent,
    int TenureMonths,
    DateOnly DisbursedOn);

public record LoanDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    string Type,
    string Status,
    decimal PrincipalAmount,
    decimal OutstandingBalance,
    decimal MonthlyEmi,
    int TenureMonths,
    DateOnly DisbursedOn);

public record LoanInstallmentDto(
    int InstallmentNumber,
    string PeriodName,
    decimal PrincipalAmount,
    decimal InterestAmount,
    decimal TotalAmount,
    decimal OutstandingAfter,
    string Status);

// ── Variable Pay DTOs ─────────────────────────────────────────────────────────

public record AllocateVariablePayRequest(
    Guid EmployeeId,
    string EmployeeName,
    string VariablePayType,
    decimal AllocatedAmount,
    string TaxTreatment,
    string? PerformancePeriod,
    DateOnly? ScheduledPayoutDate,
    int ClawbackWindowMonths);

public record VariablePayAllocationDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    string Type,
    string Status,
    decimal AllocatedAmount,
    decimal ProratedAmount,
    decimal PaidAmount,
    string? PayoutPeriodName,
    bool IsClawedBack,
    DateTimeOffset CreatedAtUtc);

// ── Salary Revision DTOs ──────────────────────────────────────────────────────

public record CreateRevisionRequest(
    Guid EmployeeId,
    string EmployeeName,
    string RevisionType,
    decimal PreviousCTC,
    decimal NewCTC,
    DateOnly EffectiveFrom,
    bool RequiresArrears,
    string? Remarks);

public record SalaryRevisionDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    string Type,
    string Status,
    decimal PreviousCTC,
    decimal NewCTC,
    decimal IncrementPercent,
    DateOnly EffectiveFrom,
    bool RequiresArrears,
    DateTimeOffset CreatedAtUtc);

// ── Simulation DTOs ───────────────────────────────────────────────────────────

public record RunSimulationRequest(
    string SimulationType,
    IReadOnlyList<Guid>? EmployeeIds,
    decimal? GlobalCTCAdjustmentPercent,
    Dictionary<Guid, decimal>? PerEmployeeCTC,
    string? ForPeriodName);

public record SimulationResultDto(
    Guid SimulationId,
    string Type,
    string Status,
    decimal TotalProjectedGross,
    decimal TotalProjectedNet,
    decimal TotalProjectedDelta,
    IReadOnlyList<SimulationEmployeeResultDto> Results,
    DateTimeOffset CreatedAtUtc);

public record SimulationEmployeeResultDto(
    Guid EmployeeId,
    string EmployeeName,
    decimal CurrentGross,
    decimal ProjectedGross,
    decimal CurrentNet,
    decimal ProjectedNet,
    decimal NetDelta,
    IReadOnlyDictionary<string, decimal> ComponentBreakdown);

// ── Disbursement DTOs ─────────────────────────────────────────────────────────

public record InitiateDisbursementRequest(
    Guid RunId,
    string PeriodName,
    string BankProvider);

public record DisbursementBatchDto(
    Guid Id,
    Guid RunId,
    string PeriodName,
    string BankProvider,
    string Status,
    decimal TotalAmount,
    int TotalEntries,
    int SuccessCount,
    int FailedCount,
    int RetryCount,
    DateTimeOffset CreatedAtUtc);

// ── Reconciliation DTOs ───────────────────────────────────────────────────────

public record ReconciliationJobDto(
    Guid Id,
    string PeriodName,
    string Status,
    int EmployeesScanned,
    int TotalAnomalies,
    int CriticalCount,
    int WarningCount,
    decimal AnomalyScore,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc);

public record PayrollAnomalyDto(
    Guid Id,
    string Type,
    string Severity,
    string ResolutionStatus,
    Guid? EmployeeId,
    string? EmployeeName,
    string Description,
    decimal? AnomalyScore);

// ── Cockpit DTOs ──────────────────────────────────────────────────────────────

public record PayrollCockpitDto(
    PayrollCockpitSummaryDto Summary,
    IReadOnlyList<PayrollCockpitQueueItemDto> FnFQueue,
    IReadOnlyList<PayrollCockpitQueueItemDto> ArrearQueue,
    IReadOnlyList<PayrollCockpitQueueItemDto> CorrectionQueue,
    IReadOnlyList<PayrollAnomalyDto> OpenAnomalies);

public record PayrollCockpitSummaryDto(
    int ActiveFnFSettlements,
    int PendingArrears,
    int PendingCorrections,
    int OpenAnomalies,
    int ActiveLoans,
    int PendingReimbursements);

public record PayrollCockpitQueueItemDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    string Type,
    string Status,
    decimal Amount,
    DateTimeOffset CreatedAtUtc);
