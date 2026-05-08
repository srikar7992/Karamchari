using System.Globalization;
using System.Text;
using Karamchari.Notifications.Domain;
using Karamchari.Notifications.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.Notifications.Services;

/// <summary>
/// Aggregates DigestQueued NotificationMessages into DigestBatch aggregates
/// for a given tenant. Called by DigestGenerationWorker on schedule.
///
/// Safety guarantees:
/// - Idempotency key blocks double-batch for same recipient+type+window.
/// - Source messages transitioned to Delivered after batch creation so
///   they are never included in a second digest.
/// - Max messages per digest: 50 (prevents giant email bodies).
/// </summary>
public sealed class DigestAggregatorService
{
    private const int MaxMessagesPerDigest = 50;

    private readonly NotificationsDbContext _db;
    private readonly ILogger<DigestAggregatorService> _logger;

    public DigestAggregatorService(
        NotificationsDbContext db,
        ILogger<DigestAggregatorService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Generates daily digest batches for all recipients with queued messages.
    /// Returns the count of batches created.
    /// </summary>
    public async Task<int> GenerateDailyDigestsAsync(
        string tenantId,
        DateOnly windowDate,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        var windowKey = windowDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return await GenerateDigestsInternalAsync(
            tenantId, DigestType.Daily, windowKey, cancellationToken);
    }

    /// <summary>
    /// Generates weekly digest batches for all recipients with queued messages.
    /// WindowKey format: "2026-W19" (ISO week number).
    /// </summary>
    public async Task<int> GenerateWeeklyDigestsAsync(
        string tenantId,
        DateOnly anyDateInWeek,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        var cal = CultureInfo.InvariantCulture.Calendar;
        var weekNumber = cal.GetWeekOfYear(anyDateInWeek.ToDateTime(TimeOnly.MinValue), System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        var windowKey = $"{anyDateInWeek.Year:D4}-W{weekNumber:D2}";

        return await GenerateDigestsInternalAsync(
            tenantId, DigestType.Weekly, windowKey, cancellationToken);
    }

    private async Task<int> GenerateDigestsInternalAsync(
        string tenantId,
        DigestType digestType,
        string windowKey,
        CancellationToken ct)
    {
        // Load queued messages grouped by recipient, oldest first.
        var queued = await _db.NotificationMessages
            .Where(m => m.TenantId == tenantId
                     && m.Status == NotificationStatus.DigestQueued)
            .OrderBy(m => m.CreatedOnUtc)
            .Take(5000)                          // safety cap: max 5000 queued msgs per run
            .Select(m => new
            {
                m.Id,
                m.RecipientEmployeeId,
                m.RecipientEmail,
                m.Category,
                m.RenderedSubject,
                m.RenderedBodyText,
                m.CreatedOnUtc,
            })
            .ToListAsync(ct);

        if (queued.Count == 0) return 0;

        var grouped = queued
            .GroupBy(m => m.RecipientEmployeeId)
            .ToList();

        int batchesCreated = 0;

        foreach (var group in grouped)
        {
            var recipientId = group.Key;
            var messages = group.Take(MaxMessagesPerDigest).ToList();
            var recipientEmail = messages[0].RecipientEmail;

            var idempotencyKey = $"{tenantId}:{recipientId}:{digestType}:{windowKey}";

            // Skip if batch already exists for this window.
            var exists = await _db.DigestBatches
                .AnyAsync(b => b.IdempotencyKey == idempotencyKey, ct);

            if (exists)
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                    _logger.LogDebug(
                        "Digest batch already exists for key {Key}; skipping",
                        idempotencyKey);
                continue;
            }

            var sourceIds = messages.Select(m => m.Id).ToList();
            var (subject, bodyHtml, bodyText) = RenderDigestBody(digestType, windowKey, messages
                .Select(m => (m.Category, m.RenderedSubject, m.RenderedBodyText))
                .ToList());

            var batch = DigestBatch.Create(
                tenantId, recipientId, recipientEmail,
                digestType, windowKey,
                sourceIds.AsReadOnly(),
                subject, bodyHtml, bodyText);

            _db.DigestBatches.Add(batch);

            // Transition source messages out of DigestQueued so they won't be re-batched.
            var sourceMessages = await _db.NotificationMessages
                .Where(m => sourceIds.Contains(m.Id))
                .ToListAsync(ct);

            foreach (var msg in sourceMessages)
                msg.RecordDeliveryAttempt(
                    NotificationChannel.InApp,
                    DeliveryResult.Success,
                    providerMessageId: $"digest:{batch.Id}",
                    failureReason: null);

            await _db.SaveChangesAsync(ct);
            batchesCreated++;
        }

        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation(
                "DigestAggregator created {Count} {Type} batches for tenant {TenantId} window {Window}",
                batchesCreated, digestType, tenantId, windowKey);

        return batchesCreated;
    }

    private static (string Subject, string BodyHtml, string BodyText) RenderDigestBody(
        DigestType digestType,
        string windowKey,
        List<(NotificationCategory Category, string RenderedSubject, string RenderedBodyText)> messages)
    {
        var typeLabel = digestType == DigestType.Daily ? "Daily" : "Weekly";
        var subject = string.Create(
            CultureInfo.InvariantCulture,
            $"Your {typeLabel} Digest — {windowKey} ({messages.Count} updates)");

        var grouped = messages
            .GroupBy(m => m.Category)
            .OrderBy(g => g.Key.ToString());

        var textSb = new StringBuilder();
        var htmlSb = new StringBuilder();

        htmlSb.AppendLine("<html><body>");
        htmlSb.AppendLine(CultureInfo.InvariantCulture, $"<h2>Your {typeLabel} Digest</h2>");
        htmlSb.AppendLine(CultureInfo.InvariantCulture, $"<p>{messages.Count} updates for {windowKey}</p>");

        textSb.AppendLine(CultureInfo.InvariantCulture, $"YOUR {typeLabel.ToUpperInvariant()} DIGEST — {windowKey}");
        textSb.AppendLine(CultureInfo.InvariantCulture, $"{messages.Count} updates");
        textSb.AppendLine();

        foreach (var categoryGroup in grouped)
        {
            var categoryName = categoryGroup.Key.ToString();
            var items = categoryGroup.ToList();

            htmlSb.AppendLine(CultureInfo.InvariantCulture, $"<h3>{categoryName} ({items.Count})</h3><ul>");
            textSb.AppendLine(CultureInfo.InvariantCulture, $"{categoryName?.ToUpperInvariant()} ({items.Count}):");

            foreach (var item in items.Take(10))
            {
                htmlSb.AppendLine(CultureInfo.InvariantCulture,
                    $"<li>{System.Net.WebUtility.HtmlEncode(item.RenderedSubject)}</li>");
                textSb.AppendLine(CultureInfo.InvariantCulture, $"  - {item.RenderedSubject}");
            }

            if (items.Count > 10)
            {
                htmlSb.AppendLine(CultureInfo.InvariantCulture, $"<li>...and {items.Count - 10} more</li>");
                textSb.AppendLine(CultureInfo.InvariantCulture, $"  ...and {items.Count - 10} more");
            }

            htmlSb.AppendLine("</ul>");
            textSb.AppendLine();
        }

        htmlSb.AppendLine("</body></html>");

        return (subject, htmlSb.ToString(), textSb.ToString());
    }
}
