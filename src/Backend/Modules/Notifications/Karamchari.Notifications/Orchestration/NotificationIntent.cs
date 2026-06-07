using Karamchari.Notifications.Domain;

namespace Karamchari.Notifications.Orchestration;

/// <summary>
/// Channel-agnostic description of a notification that should be delivered
/// to exactly one recipient. Created by event consumers; executed by
/// <see cref="INotificationOrchestrator"/>.
///
/// TriggerEventId is the integration event's correlation / EventId â€” used as
/// part of the idempotency key to block duplicate delivery on event replay.
/// </summary>
public sealed record NotificationIntent(
    string TenantId,
    Guid TriggerEventId,
    NotificationIntentType IntentType,
    NotificationCategory Category,
    Guid RecipientEmployeeId,
    string RecipientEmail,
    string Locale,
    IReadOnlyDictionary<string, string> Variables)
{
    /// <summary>
    /// Derived idempotency key.
    /// Format: {TenantId}:{TriggerEventId}:{IntentType}:{RecipientEmployeeId}
    /// </summary>
    public string IdempotencyKey =>
        $"{TenantId}:{TriggerEventId}:{IntentType}:{RecipientEmployeeId}";

    /// <summary>
    /// Template code used to look up <see cref="NotificationTemplate"/>.
    /// Derived from IntentType: ReviewAssigned â†’ "review.assigned".
    /// </summary>
    public string TemplateCode => IntentType switch
    {
        NotificationIntentType.ReviewAssigned => "review.assigned",
        NotificationIntentType.ReviewDueSoon => "review.due-soon",
        NotificationIntentType.ReviewOverdue => "review.overdue",
        NotificationIntentType.ReviewSubmitted => "review.submitted",
        NotificationIntentType.ReviewCycleCompleted => "review.cycle-completed",
        NotificationIntentType.CalibrationSessionScheduled => "calibration.session-scheduled",
        NotificationIntentType.CalibrationFinalized => "calibration.finalized",
        NotificationIntentType.PromotionApproved => "promotion.approved",
        NotificationIntentType.PromotionPendingHRReview => "promotion.pending-hr-review",
        NotificationIntentType.GoalCycleStarted => "goal.cycle-started",
        NotificationIntentType.GoalApprovalRequired => "goal.approval-required",
        NotificationIntentType.GoalDueSoon => "goal.due-soon",
        NotificationIntentType.GoalOverdue => "goal.overdue",
        NotificationIntentType.FeedbackRequested => "feedback.requested",
        NotificationIntentType.FeedbackResponsePendingReminder => "feedback.response-pending",
        NotificationIntentType.FnFApproved => "payroll.fnf-approved",
        NotificationIntentType.FnFDisbursed => "payroll.fnf-disbursed",
        NotificationIntentType.ArrearApproved => "payroll.arrear-approved",
        NotificationIntentType.ReimbursementApproved => "payroll.reimbursement-approved",
        NotificationIntentType.SalaryRevisionApproved => "payroll.salary-revision-approved",
        NotificationIntentType.AttendanceLateArrival => "attendance.late-arrival",
        NotificationIntentType.AttendanceNoShow => "attendance.no-show",
        NotificationIntentType.AttendanceMissingCheckout => "attendance.missing-checkout",
        NotificationIntentType.AttendanceRegularizationApproved => "attendance.regularization-approved",
        NotificationIntentType.AttendanceRegularizationRejected => "attendance.regularization-rejected",
        NotificationIntentType.AttendanceGeoFraudDetected => "attendance.geo-fraud-detected",
        NotificationIntentType.AttendanceConsecutiveAbsenceEscalated => "attendance.consecutive-absence-escalated",
        _ => throw new ArgumentOutOfRangeException(nameof(IntentType), IntentType, "No template code mapping.")
    };
}
