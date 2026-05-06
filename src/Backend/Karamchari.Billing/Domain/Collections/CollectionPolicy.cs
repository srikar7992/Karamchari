namespace Karamchari.Billing.Domain.Collections;

/// <summary>
/// Defines the rules for automated reminders and escalations for a tenant.
/// </summary>
public sealed class CollectionPolicy
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string TenantId { get; init; } = string.Empty;

    public int FirstReminderDays { get; set; } = 7;
    public int SecondReminderDays { get; set; } = 30;
    public int EscalationDays { get; set; } = 60;
    public int CriticalDays { get; set; } = 90;

    public bool AutoEscalate { get; set; } = true;
    public string? EscalationEmailCC { get; set; } // Finance Head / Manager
}
