namespace Karamchari.Api.BFF.Payroll;

// Duplicate records removed. They are now defined in their respective endpoint files
// for better vertical slice locality or are unique to those files.

public record PayrollCockpitSummaryDto(
    int FnFQueueCount,
    int ArrearQueueCount,
    int CorrectionQueueCount,
    int OpenAnomalyCount,
    int ActiveLoanCount,
    int PendingReimbursementCount);

public record PayrollCockpitQueueItemDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    string Category,
    string Status,
    decimal Amount,
    DateTimeOffset CreatedAt);

public record PayrollAnomalyDto(
    Guid Id,
    string Type,
    string Severity,
    string Status,
    Guid EmployeeId,
    string EmployeeName,
    string Description,
    decimal? AnomalyScore);

public record PayrollCockpitDto(
    PayrollCockpitSummaryDto Summary,
    List<PayrollCockpitQueueItemDto> FnFQueue,
    List<PayrollCockpitQueueItemDto> ArrearQueue,
    List<PayrollCockpitQueueItemDto> CorrectionQueue,
    List<PayrollAnomalyDto> OpenAnomalies);

public record ReconciliationJobDto(
    Guid Id,
    string PeriodName,
    string Status,
    int EmployeesScanned,
    int TotalAnomalies,
    int CriticalCount,
    int WarningCount,
    decimal AnomalyScore,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt);

public record CreateLoanRequest(
    Guid EmployeeId, string EmployeeName, string LoanType, string InterestType,
    decimal PrincipalAmount, decimal InterestRatePercent, int TenureMonths, DateOnly DisbursedOn);

public record LoanDto(
    Guid Id, Guid EmployeeId, string EmployeeName, string LoanType,
    string Status, decimal PrincipalAmount, decimal OutstandingBalance,
    decimal MonthlyEmi, int TenureMonths, DateOnly DisbursedOn);

public record SubmitReimbursementRequest(
    string Category, string Description, decimal ClaimedAmount,
    DateOnly ExpenseDate, string? AttachmentBlobPath,
    string? AttachmentFileName, string? AttachmentHash);

public record ReimbursementClaimDto(
    Guid Id, Guid EmployeeId, string EmployeeName, string Category,
    string Status, decimal ClaimedAmount, decimal ApprovedAmount,
    decimal PolicyLimit, string Description, DateOnly ExpenseDate,
    string FraudIndicator, bool IsClawedBack, DateTimeOffset CreatedAt);

public record LoanInstallmentDto(
    int InstallmentNumber, string PeriodName,
    decimal PrincipalAmount, decimal InterestAmount, decimal TotalAmount,
    decimal OutstandingAfter, string Status);
