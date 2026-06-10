// -----------------------------------------------------------------------
// <copyright file="PayrollDtos.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Api.BFF.Payroll;

// Duplicate records removed. They are now defined in their respective endpoint files
// for better vertical slice locality or are unique to those files.

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record PayrollCockpitSummaryDto(
    int FnFQueueCount,
    int ArrearQueueCount,
    int CorrectionQueueCount,
    int OpenAnomalyCount,
    int ActiveLoanCount,
    int PendingReimbursementCount);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record PayrollCockpitQueueItemDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    string Category,
    string Status,
    decimal Amount,
    DateTimeOffset CreatedAt);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record PayrollAnomalyDto(
    Guid Id,
    string Type,
    string Severity,
    string Status,
    Guid EmployeeId,
    string EmployeeName,
    string Description,
    decimal? AnomalyScore);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record PayrollCockpitDto(
    PayrollCockpitSummaryDto Summary,
    List<PayrollCockpitQueueItemDto> FnFQueue,
    List<PayrollCockpitQueueItemDto> ArrearQueue,
    List<PayrollCockpitQueueItemDto> CorrectionQueue,
    List<PayrollAnomalyDto> OpenAnomalies);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
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

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record CreateLoanRequest(
    Guid EmployeeId, string EmployeeName, string LoanType, string InterestType,
    decimal PrincipalAmount, decimal InterestRatePercent, int TenureMonths, DateOnly DisbursedOn);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record LoanDto(
    Guid Id, Guid EmployeeId, string EmployeeName, string LoanType,
    string Status, decimal PrincipalAmount, decimal OutstandingBalance,
    decimal MonthlyEmi, int TenureMonths, DateOnly DisbursedOn);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record SubmitReimbursementRequest(
    string Category, string Description, decimal ClaimedAmount,
    DateOnly ExpenseDate, string? AttachmentBlobPath,
    string? AttachmentFileName, string? AttachmentHash);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record ReimbursementClaimDto(
    Guid Id, Guid EmployeeId, string EmployeeName, string Category,
    string Status, decimal ClaimedAmount, decimal ApprovedAmount,
    decimal PolicyLimit, string Description, DateOnly ExpenseDate,
    string FraudIndicator, bool IsClawedBack, DateTimeOffset CreatedAt);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record LoanInstallmentDto(
    int InstallmentNumber, string PeriodName,
    decimal PrincipalAmount, decimal InterestAmount, decimal TotalAmount,
    decimal OutstandingAfter, string Status);
