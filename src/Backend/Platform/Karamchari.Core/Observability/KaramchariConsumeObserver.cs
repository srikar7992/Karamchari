// -----------------------------------------------------------------------
// <copyright file="KaramchariConsumeObserver.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Diagnostics;
using MassTransit;

namespace Karamchari.Core.Observability;

/// <summary>
/// MassTransit <see cref="IConsumeObserver"/> that records per-event consumer latency
/// and fault counts into <see cref="KaramchariEventMetrics"/>.
///
/// Registered as a singleton via <c>x.AddConsumeObserver&lt;KaramchariConsumeObserver&gt;()</c>.
///
/// Timing strategy: stores a <see cref="Stopwatch"/> per message (keyed by <c>MessageId</c>)
/// in <c>PreConsume</c> and reads elapsed time in <c>PostConsume</c>/<c>ConsumeFault</c>.
/// The ConcurrentDictionary is bounded in practice — MassTransit concurrency limits control
/// how many in-flight messages exist simultaneously.
/// </summary>
public sealed class KaramchariConsumeObserver : IConsumeObserver
{
    private readonly KaramchariEventMetrics _metrics;
    private readonly ConcurrentDictionary<Guid, Stopwatch> _timers = new();

    public KaramchariConsumeObserver(KaramchariEventMetrics metrics)
    {
        _metrics = metrics;
    }

    /// <inheritdoc />
    public Task PreConsume<T>(ConsumeContext<T> context) where T : class
    {
        if (context.MessageId.HasValue)
            _timers[context.MessageId.Value] = Stopwatch.StartNew();

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task PostConsume<T>(ConsumeContext<T> context) where T : class
    {
        var elapsedMs = StopAndRemove(context.MessageId) ?? Activity.Current?.Duration.TotalMilliseconds ?? 0;
        var eventName = typeof(T).Name;
        var consumerName = ResolveConsumerName(context);
        var tenantId = context.Headers.Get<string>("x-tenant-id") ?? "unknown";

        _metrics.RecordConsumerLatency(eventName, consumerName, tenantId, elapsedMs);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ConsumeFault<T>(ConsumeContext<T> context, Exception exception) where T : class
    {
        StopAndRemove(context.MessageId);

        var eventName = typeof(T).Name;
        var consumerName = ResolveConsumerName(context);
        var tenantId = context.Headers.Get<string>("x-tenant-id") ?? "unknown";
        var errorType = exception.GetType().Name;

        _metrics.RecordConsumeFault(eventName, consumerName, tenantId, errorType);

        // Dead-letter detection: MassTransit sets a header when moving to DLQ.
        // After MaxRetries fault the observer fires with a MassTransitException wrapper.
        if (exception is MassTransitException { InnerException: not null })
            _metrics.RecordDeadLetter(eventName, tenantId);

        return Task.CompletedTask;
    }

    private double? StopAndRemove(Guid? messageId)
    {
        if (messageId.HasValue && _timers.TryRemove(messageId.Value, out var sw))
        {
            sw.Stop();
            return sw.Elapsed.TotalMilliseconds;
        }

        return null;
    }

    private static string ResolveConsumerName<T>(ConsumeContext<T> context) where T : class
    {
        // MassTransit sets the consumer type name on the receive endpoint name.
        // Use the receive endpoint as a readable consumer identifier.
        return context.ReceiveContext.InputAddress.Segments.LastOrDefault() ?? typeof(T).Name;
    }
}
