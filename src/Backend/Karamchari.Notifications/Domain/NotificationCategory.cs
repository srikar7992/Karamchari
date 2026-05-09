namespace Karamchari.Notifications.Domain;

/// <summary>
/// Category of notification. Used for rate-limiting, user preference filtering,
/// and digest grouping.
/// </summary>
public enum NotificationCategory
{
    /// <summary>Review deadlines, overdue reminders, submission confirmations.</summary>
    ReviewWorkflow = 1,

    /// <summary>Goal approvals, progress updates, overdue goal alerts.</summary>
    GoalManagement = 2,

    /// <summary>Feedback requests, submission confirmations, anonymity threshold reached.</summary>
    FeedbackRequests = 3,

    /// <summary>Calibration panel invitations, session actions, finalization.</summary>
    CalibrationActions = 4,

    /// <summary>Promotion nominations, approval stage transitions, final decisions.</summary>
    PromotionApprovals = 5,

    /// <summary>KPI anomalies, threshold breaches, off-track alerts.</summary>
    KPIAlerts = 6,

    /// <summary>Growth plan task reminders, development action due dates.</summary>
    GrowthPlanReminders = 7,

    /// <summary>SLA breaches and escalation notifications to managers or HR.</summary>
    SLAEscalations = 8,

    /// <summary>System-level notifications (maintenance, provisioning, billing — admin only).</summary>
    SystemAlerts = 9,

    /// <summary>Payroll-related notifications (salary, FnF, arrears, reimbursements).</summary>
    Payroll = 10,
}
