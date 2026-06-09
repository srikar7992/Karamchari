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

    /// <summary>Published when a new leave request is created and awaiting coordination.</summary>
    public record LeaveRequestCreatedIntegrationEvent(
        Guid RequestId,
        string TenantId,
        Guid EmployeeId,
        DateOnly StartDate,
        DateOnly EndDate,
        double TotalDays);

    /// <summary>Published when a new reimbursement claim is submitted and awaiting coordination.</summary>
    public record ReimbursementSubmittedIntegrationEvent(
        Guid ClaimId,
        string TenantId,
        Guid EmployeeId,
        string Category,
        decimal Amount);

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

    /// <summary>Synthetic event used for platform-wide execution context propagation testing.</summary>
    public record SyntheticPingIntegrationEvent(
        Guid PingId,
        string TenantId,
        string Payload);

    // ── Phase 5: Workforce Planning Events ──────────────────────────────────

    /// <summary>
    /// Published when an offer is accepted and a future start date is known.
    /// Consumed by Forecasting to track future workforce supply before the employee onboards.
    /// </summary>
    /// <param name="OfferId">Stable offer identifier. Used as idempotency key by the WFP consumer.</param>
    /// <param name="TenantId">Tenant owning this hire.</param>
    /// <param name="LocationCode">Work location the candidate will join.</param>
    /// <param name="SkillCode">Primary skill associated with this offer.</param>
    /// <param name="ExpectedStartDate">Date on which the candidate is expected to begin work.</param>
    /// <param name="JoiningProbability">
    /// Probability the accepted offer converts to an actual join (0.0–1.0). Default 1.0.
    /// Set lower for graduate/external hires with higher drop-off rates (e.g. 0.75).
    /// WFP supply contribution = SUM(JoiningProbability) rounded to nearest integer.
    /// </param>
    public record HireOfferAcceptedIntegrationEvent(
        Guid OfferId,
        string TenantId,
        string LocationCode,
        string SkillCode,
        DateOnly ExpectedStartDate,
        decimal JoiningProbability = 1.0m);

    /// <summary>Published when an approved leave request is cancelled before it starts.</summary>
    public record LeaveCancelledIntegrationEvent(
        Guid RequestId,
        string TenantId,
        Guid EmployeeId,
        DateOnly StartDate,
        DateOnly EndDate);

    /// <summary>Published when an employee is terminated or their contract ends.</summary>
    public record EmployeeTerminatedIntegrationEvent(
        Guid EmployeeId,
        string TenantId,
        string EmployeeNumber,
        DateOnly TerminatedOn,
        string TerminationReason);

    /// <summary>Published when a skill is assigned or renewed for an employee.</summary>
    public record SkillAssignedIntegrationEvent(
        Guid EmployeeId,
        string TenantId,
        Guid SkillId,
        string SkillCode,
        string SkillName,
        int Level,
        DateOnly? ExpiryDate);

    /// <summary>Published when a skill certification expires for an employee.</summary>
    public record SkillExpiredIntegrationEvent(
        Guid EmployeeId,
        string TenantId,
        Guid SkillId,
        string SkillCode);

    /// <summary>Published when an employee's skill is validated.</summary>
    /// <param name="EmployeeId">The unique identifier of the employee whose skill was validated.</param>
    /// <param name="TenantId">The tenant identifier associated with the validation.</param>
    /// <param name="SkillId">The unique identifier of the validated skill.</param>
    /// <param name="SkillCode">The code representing the skill taxonomy category.</param>
    /// <param name="SkillName">The display name of the skill.</param>
    /// <param name="Level">The validated proficiency level score achieved.</param>
    /// <param name="ValidatedAt">The timestamp when this validation was recorded.</param>
    public record SkillValidatedIntegrationEvent(
        Guid EmployeeId,
        string TenantId,
        Guid SkillId,
        string SkillCode,
        string SkillName,
        int Level,
        DateTimeOffset ValidatedAt);

    // ── Phase 25: Internal Mobility Marketplace ──────────────────────────────

    /// <summary>
    /// Published when a position vacancy is opened and ready for internal candidate matching.
    /// Consumed by the Capability module to generate InternalMobilityProjection rows.
    /// </summary>
    /// <param name="VacancyId">Stable vacancy identifier — idempotency key for mobility consumers.</param>
    /// <param name="TenantId">Tenant owning this vacancy.</param>
    /// <param name="PositionId">The position that has become vacant.</param>
    /// <param name="RoleSkillRequirementId">
    /// The role skill requirement profile linked to this position.
    /// Null when the position has no linked requirement — consumers skip mobility generation.
    /// </param>
    /// <param name="OpenedUtc">Timestamp when the vacancy was opened.</param>
    public record VacancyOpenedIntegrationEvent(
        Guid VacancyId,
        string TenantId,
        Guid PositionId,
        Guid? RoleSkillRequirementId,
        DateTimeOffset OpenedUtc);

    /// <summary>
    /// Published when a position vacancy is closed (filled or cancelled).
    /// Consumed by the Capability module to delete InternalMobilityProjection rows.
    /// </summary>
    /// <param name="VacancyId">Stable vacancy identifier.</param>
    /// <param name="TenantId">Tenant owning this vacancy.</param>
    /// <param name="PositionId">The position whose vacancy was closed.</param>
    /// <param name="CloseReason">How it was closed: "Filled" or "Cancelled".</param>
    public record VacancyClosedIntegrationEvent(
        Guid VacancyId,
        string TenantId,
        Guid PositionId,
        string CloseReason);
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
    /// <param name="DateOfBirth">Optional date of birth. Used by WFP to derive retirement forecast dates.</param>
    /// <param name="ContractEndDate">Optional fixed-term contract end date. Used by WFP to forecast contract expiry deductions.</param>
    public record EmployeeOnboardedIntegrationEvent(
        Guid EmployeeId,
        string TenantId,
        string EmployeeNumber,
        string LegalName,
        string? WorkEmail,
        DateOnly HiredOn,
        DateOnly? DateOfBirth = null,
        DateOnly? ContractEndDate = null);

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

namespace Karamchari.Core.Contracts.IntegrationEvents.Workforce
{
    // ── Phase 6.1: Shift swap and overtime event contracts ───────────────────
    // These fill the real-time signal gaps identified in Phase 6.
    // TenantId is required on all events to support multi-tenant RLS.

    /// <summary>Published when a requested shift swap is approved by a manager.</summary>
    public record ShiftSwapApprovedIntegrationEvent(
        Guid SwapId,
        Guid RequestingEmployeeId,
        Guid TargetEmployeeId,
        string TenantId,
        DateOnly SwapDate,
        Guid RequestingShiftId,
        Guid TargetShiftId,
        DateTime ApprovedAt);

    /// <summary>Published when a requested shift swap is rejected by a manager.</summary>
    public record ShiftSwapRejectedIntegrationEvent(
        Guid SwapId,
        Guid RequestingEmployeeId,
        string TenantId,
        DateOnly SwapDate,
        string RejectionReason,
        DateTime RejectedAt);

    /// <summary>Published when an employee accepts an overtime offer.</summary>
    public record OvertimeAcceptedIntegrationEvent(
        Guid OvertimeOfferId,
        Guid EmployeeId,
        string TenantId,
        DateOnly WorkDate,
        decimal OfferedHours,
        DateTime AcceptedAt);

    /// <summary>
    /// Published when an employee declines an overtime offer.
    /// Consumed by Intelligence module to update OvertimeRejections30d signal.
    /// </summary>
    public record OvertimeRejectedIntegrationEvent(
        Guid OvertimeOfferId,
        Guid EmployeeId,
        string TenantId,
        DateOnly WorkDate,
        decimal OfferedHours,
        string? RejectionReason,
        DateTime RejectedAt);
}
