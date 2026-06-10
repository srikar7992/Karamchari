// -----------------------------------------------------------------------
// <copyright file="KaramchariEventCatalog.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

// Karamchari Platform — Integration Event Catalog
// Registry of all integration events across all bounded contexts.
// Answers three governance questions within 30 seconds:
//   - What events exist?
//   - Who publishes each event?
//   - Who consumes each event?
//   - Which version is active?
//
// Update this file when adding, versioning, or deprecating any integration event.
// Consumers that need to pin to a specific version: reference the FullTypeName directly.

namespace Karamchari.Core.Contracts.EventCatalog;

/// <summary>Lifecycle status of an integration event.</summary>
public enum EventStatus
{
    /// <summary>Event is active and may be published by its owning module.</summary>
    Active,

    /// <summary>
    /// Event is deprecated. New consumers must not subscribe to this version.
    /// Existing consumers should migrate to <see cref="EventCatalogEntry.DeprecatedInFavorOf"/>.
    /// </summary>
    Deprecated,
}

/// <summary>
/// Registry entry describing a single integration event version.
/// </summary>
/// <param name="EventName">Short class name (e.g. "EmployeeTerminatedIntegrationEvent").</param>
/// <param name="FullTypeName">Fully qualified type name including namespace. Used as MassTransit message type key.</param>
/// <param name="Version">Schema version. Monotonically increasing per event family.</param>
/// <param name="Publisher">Bounded context that owns and publishes this event.</param>
/// <param name="Consumers">Bounded contexts known to consume this event.</param>
/// <param name="Status">Active or Deprecated.</param>
/// <param name="Description">One-line description of what this event represents.</param>
/// <param name="AddedInPhase">Platform phase in which this event was introduced.</param>
/// <param name="DeprecatedInFavorOf">For deprecated events: the EventName of the replacement event.</param>
public sealed record EventCatalogEntry(
    string EventName,
    string FullTypeName,
    int Version,
    string Publisher,
    string[] Consumers,
    EventStatus Status,
    string Description,
    int AddedInPhase,
    string? DeprecatedInFavorOf = null);

/// <summary>
/// Static registry of all Karamchari integration events.
/// Queryable via <c>GET /api/v1/catalog/events</c>.
/// </summary>
public static class KaramchariEventCatalog
{
    private static readonly EventCatalogEntry[] _entries =
    [
        // ── Core: Employee lifecycle ───────────────────────────────────────────

        new(
            "EmployeeOnboardedIntegrationEvent",
            "Karamchari.Core.Contracts.IntegrationEvents.V1.EmployeeOnboardedIntegrationEvent",
            Version: 1,
            Publisher: "HR",
            Consumers: ["Payroll", "Performance", "TimeAttendance"],
            Status: EventStatus.Active,
            Description: "Published when a new employee completes onboarding.",
            AddedInPhase: 1),

        new(
            "EmployeeTerminatedIntegrationEvent",
            "Karamchari.Core.Contracts.IntegrationEvents.EmployeeTerminatedIntegrationEvent",
            Version: 1,
            Publisher: "HR",
            Consumers: ["AssetManagement", "Helpdesk", "Forecasting"],
            Status: EventStatus.Active,
            Description: "Published when an employee is terminated or their contract ends.",
            AddedInPhase: 5),

        // ── Core: Leave ────────────────────────────────────────────────────────

        new(
            "LeaveRequestCreatedIntegrationEvent",
            "Karamchari.Core.Contracts.IntegrationEvents.LeaveRequestCreatedIntegrationEvent",
            Version: 1,
            Publisher: "TimeAttendance",
            Consumers: ["Workflow", "Notifications"],
            Status: EventStatus.Active,
            Description: "Published when a new leave request is created and awaiting coordination.",
            AddedInPhase: 2),

        new(
            "LeaveRequestApprovedIntegrationEvent",
            "Karamchari.Core.Contracts.IntegrationEvents.LeaveRequestApprovedIntegrationEvent",
            Version: 1,
            Publisher: "TimeAttendance",
            Consumers: ["Payroll", "Forecasting"],
            Status: EventStatus.Active,
            Description: "Published when a leave request is approved by HR.",
            AddedInPhase: 2),

        new(
            "LeaveCancelledIntegrationEvent",
            "Karamchari.Core.Contracts.IntegrationEvents.LeaveCancelledIntegrationEvent",
            Version: 1,
            Publisher: "TimeAttendance",
            Consumers: ["Forecasting"],
            Status: EventStatus.Active,
            Description: "Published when an approved leave request is cancelled before it starts.",
            AddedInPhase: 5),

        // ── Core: Payroll ──────────────────────────────────────────────────────

        new(
            "PayrollRunCompletedIntegrationEvent",
            "Karamchari.Core.Contracts.IntegrationEvents.V1.PayrollRunCompletedIntegrationEvent",
            Version: 1,
            Publisher: "Payroll",
            Consumers: ["Notifications"],
            Status: EventStatus.Deprecated,
            Description: "V1: does not include EmployeeName — consumers must look it up separately.",
            AddedInPhase: 1,
            DeprecatedInFavorOf: "PayrollRunCompletedIntegrationEvent (V2)"),

        new(
            "PayrollRunCompletedIntegrationEvent",
            "Karamchari.Core.Contracts.IntegrationEvents.V2.PayrollRunCompletedIntegrationEvent",
            Version: 2,
            Publisher: "Payroll",
            Consumers: ["Notifications", "FinancialOps"],
            Status: EventStatus.Active,
            Description: "V2: includes EmployeeName to avoid cross-context lookups during payslip generation.",
            AddedInPhase: 2),

        new(
            "PayrollRunLockedIntegrationEvent",
            "Karamchari.Core.Contracts.IntegrationEvents.PayrollRunLockedIntegrationEvent",
            Version: 1,
            Publisher: "Payroll",
            Consumers: ["Compliance", "FinancialOps"],
            Status: EventStatus.Active,
            Description: "Published when a payroll run is locked and no further adjustments are accepted.",
            AddedInPhase: 2),

        new(
            "ReimbursementSubmittedIntegrationEvent",
            "Karamchari.Core.Contracts.IntegrationEvents.ReimbursementSubmittedIntegrationEvent",
            Version: 1,
            Publisher: "Payroll",
            Consumers: ["Workflow", "Notifications"],
            Status: EventStatus.Active,
            Description: "Published when a new reimbursement claim is submitted and awaiting coordination.",
            AddedInPhase: 3),

        new(
            "ITDeclarationRejectedIntegrationEvent",
            "Karamchari.Core.Contracts.IntegrationEvents.ITDeclarationRejectedIntegrationEvent",
            Version: 1,
            Publisher: "Payroll",
            Consumers: ["Notifications"],
            Status: EventStatus.Active,
            Description: "Published when a tax declaration is rejected by HR.",
            AddedInPhase: 3),

        // ── Core: Timesheet ────────────────────────────────────────────────────

        new(
            "TimesheetApprovedIntegrationEvent",
            "Karamchari.Core.Contracts.IntegrationEvents.TimesheetApprovedIntegrationEvent",
            Version: 1,
            Publisher: "TimeAttendance",
            Consumers: ["Payroll", "PSA", "Billing", "TimeAttendance"],
            Status: EventStatus.Active,
            Description: "Published when a timesheet transitions to Approved status.",
            AddedInPhase: 1),

        // ── Core: Tenant ────────────────────────────────────────────────────────

        new(
            "TenantProvisionedIntegrationEvent",
            "Karamchari.Core.Contracts.IntegrationEvents.TenantProvisionedIntegrationEvent",
            Version: 1,
            Publisher: "Identity",
            Consumers: ["HR", "Payroll", "TimeAttendance"],
            Status: EventStatus.Active,
            Description: "Published when a new tenant is provisioned and its schema is ready.",
            AddedInPhase: 1),

        // ── Core: Skills / Workforce Planning ─────────────────────────────────

        new(
            "HireOfferAcceptedIntegrationEvent",
            "Karamchari.Core.Contracts.IntegrationEvents.HireOfferAcceptedIntegrationEvent",
            Version: 1,
            Publisher: "Recruitment",
            Consumers: ["Forecasting"],
            Status: EventStatus.Active,
            Description: "Published when an offer is accepted and a future start date is known.",
            AddedInPhase: 5),

        new(
            "SkillAssignedIntegrationEvent",
            "Karamchari.Core.Contracts.IntegrationEvents.SkillAssignedIntegrationEvent",
            Version: 1,
            Publisher: "Capability",
            Consumers: ["Forecasting", "Intelligence"],
            Status: EventStatus.Active,
            Description: "Published when a skill is assigned or renewed for an employee.",
            AddedInPhase: 5),

        new(
            "SkillExpiredIntegrationEvent",
            "Karamchari.Core.Contracts.IntegrationEvents.SkillExpiredIntegrationEvent",
            Version: 1,
            Publisher: "Capability",
            Consumers: ["Notifications", "Intelligence"],
            Status: EventStatus.Active,
            Description: "Published when a skill certification expires for an employee.",
            AddedInPhase: 5),

        // ── Core: Workflow ─────────────────────────────────────────────────────

        new(
            "WorkflowStartedIntegrationEvent",
            "Karamchari.Core.Contracts.IntegrationEvents.WorkflowStartedIntegrationEvent",
            Version: 1,
            Publisher: "Workflow",
            Consumers: ["Notifications", "Compliance"],
            Status: EventStatus.Active,
            Description: "Published when any platform workflow instance is started.",
            AddedInPhase: 12),

        new(
            "WorkflowTransitionedIntegrationEvent",
            "Karamchari.Core.Contracts.IntegrationEvents.WorkflowTransitionedIntegrationEvent",
            Version: 1,
            Publisher: "Workflow",
            Consumers: ["Notifications", "Audit"],
            Status: EventStatus.Active,
            Description: "Published when a workflow state transition occurs.",
            AddedInPhase: 12),

        new(
            "WorkflowApprovalDecisionIntegrationEvent",
            "Karamchari.Core.Contracts.IntegrationEvents.WorkflowApprovalDecisionIntegrationEvent",
            Version: 1,
            Publisher: "Workflow",
            Consumers: ["Notifications", "Compliance"],
            Status: EventStatus.Active,
            Description: "Published when an approval decision is recorded.",
            AddedInPhase: 12),

        new(
            "WorkflowCompletedIntegrationEvent",
            "Karamchari.Core.Contracts.IntegrationEvents.WorkflowCompletedIntegrationEvent",
            Version: 1,
            Publisher: "Workflow",
            Consumers: ["Payroll", "TimeAttendance", "Compliance", "Audit"],
            Status: EventStatus.Active,
            Description: "Published when a workflow is completed (Approved/Rejected/Cancelled).",
            AddedInPhase: 12),

        // ── Workforce: Shift & Overtime ────────────────────────────────────────

        new(
            "ShiftSwapApprovedIntegrationEvent",
            "Karamchari.Core.Contracts.IntegrationEvents.Workforce.ShiftSwapApprovedIntegrationEvent",
            Version: 1,
            Publisher: "TimeAttendance",
            Consumers: ["Notifications"],
            Status: EventStatus.Active,
            Description: "Published when a requested shift swap is approved by a manager.",
            AddedInPhase: 6),

        new(
            "ShiftSwapRejectedIntegrationEvent",
            "Karamchari.Core.Contracts.IntegrationEvents.Workforce.ShiftSwapRejectedIntegrationEvent",
            Version: 1,
            Publisher: "TimeAttendance",
            Consumers: ["Notifications"],
            Status: EventStatus.Active,
            Description: "Published when a requested shift swap is rejected by a manager.",
            AddedInPhase: 6),

        new(
            "OvertimeAcceptedIntegrationEvent",
            "Karamchari.Core.Contracts.IntegrationEvents.Workforce.OvertimeAcceptedIntegrationEvent",
            Version: 1,
            Publisher: "TimeAttendance",
            Consumers: ["Intelligence"],
            Status: EventStatus.Active,
            Description: "Published when an employee accepts an overtime offer.",
            AddedInPhase: 6),

        new(
            "OvertimeRejectedIntegrationEvent",
            "Karamchari.Core.Contracts.IntegrationEvents.Workforce.OvertimeRejectedIntegrationEvent",
            Version: 1,
            Publisher: "TimeAttendance",
            Consumers: ["Intelligence"],
            Status: EventStatus.Active,
            Description: "Published when an employee declines an overtime offer. Updates OvertimeRejections30d burnout signal.",
            AddedInPhase: 6),

        // ── Billing ────────────────────────────────────────────────────────────

        new(
            "InvoiceIssuedIntegrationEvent",
            "Karamchari.Billing.Contracts.InvoiceIssuedIntegrationEvent",
            Version: 1,
            Publisher: "Billing",
            Consumers: ["FinancialOps"],
            Status: EventStatus.Active,
            Description: "Published when an invoice is issued to a client.",
            AddedInPhase: 9),

        new(
            "PaymentReceivedIntegrationEvent",
            "Karamchari.Billing.Contracts.PaymentReceivedIntegrationEvent",
            Version: 1,
            Publisher: "Billing",
            Consumers: ["FinancialOps", "PSA"],
            Status: EventStatus.Active,
            Description: "Published when a client payment is received against an invoice.",
            AddedInPhase: 9),

        // ── Capability ─────────────────────────────────────────────────────────

        new(
            "SkillAchievedPayload",
            "Karamchari.Capability.Contracts.SkillAchievedPayload",
            Version: 1,
            Publisher: "Capability",
            Consumers: ["Intelligence", "Succession"],
            Status: EventStatus.Active,
            Description: "Published when an employee achieves or renews a skill at a new level.",
            AddedInPhase: 8),

        new(
            "LearningCompletedPayload",
            "Karamchari.Capability.Contracts.LearningCompletedPayload",
            Version: 1,
            Publisher: "Capability",
            Consumers: ["Intelligence"],
            Status: EventStatus.Active,
            Description: "Published when an employee completes a learning module enrollment.",
            AddedInPhase: 8),

        new(
            "CertificationExpiredPayload",
            "Karamchari.Capability.Contracts.CertificationExpiredPayload",
            Version: 1,
            Publisher: "Capability",
            Consumers: ["Notifications", "Compliance"],
            Status: EventStatus.Active,
            Description: "Published when an employee's certification expires.",
            AddedInPhase: 8),

        new(
            "GrowthPlanActivatedPayload",
            "Karamchari.Capability.Contracts.GrowthPlanActivatedPayload",
            Version: 1,
            Publisher: "Capability",
            Consumers: ["Intelligence"],
            Status: EventStatus.Active,
            Description: "Published when a growth plan is activated for an employee.",
            AddedInPhase: 8),

        // ── Compliance ────────────────────────────────────────────────────────

        new(
            "PolicyViolationDetectedEvent",
            "Karamchari.Compliance.Contracts.PolicyViolationDetectedEvent",
            Version: 1,
            Publisher: "Compliance",
            Consumers: ["Notifications", "Audit"],
            Status: EventStatus.Active,
            Description: "Published when a compliance policy violation is detected.",
            AddedInPhase: 7),

        new(
            "PolicyViolationResolvedEvent",
            "Karamchari.Compliance.Contracts.PolicyViolationResolvedEvent",
            Version: 1,
            Publisher: "Compliance",
            Consumers: ["Notifications", "Audit"],
            Status: EventStatus.Active,
            Description: "Published when a policy violation is resolved.",
            AddedInPhase: 7),

        new(
            "RetentionActionScheduledEvent",
            "Karamchari.Compliance.Contracts.RetentionActionScheduledEvent",
            Version: 1,
            Publisher: "Compliance",
            Consumers: ["Audit"],
            Status: EventStatus.Active,
            Description: "Published when a data retention action is scheduled.",
            AddedInPhase: 7),

        new(
            "RetentionActionExecutedEvent",
            "Karamchari.Compliance.Contracts.RetentionActionExecutedEvent",
            Version: 1,
            Publisher: "Compliance",
            Consumers: ["Audit"],
            Status: EventStatus.Active,
            Description: "Published when a scheduled retention action is executed.",
            AddedInPhase: 7),

        new(
            "LegalHoldCreatedEvent",
            "Karamchari.Compliance.Contracts.LegalHoldCreatedEvent",
            Version: 1,
            Publisher: "Compliance",
            Consumers: ["Audit", "Notifications"],
            Status: EventStatus.Active,
            Description: "Published when a legal hold is placed on tenant records.",
            AddedInPhase: 7),

        new(
            "LegalHoldReleasedEvent",
            "Karamchari.Compliance.Contracts.LegalHoldReleasedEvent",
            Version: 1,
            Publisher: "Compliance",
            Consumers: ["Audit", "Notifications"],
            Status: EventStatus.Active,
            Description: "Published when a legal hold is released.",
            AddedInPhase: 7),

        new(
            "ComplianceScoreChangedEvent",
            "Karamchari.Compliance.Contracts.ComplianceScoreChangedEvent",
            Version: 1,
            Publisher: "Compliance",
            Consumers: ["Executive", "PlatformIntelligence"],
            Status: EventStatus.Active,
            Description: "Published when a tenant's compliance score changes significantly.",
            AddedInPhase: 7),

        new(
            "AuditPackageGeneratedEvent",
            "Karamchari.Compliance.Contracts.AuditPackageGeneratedEvent",
            Version: 1,
            Publisher: "Compliance",
            Consumers: ["Notifications"],
            Status: EventStatus.Active,
            Description: "Published when an audit evidence package is generated.",
            AddedInPhase: 7),

        // ── Compensation ───────────────────────────────────────────────────────

        new(
            "EmployeeCompensationRevisedIntegrationEvent",
            "Karamchari.Compensation.Contracts.IntegrationEvents.EmployeeCompensationRevisedIntegrationEvent",
            Version: 1,
            Publisher: "Compensation",
            Consumers: ["Payroll", "HR", "Notifications"],
            Status: EventStatus.Active,
            Description: "Published when an employee's compensation is revised through any channel.",
            AddedInPhase: 10),

        // ── Performance ────────────────────────────────────────────────────────

        new(
            "ReviewCycleCompletedIntegrationEvent",
            "Karamchari.Performance.Contracts.IntegrationEvents.ReviewCycleCompletedIntegrationEvent",
            Version: 1,
            Publisher: "Performance",
            Consumers: ["Compensation", "HR"],
            Status: EventStatus.Active,
            Description: "Published when a review cycle is fully completed (calibration approved and locked).",
            AddedInPhase: 4),

        new(
            "EmployeeCalibrationFinalizedIntegrationEvent",
            "Karamchari.Performance.Contracts.IntegrationEvents.EmployeeCalibrationFinalizedIntegrationEvent",
            Version: 1,
            Publisher: "Performance",
            Consumers: ["Succession", "Analytics", "Intelligence"],
            Status: EventStatus.Active,
            Description: "Published per-employee after calibration session is approved and locked.",
            AddedInPhase: 4),

        new(
            "PromotionApprovedIntegrationEvent",
            "Karamchari.Performance.Contracts.IntegrationEvents.PromotionApprovedIntegrationEvent",
            Version: 1,
            Publisher: "Performance",
            Consumers: ["HR", "Payroll", "Notifications"],
            Status: EventStatus.Active,
            Description: "Published when a promotion is fully approved through all workflow stages.",
            AddedInPhase: 4),

        // ── Intelligence ───────────────────────────────────────────────────────

        new(
            "StaleIntelligenceAlertEvent",
            "Karamchari.Intelligence.Contracts.StaleIntelligenceAlertEvent",
            Version: 1,
            Publisher: "Intelligence",
            Consumers: ["Notifications", "Ops"],
            Status: EventStatus.Active,
            Description: "Published when a workforce intelligence signal has not been refreshed within its SLA.",
            AddedInPhase: 6),

        // ── Data Migration ─────────────────────────────────────────────────────

        new(
            "ImportJobQueued",
            "Karamchari.DataMigration.Contracts.Events.ImportJobQueued",
            Version: 1,
            Publisher: "DataMigration",
            Consumers: ["DataMigration"],
            Status: EventStatus.Active,
            Description: "Published when a bulk import job is queued for background processing.",
            AddedInPhase: 11),
    ];

    /// <summary>All registered integration events across all bounded contexts.</summary>
    public static IReadOnlyList<EventCatalogEntry> All => _entries;

    /// <summary>Active events only (excludes deprecated).</summary>
    public static IReadOnlyList<EventCatalogEntry> Active =>
        _entries.Where(e => e.Status == EventStatus.Active).ToList();

    /// <summary>Events published by the given bounded context (case-insensitive).</summary>
    public static IReadOnlyList<EventCatalogEntry> GetByPublisher(string publisher) =>
        _entries.Where(e => e.Publisher.Equals(publisher, StringComparison.OrdinalIgnoreCase)).ToList();

    /// <summary>Events consumed by the given bounded context (case-insensitive).</summary>
    public static IReadOnlyList<EventCatalogEntry> GetByConsumer(string consumer) =>
        _entries.Where(e => e.Consumers.Contains(consumer, StringComparer.OrdinalIgnoreCase)).ToList();

    /// <summary>
    /// Find entry by short event name and optional version.
    /// If version is null, returns the highest-version active entry.
    /// </summary>
    public static EventCatalogEntry? Find(string eventName, int? version = null)
    {
        var candidates = _entries
            .Where(e => e.EventName.Equals(eventName, StringComparison.OrdinalIgnoreCase));

        if (version.HasValue)
            return candidates.FirstOrDefault(e => e.Version == version.Value);

        return candidates
            .Where(e => e.Status == EventStatus.Active)
            .OrderByDescending(e => e.Version)
            .FirstOrDefault()
            ?? candidates.OrderByDescending(e => e.Version).FirstOrDefault();
    }

    /// <summary>Events added or changed in the given platform phase.</summary>
    public static IReadOnlyList<EventCatalogEntry> GetByPhase(int phase) =>
        _entries.Where(e => e.AddedInPhase == phase).ToList();
}
