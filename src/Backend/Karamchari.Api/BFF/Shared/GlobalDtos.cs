using Karamchari.TimeAttendance.Domain.Leaves;
using Karamchari.Payroll.Domain.SalaryStructures;
using Karamchari.Payroll.Domain.Statutory;

namespace Karamchari.Api.BFF.Common;

public record MapDlqRequest(Guid EmployeeId);

public record ReprocessAttendanceRequest(Guid? EmployeeId, string FromDate, string ToDate);

public record MarkFiledRequest(Guid RunId, Karamchari.Payroll.Domain.Compliance.ComplianceType Type, string ReferenceNumber);

public record TaxSimulationRequest(decimal Section80C, decimal Section80D, decimal MonthlyRent, bool IsMetro, int FinancialYear);

public record TaxSimulationResult(decimal OldRegimeTax, decimal NewRegimeTax, decimal Difference, string RecommendedRegime, string Reason);

public record StartPayrollRunRequest(string PeriodName);

public record AddHolidayRequest(string Name, DateOnly Date);

public record CreateLeavePolicyRequest(string Name, string Description, LeavePolicyRules Rules);

public record SubmitLeaveRequestRequest(Guid PolicyId, DateOnly StartDate, DateOnly EndDate, string? Reason);

public record CalculateLeaveDaysRequest(Guid PolicyId, DateOnly StartDate, DateOnly EndDate);

public record ProvisionTenantRequest(string CompanyName, string AdminEmail);

public record SubmitTimesheetRequest(DateOnly WeekStartDate, List<TimeEntryDto> Entries);

public record TimeEntryDto(DateOnly Date, decimal Hours, string? Description);

public record RejectTimesheetRequest(string Reason);

public record CreateSalaryComponentRequest(string Name, ComponentType Type, RoundingStrategy Rounding);

public record CreateSalaryTemplateRequest(string Name, List<SalaryTemplateComponent> Components);

public record CalculateCTCBreakdownRequest(decimal AnnualCTC, Dictionary<Guid, decimal>? Overrides);

public record CalculateStatutoryRequest(
    decimal AnnualCTC, 
    Guid? EmployeeId, 
    List<Guid> EpfBaseComponentIds, 
    Dictionary<Guid, decimal>? Overrides);

public record LockPayrollRunRequest(string ApprovedBy);

public record CreateClientRequest(
    string Name,
    string Gstin,
    string BillingAddress,
    string Currency = "INR");

public record CreateProjectRequest(
    Guid ClientId,
    string Name,
    string BillingType,
    string StartDate,
    string? EndDate = null);

public record AssignResourceRequest(
    Guid EmployeeId,
    decimal BillableRate,
    string Currency,
    string EffectiveFrom,
    bool IsBillable = true);

public record GenerateInvoiceRequest(
    Guid ClientId,
    string CutoffDate,
    string InvoiceNumberSeed,
    bool IsInterState = false);

public record CreateBillingContractRequest(
    Guid ClientId,
    Guid ProjectId,
    Karamchari.Billing.Domain.Contracts.BillingType BillingType,
    string Currency = "INR",
    string? StartDate = null,
    Karamchari.Billing.Domain.Contracts.BillingCycle Cycle = Karamchari.Billing.Domain.Contracts.BillingCycle.Monthly);

public record AddRateCardRequest(
    Guid RoleId,
    decimal Rate,
    Karamchari.Billing.Domain.Contracts.RateUnit Unit,
    string EffectiveFrom,
    string? EffectiveTo = null);

public record MapEmployeeRoleRequest(
    Guid EmployeeId,
    Guid RoleId,
    Guid? ProjectId,
    string EffectiveFrom,
    string? EffectiveTo = null);

public record GenerateInvoiceRunRequest(
    Guid ContractId,
    string StartDate,
    string EndDate);

public record FinalizeInvoiceRequest(string InvoiceNumber);

public record RecordPaymentRequest(decimal Amount, DateTimeOffset PaidAt);

public record SimulateRequest(
    decimal CurrentRevenue,
    decimal CurrentCost,
    decimal RateChangePercent = 0,
    decimal CostChangePercent = 0);
