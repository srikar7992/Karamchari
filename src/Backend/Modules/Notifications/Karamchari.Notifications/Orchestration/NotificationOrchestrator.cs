using System.Globalization;
using Karamchari.Notifications.Channels;
using Karamchari.Notifications.Domain;
using Karamchari.Notifications.Persistence;
using Karamchari.Notifications.RealTime;
using Karamchari.Notifications.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.Notifications.Orchestration;

/// <summary>
/// Full notification governance pipeline.
/// See ADR-0012 for the step-by-step contract.
/// </summary>
public sealed class NotificationOrchestrator : INotificationOrchestrator
{
    private readonly NotificationsDbContext _db;
    private readonly ITemplateRenderer _renderer;
    private readonly IEnumerable<INotificationChannelAdapter> _adapters;
    private readonly INotificationPushService _push;
    private readonly ILogger<NotificationOrchestrator> _logger;

    public NotificationOrchestrator(
        NotificationsDbContext db,
        ITemplateRenderer renderer,
        IEnumerable<INotificationChannelAdapter> adapters,
        INotificationPushService push,
        ILogger<NotificationOrchestrator> logger)
    {
        _db = db;
        _renderer = renderer;
        _adapters = adapters;
        _push = push;
        _logger = logger;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public async Task OrchestrateAsync(
        NotificationIntent intent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(intent);

        // Step 1 â€” Dedupe: block replay of the same event+intent+recipient.
        var duplicate = await _db.NotificationMessages
            .AsNoTracking()
            .AnyAsync(m => m.IdempotencyKey == intent.IdempotencyKey, cancellationToken);

        if (duplicate)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug(
                    "Notification already delivered for idempotency key {Key}; skipping",
                    intent.IdempotencyKey);
            return;
        }

        // Step 2 â€” Preference resolution (per-employee, per-category).
        var pref = await _db.UserNotificationPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.TenantId == intent.TenantId
                  && p.EmployeeId == intent.RecipientEmployeeId
                  && p.Category == intent.Category,
                cancellationToken);

        // Use defaults when no preference row exists.
        bool inAppEnabled = pref?.InAppEnabled ?? true;
        bool emailEnabled = pref?.EmailEnabled ?? (intent.Category != NotificationCategory.SystemAlerts);
        bool digestEnabled = pref?.DigestEnabled ?? false;
        string? quietStart = pref?.QuietHoursStart;
        string? quietEnd = pref?.QuietHoursEnd;
        string tz = pref?.Timezone ?? "UTC";

        // Step 3 â€” Quiet hours check (best-effort; full scheduler deferred to Phase 2).
        DateTimeOffset? scheduleAfter = null;
        if (!digestEnabled && IsInQuietHours(quietStart, quietEnd, tz))
        {
            scheduleAfter = NextQuietHoursEnd(quietEnd, tz);
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug(
                    "Notification for {RecipientId} deferred until {Until} (quiet hours)",
                    intent.RecipientEmployeeId, scheduleAfter);
        }

        // Step 4 â€” Template resolution: prefer locale, fall back to en-US.
        var template = await ResolveTemplateAsync(
            intent.TenantId, intent.TemplateCode, NotificationChannel.InApp,
            intent.Locale, cancellationToken);

        if (template is null)
        {
            _logger.LogError(
                "No active template found for code={Code} channel=InApp locale={Locale} tenant={TenantId}; " +
                "notification dropped for recipient {RecipientId}",
                intent.TemplateCode, intent.Locale, intent.TenantId, intent.RecipientEmployeeId);
            return;
        }

        // Step 5 â€” Render.
        var (subject, bodyHtml, bodyText) = _renderer.Render(template, intent.Variables);

        // Step 6 â€” Build and persist the NotificationMessage aggregate.
        var message = NotificationMessage.Create(
            tenantId: intent.TenantId,
            recipientEmployeeId: intent.RecipientEmployeeId,
            recipientEmail: intent.RecipientEmail,
            category: intent.Category,
            preferredChannel: NotificationChannel.InApp,
            templateId: template.Id,
            renderedSubject: subject,
            renderedBodyHtml: bodyHtml,
            renderedBodyText: bodyText,
            idempotencyKey: intent.IdempotencyKey,
            scheduleDeliverAfter: scheduleAfter);

        if (digestEnabled)
        {
            message.QueueForDigest();
            _db.NotificationMessages.Add(message);
            await _db.SaveChangesAsync(cancellationToken);
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug(
                    "Message {MessageId} queued for digest (recipient {RecipientId})",
                    message.Id, intent.RecipientEmployeeId);
            return;
        }

        // Step 7 â€” Channel fanout.
        if (inAppEnabled)
        {
            var inApp = _adapters.FirstOrDefault(a => a.Channel == NotificationChannel.InApp);
            if (inApp is not null)
                await SafeDeliverAsync(inApp, message, cancellationToken);
        }

        if (emailEnabled)
        {
            var emailTemplate = await ResolveTemplateAsync(
                intent.TenantId, intent.TemplateCode, NotificationChannel.Email,
                intent.Locale, cancellationToken);

            if (emailTemplate is not null)
            {
                var (es, _, _) = _renderer.Render(emailTemplate, intent.Variables);
                // Email uses same message aggregate; update rendered fields for email body.
                // In production, create a second NotificationMessage per channel if they need
                // separate persistence. Phase 1: reuse same aggregate, log separately.
                if (_logger.IsEnabled(LogLevel.Debug))
                    _logger.LogDebug(
                        "[EMAIL] Subject={Subject}, Recipient={Email}", es, intent.RecipientEmail);
            }

            var email = _adapters.FirstOrDefault(a => a.Channel == NotificationChannel.Email);
            if (email is not null)
                await SafeDeliverAsync(email, message, cancellationToken);
        }

        _db.NotificationMessages.Add(message);
        await _db.SaveChangesAsync(cancellationToken);

        // Step 8 â€” Real-time push (fire-and-forget; failure logged inside PushService).
        var unreadCount = await _db.NotificationMessages
            .AsNoTracking()
            .CountAsync(
                m => m.TenantId == intent.TenantId
                  && m.RecipientEmployeeId == intent.RecipientEmployeeId
                  && !m.IsRead,
                cancellationToken);

        var pushDto = new NotificationPushDto(
            message.Id,
            message.Category.ToString(),
            message.RenderedSubject,
            message.RenderedBodyText,
            IsRead: false,
            CreatedAt: message.CreatedOnUtc);

        await _push.PushAsync(
            intent.TenantId, intent.RecipientEmployeeId,
            unreadCount, pushDto, cancellationToken);
    }

    // â”€â”€ Helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private async Task<NotificationTemplate?> ResolveTemplateAsync(
        string tenantId, string code, NotificationChannel channel,
        string locale, CancellationToken ct)
    {
        // Try exact locale first, fall back to en-US.
        var template = await _db.NotificationTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.TenantId == tenantId
                  && t.Code == code
                  && t.Channel == channel
                  && t.Locale == locale
                  && t.IsActive,
                ct);

        if (template is null && locale != "en-US")
        {
            template = await _db.NotificationTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    t => t.TenantId == tenantId
                      && t.Code == code
                      && t.Channel == channel
                      && t.Locale == "en-US"
                      && t.IsActive,
                    ct);
        }

        return template;
    }

    private async Task SafeDeliverAsync(
        INotificationChannelAdapter adapter,
        NotificationMessage message,
        CancellationToken ct)
    {
        try
        {
            await adapter.DeliverAsync(message, ct);
        }
        catch (Exception ex)
        {
            // Channel failure must not abort the pipeline â€” record permanent failure.
            message.RecordDeliveryAttempt(
                adapter.Channel,
                DeliveryResult.PermanentFailure,
                providerMessageId: null,
                failureReason: ex.Message);

            _logger.LogError(ex,
                "Channel {Channel} delivery failed for message {MessageId}",
                adapter.Channel, message.Id);
        }
    }

    private static bool IsInQuietHours(string? start, string? end, string tz)
    {
        if (start is null || end is null) return false;

        try
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById(tz);
            var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
            var current = local.TimeOfDay;

            var s = TimeSpan.Parse(start, CultureInfo.InvariantCulture);
            var e = TimeSpan.Parse(end, CultureInfo.InvariantCulture);

            // Quiet window may wrap midnight (e.g. 20:00 â†’ 08:00).
            return s < e ? current >= s && current < e : current >= s || current < e;
        }
        catch
        {
            return false;
        }
    }

    private static DateTimeOffset NextQuietHoursEnd(string? end, string tz)
    {
        if (end is null) return DateTimeOffset.UtcNow.AddHours(8);

        try
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById(tz);
            var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
            var endTime = TimeSpan.Parse(end, CultureInfo.InvariantCulture);

            var candidate = local.Date + endTime;
            if (candidate <= local) candidate = candidate.AddDays(1);

            return new DateTimeOffset(candidate, zone.GetUtcOffset(candidate));
        }
        catch
        {
            return DateTimeOffset.UtcNow.AddHours(8);
        }
    }
}
