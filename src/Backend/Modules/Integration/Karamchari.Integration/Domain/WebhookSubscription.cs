using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Integration.Domain;

public enum WebhookDeliveryStatus { Pending, Delivered, Failed, Skipped }

public record WebhookSubscriptionId(Guid Value)
{
    public static WebhookSubscriptionId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Tenant-registered outbound webhook endpoint.
/// Receives events matching the subscribed event types via HTTPS POST.
/// Payloads are HMAC-SHA256 signed using the stored secret.
/// </summary>
public sealed class WebhookSubscription : AggregateRoot<WebhookSubscriptionId>, ITenantOwned
{
    private readonly List<WebhookDelivery> _deliveries = [];
    private readonly List<string> _eventTypes = [];

    private WebhookSubscription() { TenantId = string.Empty; TargetUrl = string.Empty; Secret = string.Empty; Description = string.Empty; }

    private WebhookSubscription(WebhookSubscriptionId id, string tenantId, string targetUrl,
        string secret, string description, IEnumerable<string> eventTypes) : base(id)
    {
        TenantId = tenantId;
        TargetUrl = targetUrl;
        Secret = secret;
        Description = description;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
        _eventTypes.AddRange(eventTypes.Distinct(StringComparer.OrdinalIgnoreCase));
    }

    public string TenantId { get; private set; }
    public string TargetUrl { get; private set; }

    /// <summary>HMAC-SHA256 signing secret. Store hashed in production.</summary>
    public string Secret { get; private set; }
    public string Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastTriggeredAt { get; private set; }

    /// <summary>Pipe-separated event type filter, e.g. "EmployeeTerminated|LeaveApproved".</summary>
    public string EventTypeFilter { get; private set; } = string.Empty;

    public IReadOnlyList<string> EventTypes => _eventTypes.AsReadOnly();
    public IReadOnlyList<WebhookDelivery> Deliveries => _deliveries.AsReadOnly();

    public static WebhookSubscription Create(string tenantId, string targetUrl, string secret,
        string description, IEnumerable<string> eventTypes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        if (!Uri.TryCreate(targetUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new ArgumentException("TargetUrl must be a valid http/https URL.");

        var types = eventTypes.ToList();
        if (types.Count == 0) throw new ArgumentException("At least one event type required.");

        var sub = new WebhookSubscription(WebhookSubscriptionId.New(), tenantId, targetUrl,
            secret, description, types);
        sub.EventTypeFilter = string.Join("|", sub._eventTypes);
        return sub;
    }

    public bool Matches(string eventType) =>
        IsActive && _eventTypes.Contains(eventType, StringComparer.OrdinalIgnoreCase);

    public WebhookDelivery RecordDelivery(string eventType, string payloadJson, string? correlationId = null)
    {
        var delivery = new WebhookDelivery(Guid.NewGuid(), Id, eventType, payloadJson, correlationId);
        _deliveries.Add(delivery);
        LastTriggeredAt = DateTimeOffset.UtcNow;
        return delivery;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;

    public void UpdateEventTypes(IEnumerable<string> eventTypes)
    {
        _eventTypes.Clear();
        _eventTypes.AddRange(eventTypes.Distinct(StringComparer.OrdinalIgnoreCase));
        EventTypeFilter = string.Join("|", _eventTypes);
    }
}

/// <summary>
/// Tracks a single outbound webhook delivery attempt with retry metadata.
/// </summary>
public sealed class WebhookDelivery : Entity<Guid>
{
    private WebhookDelivery() { PayloadJson = string.Empty; EventType = string.Empty; }

    internal WebhookDelivery(Guid id, WebhookSubscriptionId subscriptionId,
        string eventType, string payloadJson, string? correlationId = null) : base(id)
    {
        SubscriptionId = subscriptionId;
        EventType = eventType;
        PayloadJson = payloadJson;
        CorrelationId = correlationId;
        Status = WebhookDeliveryStatus.Pending;
        AttemptCount = 0;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public WebhookSubscriptionId SubscriptionId { get; private set; } = null!;
    public string EventType { get; private set; }
    public string PayloadJson { get; private set; }

    /// <summary>
    /// Correlation ID propagated from the originating integration event header
    /// (<see cref="Karamchari.Core.Messaging.Tenant.ExecutionContextHeaders.CorrelationId"/>).
    /// Enables tracing: API request → MassTransit event → webhook delivery.
    /// Null for deliveries created before Phase 14.
    /// </summary>
    public string? CorrelationId { get; private set; }

    public WebhookDeliveryStatus Status { get; private set; }
    public int AttemptCount { get; private set; }
    public int? ResponseStatusCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastAttemptAt { get; private set; }
    public DateTimeOffset? NextRetryAt { get; private set; }

    public void MarkDelivered(int httpStatusCode)
    {
        AttemptCount++;
        ResponseStatusCode = httpStatusCode;
        Status = WebhookDeliveryStatus.Delivered;
        LastAttemptAt = DateTimeOffset.UtcNow;
        NextRetryAt = null;
        ErrorMessage = null;
    }

    public void MarkFailed(string error, int? httpStatusCode = null)
    {
        AttemptCount++;
        ErrorMessage = error;
        ResponseStatusCode = httpStatusCode;
        LastAttemptAt = DateTimeOffset.UtcNow;

        // Exponential backoff: 30s, 2m, 10m, 1h, give up
        if (AttemptCount >= 5)
        {
            Status = WebhookDeliveryStatus.Failed;
            NextRetryAt = null;
        }
        else
        {
            var backoffSeconds = (int)Math.Pow(4, AttemptCount) * 30;
            NextRetryAt = DateTimeOffset.UtcNow.AddSeconds(backoffSeconds);
        }
    }

    public void Skip() => Status = WebhookDeliveryStatus.Skipped;
}
