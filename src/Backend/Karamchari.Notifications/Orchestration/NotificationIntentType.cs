namespace Karamchari.Notifications.Orchestration;

/// <summary>
/// Stable machine-readable identifier for each distinct notification scenario.
/// Maps 1:1 to a NotificationTemplate.Code in the database.
/// Convention: "{Domain}.{Action}" in lower-kebab-case when serialized.
/// </summary>
public enum NotificationIntentType
{
    // ── Review Workflow ────────────────────────────────────────────────────────

    /// <summary>Reviewer receives a new review assignment.</summary>
    ReviewAssigned = 100,

    /// <summary>Review deadline is approaching (3-day reminder).</summary>
    ReviewDueSoon = 101,

    /// <summary>Review assignment is past its deadline.</summary>
    ReviewOverdue = 102,

    /// <summary>Manager / HR notified when a review is submitted.</summary>
    ReviewSubmitted = 103,

    /// <summary>Review cycle is fully completed (HR summary notification).</summary>
    ReviewCycleCompleted = 104,

    // ── Calibration ────────────────────────────────────────────────────────────

    /// <summary>Panel member invited to a calibration session.</summary>
    CalibrationSessionScheduled = 200,

    /// <summary>Calibration session has been finalized and locked.</summary>
    CalibrationFinalized = 201,

    // ── Promotions ─────────────────────────────────────────────────────────────

    /// <summary>Employee / manager notified that a promotion was approved.</summary>
    PromotionApproved = 300,

    /// <summary>HR notified that a promotion recommendation requires review.</summary>
    PromotionPendingHRReview = 301,

    // ── Goals ──────────────────────────────────────────────────────────────────

    /// <summary>Goal cycle activated; employees prompted to set / review goals.</summary>
    GoalCycleStarted = 400,

    /// <summary>Manager notified that a direct report's goal requires approval.</summary>
    GoalApprovalRequired = 401,

    /// <summary>Owner notified that their goal is approaching its due date.</summary>
    GoalDueSoon = 402,

    /// <summary>Owner / manager notified that a goal is overdue.</summary>
    GoalOverdue = 403,

    // ── Feedback ───────────────────────────────────────────────────────────────

    /// <summary>Employee asked to provide feedback by a peer or manager.</summary>
    FeedbackRequested = 500,

    /// <summary>Feedback provider receives a reminder that their response is pending.</summary>
    FeedbackResponsePendingReminder = 501,
}
