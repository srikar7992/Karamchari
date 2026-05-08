using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Notifications.Domain;

/// <summary>
/// Per-user, per-category notification preferences.
/// Controls which channels receive which categories and when delivery is allowed.
/// Seeded with defaults when an employee is onboarded; editable via the profile UI.
/// </summary>
public sealed class UserNotificationPreference : AggregateRoot<Guid>, ITenantOwned
{
    private UserNotificationPreference() { /* EF materialization */ }

    private UserNotificationPreference(
        Guid id,
        string tenantId,
        Guid employeeId,
        NotificationCategory category,
        bool inAppEnabled,
        bool emailEnabled,
        bool pushEnabled,
        bool digestEnabled,
        DigestFrequency digestFrequency,
        string? quietHoursStart,
        string? quietHoursEnd,
        string timezone) : base(id)
    {
        TenantId = tenantId;
        EmployeeId = employeeId;
        Category = category;
        InAppEnabled = inAppEnabled;
        EmailEnabled = emailEnabled;
        PushEnabled = pushEnabled;
        DigestEnabled = digestEnabled;
        DigestFrequency = digestFrequency;
        QuietHoursStart = quietHoursStart;
        QuietHoursEnd = quietHoursEnd;
        Timezone = timezone;
    }

    public string TenantId { get; private set; } = string.Empty;
    public Guid EmployeeId { get; private set; }
    public NotificationCategory Category { get; private set; }
    public bool InAppEnabled { get; private set; }
    public bool EmailEnabled { get; private set; }
    public bool PushEnabled { get; private set; }
    public bool DigestEnabled { get; private set; }
    public DigestFrequency DigestFrequency { get; private set; }

    /// <summary>HH:mm format in local time, e.g. "20:00".</summary>
    public string? QuietHoursStart { get; private set; }

    /// <summary>HH:mm format in local time, e.g. "08:00".</summary>
    public string? QuietHoursEnd { get; private set; }

    /// <summary>IANA timezone identifier, e.g. "Asia/Kolkata".</summary>
    public string Timezone { get; private set; } = "UTC";

    public static UserNotificationPreference CreateDefault(
        string tenantId,
        Guid employeeId,
        NotificationCategory category,
        string timezone = "UTC")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(timezone);

        return new UserNotificationPreference(
            Guid.NewGuid(), tenantId, employeeId, category,
            inAppEnabled: true,
            emailEnabled: category != NotificationCategory.SystemAlerts,
            pushEnabled: false,
            digestEnabled: category is NotificationCategory.GoalManagement
                or NotificationCategory.GrowthPlanReminders,
            digestFrequency: DigestFrequency.Daily,
            quietHoursStart: "20:00",
            quietHoursEnd: "08:00",
            timezone: timezone);
    }

    public void Update(
        bool inApp, bool email, bool push,
        bool digest, DigestFrequency frequency,
        string? quietHoursStart, string? quietHoursEnd)
    {
        InAppEnabled = inApp;
        EmailEnabled = email;
        PushEnabled = push;
        DigestEnabled = digest;
        DigestFrequency = frequency;
        QuietHoursStart = quietHoursStart;
        QuietHoursEnd = quietHoursEnd;
    }
}

/// <summary>Frequency at which digests are batched for a user.</summary>
public enum DigestFrequency
{
    /// <summary>One digest per day at the user's preferred time.</summary>
    Daily = 1,

    /// <summary>One digest per week (Monday morning by default).</summary>
    Weekly = 2,
}
