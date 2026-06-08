using System.Diagnostics;
using MassTransit;

namespace Karamchari.Core.Observability;

/// <summary>
/// MassTransit <see cref="IPublishObserver"/> that records per-event publish metrics
/// into <see cref="KaramchariEventMetrics"/>.
///
/// Registered as a singleton via <c>x.AddPublishObserver&lt;KaramchariPublishObserver&gt;()</c>.
/// Fires synchronously during the publish pipeline — keep work minimal.
/// </summary>
public sealed class KaramchariPublishObserver : IPublishObserver
{
    private readonly KaramchariEventMetrics _metrics;

    public KaramchariPublishObserver(KaramchariEventMetrics metrics)
    {
        _metrics = metrics;
    }

    /// <inheritdoc />
    public Task PrePublish<T>(PublishContext<T> context) where T : class
        => Task.CompletedTask;

    /// <inheritdoc />
    public Task PostPublish<T>(PublishContext<T> context) where T : class
    {
        var eventName = typeof(T).Name;
        var tenantId = ResolveTenantId(context.Headers);

        // Publisher is not embedded in the event type itself; fall back to header or "Unknown".
        var publisher = context.Headers.Get<string>("x-publisher") ?? "Unknown";

        _metrics.RecordPublish(eventName, publisher, tenantId);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task PublishFault<T>(PublishContext<T> context, Exception exception) where T : class
    {
        var eventName = typeof(T).Name;
        var tenantId = ResolveTenantId(context.Headers);
        var errorType = exception.GetType().Name;

        _metrics.RecordPublishFault(eventName, tenantId, errorType);

        return Task.CompletedTask;
    }

    private static string ResolveTenantId(Headers headers)
    {
        // Tenant ID is stamped into message headers by TenantPublishFilter before publish.
        var fromHeader = headers.Get<string>("x-tenant-id");
        return string.IsNullOrEmpty(fromHeader) ? "unknown" : fromHeader;
    }
}
