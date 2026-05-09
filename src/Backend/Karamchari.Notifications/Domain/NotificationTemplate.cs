using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Notifications.Domain;

/// <summary>
/// Tenant-brandable, localizable notification template.
/// Variables in Subject/Body/PreviewText are delimited by {{VarName}}.
/// Rendered at delivery time by substituting the payload dictionary.
///
/// Templates are owned per tenant. The platform ships default templates;
/// tenants override them via the HR workspace. An overridden template
/// is a copy with IsCustom=true â€” the original is never deleted.
/// </summary>
public sealed class NotificationTemplate : AggregateRoot<Guid>, ITenantOwned
{
    private NotificationTemplate() { /* EF materialization */ }

    private NotificationTemplate(
        Guid id,
        string tenantId,
        string code,
        NotificationCategory category,
        NotificationChannel channel,
        string locale,
        string subject,
        string bodyHtml,
        string bodyText,
        string? previewText,
        bool isCustom) : base(id)
    {
        TenantId = tenantId;
        Code = code;
        Category = category;
        Channel = channel;
        Locale = locale;
        Subject = subject;
        BodyHtml = bodyHtml;
        BodyText = bodyText;
        PreviewText = previewText;
        IsCustom = isCustom;
        IsActive = true;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Stable machine-readable code, e.g. "review.deadline.reminder".</summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public NotificationCategory Category { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public NotificationChannel Channel { get; private set; }

    /// <summary>BCP-47 locale code, e.g. "en-US", "hi-IN".</summary>
    public string Locale { get; private set; } = "en-US";

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Subject { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string BodyHtml { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string BodyText { get; private set; } = string.Empty;
    public string? PreviewText { get; private set; }

    /// <summary>True if this template was customized by the tenant (vs. platform default).</summary>
    public bool IsCustom { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool IsActive { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static NotificationTemplate CreateDefault(
        string tenantId,
        string code,
        NotificationCategory category,
        NotificationChannel channel,
        string locale,
        string subject,
        string bodyHtml,
        string bodyText,
        string? previewText = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(bodyHtml);
        ArgumentException.ThrowIfNullOrWhiteSpace(bodyText);

        return new NotificationTemplate(
            Guid.NewGuid(), tenantId, code.ToLowerInvariant(), category,
            channel, locale, subject, bodyHtml, bodyText, previewText, isCustom: false);
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Customize(string subject, string bodyHtml, string bodyText, string? previewText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(bodyHtml);
        ArgumentException.ThrowIfNullOrWhiteSpace(bodyText);

        Subject = subject;
        BodyHtml = bodyHtml;
        BodyText = bodyText;
        PreviewText = previewText;
        IsCustom = true;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Deactivate() => IsActive = false;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Activate() => IsActive = true;
}
