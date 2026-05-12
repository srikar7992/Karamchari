// Integration event contracts shared across all bounded contexts.
// These are pure data records â€” no domain logic, no EF entities, no framework dependencies.
// Versioned using nested namespaces so consumers can pin to a stable version
// while producers gradually migrate to V2+.

namespace Karamchari.Core.Contracts.IntegrationEvents
{
    // â”€â”€ Common events that have not evolved yet â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>
    /// A single time-entry detail carried inside <see cref="TimesheetApprovedIntegrationEvent"/>.
    /// Both the Payroll consumer (total hours) and the PSA consumer (billable entries only)
    /// work from this same event â€” no cross-context DB reads needed.
    /// </summary>
    /// <param name="EntryId">Stable identifier matching <c>TimeEntry.EntryId</c> in the TimeAttendance context.</param>
    /// <param name="Date">Calendar date of the work (employee's local date).</param>
    /// <param name="Hours">Duration in decimal hours.</param>
    /// <param name="ProjectId">Project the time is logged against. Null = internal/overhead.</param>
    /// <param name="TaskId">Task within the project. Requires ProjectId.</param>
    /// <param name="IsBillable">Whether these hours are billable to the client.</param>
    /// <param name="CostCenter">Cost center for financial allocation (e.g. "ENG-PLATFORM").</param>
    /// <param name="Tag">Custom tag for granular reporting (e.g. "R&amp;D", "Maintenance").</param>
    /// <param name="Description">Brief description of work performed.</param>
    public record ApprovedTimeEntryDto(
        Guid EntryId,
        DateOnly Date,
        decimal Hours,
        Guid? ProjectId,
        Guid? TaskId,
        bool IsBillable,
        string? CostCenter,
        string? Tag,
        string? Description);

    /// <summary>
    /// Published when a timesheet transitions to Approved status.
    /// Consumed by Payroll (salary ledger), Revenue Ledger, and Utilization Metrics.
    /// <para>
    /// <see cref="IsRetroactive"/> is true when this event corrects a previously approved
    /// timesheet. Consumers must reverse previously posted amounts before applying the
    /// new figures â€” typically by deleting existing rows for <see cref="TimesheetId"/>
    /// and re-inserting from the new <see cref="Entries"/>.
    /// </para>
    /// </summary>
    /// <param name="TimesheetId">Stable timesheet identifier â€” also the idempotency key.</param>
    /// <param name="EmployeeId">Employee who submitted the timesheet.</param>
    /// <param name="TenantId">Tenant this timesheet belongs to.</param>
    /// <param name="WeekStartDate">Monday of the work week (ISO 8601).</param>
    /// <param name="TotalHours">Sum of all entry hours (billable + non-billable).</param>
    /// <param name="BillableHours">Sum of billable entry hours only. Pre-computed for Payroll convenience.</param>
    /// <param name="IsRetroactive">True when this approval corrects a previously approved timesheet. Downstream systems must reverse prior postings before applying new figures.</param>
    /// <param name="Entries">Full per-day, per-project breakdown.</param>
    public record TimesheetApprovedIntegrationEvent(
        Guid TimesheetId,
        Guid EmployeeId,
        string TenantId,
        DateOnly WeekStartDate,
        decimal TotalHours,
        decimal BillableHours,
        bool IsRetroactive,
        IReadOnlyList<ApprovedTimeEntryDto> Entries);

    /// <summary>
    /// Published when a leave request is approved by HR.
    /// </summary>
    /// <param name="RequestId">The leave request identifier.</param>
    /// <param name="EmployeeId">The employee who requested leave.</param>
    /// <param name="StartDate">Inclusive start date of the leave.</param>
    /// <param name="EndDate">Inclusive end date of the leave.</param>
    /// <param name="TotalDays">Total calendar days of leave.</param>
    /// <param name="IsPaid">Whether the leave is paid.</param>
    public record LeaveRequestApprovedIntegrationEvent(
        Guid RequestId,
        Guid EmployeeId,
        DateOnly StartDate,
        DateOnly EndDate,
        double TotalDays,
        bool IsPaid);

    /// <summary>
    /// Published when a payroll run is locked and no further adjustments are accepted.
    /// </summary>
    /// <param name="RunId">The payroll run identifier.</param>
    /// <param name="TenantId">The tenant owning this payroll run.</param>
    /// <param name="PeriodName">Human-readable period name (e.g. "April 2026").</param>
    public record PayrollRunLockedIntegrationEvent(
        Guid RunId,
        string TenantId,
        string PeriodName);

    /// <summary>
    /// Published when a new tenant is provisioned and its schema is ready.
    /// </summary>
    /// <param name="TenantId">The tenant identifier.</param>
    /// <param name="CompanyName">The company display name.</param>
    /// <param name="AdminEmail">Email address of the primary administrator.</param>
    public record TenantProvisionedIntegrationEvent(
        string TenantId,
        string CompanyName,
        string AdminEmail);

    /// <summary>
    /// Command to start a new payroll run.
    /// </summary>
    public record StartPayrollRunCommand(
        Guid RunId,
        string TenantId,
        string PeriodName);

    /// <summary>
    /// Command to lock a payroll run.
    /// </summary>
    public record LockPayrollRunCommand(
        Guid RunId,
        string ApprovedBy);

    /// <summary>
    /// Event published when a tax declaration is rejected by HR.
    /// Triggers a notification to the employee.
    /// </summary>
    /// <param name="DeclarationId">The IT declaration identifier.</param>
    /// <param name="EmployeeId">The employee whose declaration was rejected.</param>
    /// <param name="Category">The IT declaration category (e.g. "80C", "HRA").</param>
    /// <param name="Reason">Mandatory rejection reason shown to the employee.</param>
    public record ITDeclarationRejectedIntegrationEvent(
        Guid DeclarationId,
        Guid EmployeeId,
        string Category,
        string Reason);

    // â”€â”€ Standardized Workflow Events (Phase 1B.3 Convergence) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>Published when any platform workflow instance is started.</summary>
    public record WorkflowStartedIntegrationEvent(
        Guid WorkflowId,
        string TenantId,
        string EntityType,
        Guid EntityId,
        DateTimeOffset StartedAt,
        string InitiatedBy);

    /// <summary>Published when a workflow state transition occurs.</summary>
    public record WorkflowTransitionedIntegrationEvent(
        Guid WorkflowId,
        string TenantId,
        string FromStatus,
        string ToStatus,
        Guid ActorId,
        DateTimeOffset TransitionedAt,
        string? Reason = null);

    /// <summary>Published when an approval decision is recorded.</summary>
    public record WorkflowApprovalDecisionIntegrationEvent(
        Guid WorkflowId,
        string TenantId,
        Guid StepId,
        Guid ApproverId,
        bool Approved,
        string? Note = null,
        DateTimeOffset DecidedAt = default);

    /// <summary>Published when a workflow is completed (Approved/Rejected/Cancelled).</summary>
    public record WorkflowCompletedIntegrationEvent(
        Guid WorkflowId,
        string TenantId,
        string EntityType,
        Guid EntityId,
        string FinalStatus,
        DateTimeOffset CompletedAt);
}

namespace Karamchari.Core.Contracts.IntegrationEvents.V1
{
    // V1 Contracts â€” original baseline.
    // Pin consumers to V1 if they don't yet need the V2 additions.

    /// <summary>Published when a new employee completes onboarding.</summary>
    /// <param name="EmployeeId">The employee identifier.</param>
    /// <param name="TenantId">The tenant the employee belongs to.</param>
    /// <param name="EmployeeNumber">The employee number assigned by HR.</param>
    /// <param name="LegalName">The employee's legal full name.</param>
    /// <param name="WorkEmail">The employee's work email address. Null until provisioned.</param>
    /// <param name="HiredOn">The date the employee was hired.</param>
    public record EmployeeOnboardedIntegrationEvent(
        Guid EmployeeId,
        string TenantId,
        string EmployeeNumber,
        string LegalName,
        string? WorkEmail,
        DateOnly HiredOn);

    /// <summary>
    /// V1: does NOT include EmployeeName â€” consumers must look it up separately.
    /// Prefer <see cref="V2.PayrollRunCompletedIntegrationEvent"/> for new consumers.
    /// </summary>
    /// <param name="EmployeeId">The employee identifier.</param>
    /// <param name="TenantId">The tenant owning this payroll run.</param>
    /// <param name="PeriodName">Human-readable period name (e.g. "April 2026").</param>
    /// <param name="Gross">Gross pay before deductions.</param>
    /// <param name="NetPay">Net pay after all deductions.</param>
    /// <param name="Earnings">Itemised earnings by component name.</param>
    /// <param name="Deductions">Itemised deductions by component name.</param>
    /// <param name="TaxRegime">Tax regime applied (e.g. "OldRegime", "NewRegime").</param>
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
    // V2 Contracts â€” evolved versions.

    /// <summary>
    /// Evolved version of PayrollRunCompleted that includes <see cref="EmployeeName"/>
    /// to avoid cross-context lookups during payslip generation.
    /// </summary>
    /// <param name="EmployeeId">The employee identifier.</param>
    /// <param name="EmployeeName">The employee's legal full name at time of payroll processing.</param>
    /// <param name="TenantId">The tenant owning this payroll run.</param>
    /// <param name="PeriodName">Human-readable period name (e.g. "April 2026").</param>
    /// <param name="Gross">Gross pay before deductions.</param>
    /// <param name="NetPay">Net pay after all deductions.</param>
    /// <param name="Earnings">Itemised earnings by component name.</param>
    /// <param name="Deductions">Itemised deductions by component name.</param>
    /// <param name="TaxRegime">Tax regime applied (e.g. "OldRegime", "NewRegime").</param>
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
