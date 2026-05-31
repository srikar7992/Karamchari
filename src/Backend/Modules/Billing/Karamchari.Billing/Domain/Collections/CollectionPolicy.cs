namespace Karamchari.Billing.Domain.Collections;

/// <summary>
/// Defines the rules for automated reminders and escalations for a tenant.
/// </summary>
public sealed class CollectionPolicy
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; init; } = string.Empty;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int FirstReminderDays { get; set; } = 7;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int SecondReminderDays { get; set; } = 30;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int EscalationDays { get; set; } = 60;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int CriticalDays { get; set; } = 90;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool AutoEscalate { get; set; } = true;
    public string? EscalationEmailCC { get; set; } // Finance Head / Manager
}
