// Integration event contracts shared across all bounded contexts.
// These are pure data records — no domain logic, no EF entities, no framework dependencies.
// Versioned using nested namespaces so consumers can pin to a stable version
// while producers gradually migrate to V2+.

namespace Karamchari.Core.Contracts.IntegrationEvents
{
    // ── Common events that have not evolved yet ─────────────────────────────

    public record TimesheetApprovedIntegrationEvent(
        Guid TimesheetId,
        Guid EmployeeId,
        DateOnly WeekStartDate,
        decimal TotalHours);

    public record LeaveRequestApprovedIntegrationEvent(
        Guid RequestId,
        Guid EmployeeId,
        DateOnly StartDate,
        DateOnly EndDate,
        double TotalDays,
        bool IsPaid);

    public record PayrollRunLockedIntegrationEvent(
        Guid RunId,
        string TenantId,
        string PeriodName);

    public record TenantProvisionedIntegrationEvent(
        string TenantId,
        string CompanyName,
        string AdminEmail);

    /// <summary>
    /// Event published when a tax declaration is rejected by HR.
    /// Triggers a notification to the employee.
    /// </summary>
    public record ITDeclarationRejectedIntegrationEvent(
        Guid DeclarationId,
        Guid EmployeeId,
        string Category,
        string Reason);
}

namespace Karamchari.Core.Contracts.IntegrationEvents.V1
{
    // V1 Contracts — original baseline.
    // Pin consumers to V1 if they don't yet need the V2 additions.

    public record EmployeeOnboardedIntegrationEvent(
        Guid EmployeeId,
        string EmployeeNumber,
        string LegalName,
        string? WorkEmail,
        DateOnly HiredOn);

    /// <summary>
    /// V1: does NOT include EmployeeName — consumers must look it up separately.
    /// Prefer <see cref="V2.PayrollRunCompletedIntegrationEvent"/> for new consumers.
    /// </summary>
    public record PayrollRunCompletedIntegrationEvent(
        Guid EmployeeId,
        string TenantId,
        string PeriodName,
        decimal Gross,
        decimal NetPay,
        Dictionary<string, decimal> Earnings,
        Dictionary<string, decimal> Deductions,
        string TaxRegime);
}

namespace Karamchari.Core.Contracts.IntegrationEvents.V2
{
    // V2 Contracts — evolved versions.

    /// <summary>
    /// Evolved version of PayrollRunCompleted that includes <see cref="EmployeeName"/>
    /// to avoid cross-context lookups during payslip generation.
    /// </summary>
    public record PayrollRunCompletedIntegrationEvent(
        Guid EmployeeId,
        string EmployeeName,
        string TenantId,
        string PeriodName,
        decimal Gross,
        decimal NetPay,
        Dictionary<string, decimal> Earnings,
        Dictionary<string, decimal> Deductions,
        string TaxRegime);
}
