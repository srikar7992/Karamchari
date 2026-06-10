// -----------------------------------------------------------------------
// <copyright file="GlobalDtos.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Payroll.Domain.SalaryStructures;
using Karamchari.Payroll.Domain.Statutory;
using Karamchari.TimeAttendance.Domain.Leaves;

namespace Karamchari.Api.BFF.Common;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record MapDlqRequest(Guid EmployeeId);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record ReprocessAttendanceRequest(Guid? EmployeeId, string FromDate, string ToDate);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record MarkFiledRequest(Guid RunId, Karamchari.Payroll.Domain.Compliance.ComplianceType Type, string ReferenceNumber);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record TaxSimulationRequest(decimal Section80C, decimal Section80D, decimal MonthlyRent, bool IsMetro, int FinancialYear);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record TaxSimulationResult(decimal OldRegimeTax, decimal NewRegimeTax, decimal Difference, string RecommendedRegime, string Reason);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record StartPayrollRunRequest(string PeriodName);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record AddHolidayRequest(string Name, DateOnly Date);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record CreateLeavePolicyRequest(string Name, string Description, LeavePolicyRules Rules);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record SubmitLeaveRequestRequest(Guid PolicyId, DateOnly StartDate, DateOnly EndDate, string? Reason);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record CalculateLeaveDaysRequest(Guid PolicyId, DateOnly StartDate, DateOnly EndDate);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record ProvisionTenantRequest(string CompanyName, string AdminEmail);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record SubmitTimesheetRequest(DateOnly WeekStartDate, List<TimeEntryDto> Entries);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record TimeEntryDto(DateOnly Date, decimal Hours, string? Description);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record RejectTimesheetRequest(string Reason);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record CreateSalaryComponentRequest(string Name, ComponentType Type, RoundingStrategy Rounding);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record CreateSalaryTemplateRequest(string Name, List<SalaryTemplateComponent> Components);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record CalculateCTCBreakdownRequest(decimal AnnualCTC, Dictionary<Guid, decimal>? Overrides);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record CalculateStatutoryRequest(
    decimal AnnualCTC,
    Guid? EmployeeId,
    List<Guid> EpfBaseComponentIds,
    Dictionary<Guid, decimal>? Overrides);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record LockPayrollRunRequest(string ApprovedBy);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record CreateClientRequest(
    string Name,
    string Gstin,
    string BillingAddress,
    string Currency = "INR");

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record CreateProjectRequest(
    Guid ClientId,
    string Name,
    string BillingType,
    string StartDate,
    string? EndDate = null);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record AssignResourceRequest(
    Guid EmployeeId,
    decimal BillableRate,
    string Currency,
    string EffectiveFrom,
    bool IsBillable = true);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record GenerateInvoiceRequest(
    Guid ClientId,
    string CutoffDate,
    string InvoiceNumberSeed,
    bool IsInterState = false);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record CreateBillingContractRequest(
    Guid ClientId,
    Guid ProjectId,
    Karamchari.Billing.Domain.Contracts.BillingType BillingType,
    string Currency = "INR",
    string? StartDate = null,
    Karamchari.Billing.Domain.Contracts.BillingCycle Cycle = Karamchari.Billing.Domain.Contracts.BillingCycle.Monthly);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record AddRateCardRequest(
    Guid RoleId,
    decimal Rate,
    Karamchari.Billing.Domain.Contracts.RateUnit Unit,
    string EffectiveFrom,
    string? EffectiveTo = null);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record MapEmployeeRoleRequest(
    Guid EmployeeId,
    Guid RoleId,
    Guid? ProjectId,
    string EffectiveFrom,
    string? EffectiveTo = null);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record GenerateInvoiceRunRequest(
    Guid ContractId,
    string StartDate,
    string EndDate);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record FinalizeInvoiceRequest(string InvoiceNumber);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record RecordPaymentRequest(decimal Amount, DateTimeOffset PaidAt);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record SimulateRequest(
    decimal CurrentRevenue,
    decimal CurrentCost,
    decimal RateChangePercent = 0,
    decimal CostChangePercent = 0);
