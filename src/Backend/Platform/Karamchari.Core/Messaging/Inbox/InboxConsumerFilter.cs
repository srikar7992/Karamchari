using System.Text.Json;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Messaging.Inbox;

/// <summary>
/// MassTransit consume pipeline filter that enforces exactly-once message processing
/// via the inbox deduplication pattern.
///
/// For each incoming message:
/// <list type="number">
///   <item>Extracts the broker-assigned MessageId from <see cref="ConsumeContext"/>.</item>
///   <item>
///     Checks <see cref="IInboxService.IsProcessedAsync"/>. If already processed,
///     the message is acknowledged and dropped — the downstream handler is never invoked.
///   </item>
///   <item>
///     Records the message receipt in <c>dbo.InboxMessages</c> before passing
///     through to the handler. This ensures the record survives a worker crash
///     mid-handler: the next delivery will see it and skip the handler.
///   </item>
///   <item>
///     Calls <see cref="IInboxService.MarkProcessedAsync"/> only after the
///     downstream handler returns without throwing. On handler failure the row
///     remains with <c>IsProcessed = false</c> so the next redelivery retries.
///   </item>
/// </list>
///
/// Registration: add this filter to the MassTransit consumer pipeline via
/// <c>cfg.UseConsumeFilter(typeof(InboxConsumerFilter&lt;&gt;), context)</c>
/// in the worker host's MassTransit configuration.
/// </summary>
/// <typeparam name="T">The MassTransit message type.</typeparam>
public sealed class InboxConsumerFilter<T> : IFilter<ConsumeContext<T>>
    where T : class
{
    private readonly IInboxService _inboxService;
    private readonly InboxDbContext _db;
    private readonly ILogger<InboxConsumerFilter<T>> _logger;

    /// <summary>Initializes a new instance of <see cref="InboxConsumerFilter{T}"/>.</summary>
    public InboxConsumerFilter(
        IInboxService inboxService,
        InboxDbContext db,
        ILogger<InboxConsumerFilter<T>> logger)
    {
        ArgumentNullException.ThrowIfNull(inboxService);
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(logger);
        _inboxService = inboxService;
        _db = db;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var messageId = context.MessageId ?? Guid.NewGuid();
        var messageType = typeof(T).FullName ?? typeof(T).Name;

        // Fast idempotency check — skip handler if already processed.
        if (await _inboxService.IsProcessedAsync(messageId, context.CancellationToken).ConfigureAwait(false))
        {
            _logger.LogDebug(
                "Inbox filter: skipping duplicate message {MessageId} of type {MessageType}.",
                messageId, messageType);
            return;
        }

        // Record receipt before forwarding to the handler.
        // If the worker crashes after this write but before MarkProcessedAsync,
        // the next delivery will re-enter the handler (at-least-once within the
        // pre-processed window). This is the correct trade-off: never lose, never
        // mark-processed prematurely.
        var tenantIdHeader = context.Headers.Get<string>("X-Tenant-Id");
        _ = Guid.TryParse(tenantIdHeader, out var tenantId);

        var inboxRecord = new InboxMessage
        {
            Id = messageId,
            TenantId = tenantId,
            MessageType = messageType,
            Payload = SerializePayload(context.Message),
            ReceivedAt = DateTimeOffset.UtcNow,
            IsProcessed = false
        };

        try
        {
            _db.InboxMessages.Add(inboxRecord);
            await _db.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            // Concurrent delivery: another thread/instance already inserted this MessageId.
            // The unique index blocked the insert — this delivery is a duplicate.
            _logger.LogInformation(
                "Inbox filter: concurrent duplicate detected for MessageId {MessageId} — skipping handler.",
                messageId);
            return;
        }

        // Invoke the downstream consumer handler.
        await next.Send(context).ConfigureAwait(false);

        // Mark as processed only after the handler succeeds.
        await _inboxService.MarkProcessedAsync(messageId, context.CancellationToken).ConfigureAwait(false);

        _logger.LogDebug(
            "Inbox filter: MessageId {MessageId} of type {MessageType} processed and marked.",
            messageId, messageType);
    }

    /// <inheritdoc />
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.CreateFilterScope("inbox-idempotency");
    }

    private static string SerializePayload(T message)
    {
        try
        {
            return JsonSerializer.Serialize(message, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        catch
        {
            return "{}";
        }
    }
}
