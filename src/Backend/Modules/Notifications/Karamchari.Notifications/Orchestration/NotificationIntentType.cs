namespace Karamchari.Notifications.Orchestration;

/// <summary>
/// Stable machine-readable identifier for each distinct notification scenario.
/// Maps 1:1 to a NotificationTemplate.Code in the database.
/// Convention: "{Domain}.{Action}" in lower-kebab-case when serialized.
/// </summary>
public enum NotificationIntentType
{
    // â”€â”€ Review Workflow â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

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

    // â”€â”€ Calibration â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>Panel member invited to a calibration session.</summary>
    CalibrationSessionScheduled = 200,

    /// <summary>Calibration session has been finalized and locked.</summary>
    CalibrationFinalized = 201,

    // â”€â”€ Promotions â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>Employee / manager notified that a promotion was approved.</summary>
    PromotionApproved = 300,

    /// <summary>HR notified that a promotion recommendation requires review.</summary>
    PromotionPendingHRReview = 301,

    // â”€â”€ Goals â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>Goal cycle activated; employees prompted to set / review goals.</summary>
    GoalCycleStarted = 400,

    /// <summary>Manager notified that a direct report's goal requires approval.</summary>
    GoalApprovalRequired = 401,

    /// <summary>Owner notified that their goal is approaching its due date.</summary>
    GoalDueSoon = 402,

    /// <summary>Owner / manager notified that a goal is overdue.</summary>
    GoalOverdue = 403,

    // â”€â”€ Feedback â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>Employee asked to provide feedback by a peer or manager.</summary>
    FeedbackRequested = 500,

    /// <summary>Feedback provider receives a reminder that their response is pending.</summary>
    FeedbackResponsePendingReminder = 501,

    // â”€â”€ Payroll â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>HR/approver notified that payroll run requires approval.</summary>
    PayrollApprovalRequired = 600,

    /// <summary>Employee notified that salary disbursement is complete.</summary>
    DisbursementCompleted = 601,

    /// <summary>HR alerted to a failed disbursement batch.</summary>
    DisbursementFailed = 602,

    /// <summary>Employee notified FnF settlement has been approved.</summary>
    FnFApproved = 610,

    /// <summary>Employee notified FnF amount has been disbursed.</summary>
    FnFDisbursed = 611,

    /// <summary>Employee notified that their arrear adjustment was approved.</summary>
    ArrearApproved = 620,

    /// <summary>Employee notified that their reimbursement claim was approved.</summary>
    ReimbursementApproved = 630,

    /// <summary>Employee notified of a salary revision approval.</summary>
    SalaryRevisionApproved = 640,

    /// <summary>HR alerted to a payroll reconciliation anomaly.</summary>
    PayrollAnomalyDetected = 650,

    /// <summary>HR alerted to a compliance issue.</summary>
    ComplianceIssueDetected = 660,
}
