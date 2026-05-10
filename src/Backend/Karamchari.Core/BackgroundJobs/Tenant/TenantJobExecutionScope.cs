using System.Collections.Concurrent;
using System.Globalization;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Multitenancy.Execution;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.BackgroundJobs.Tenant;

public sealed class TenantJobExecutionScope : IBackgroundTenantScope
{
    private static readonly ConcurrentDictionary<string, AsyncLocal<TenantExecutionEnvelope>> _asyncLocalSlots = new();
    private static readonly TimeSpan DefaultMaxContextAge = TimeSpan.FromHours(24);

    private readonly ITenantJobContextSerializer _serializer;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<TenantJobExecutionScope> _logger;
    private readonly TenantExecutionEnvelope _envelope;
    private readonly string _serializedContext;
    private readonly bool _ownsContext;
    private bool _disposed;

    public string CurrentTenantId => _envelope.TenantId;
    public bool IsValid => _envelope != null && !_disposed;

    private TenantJobExecutionScope(
        TenantExecutionEnvelope envelope,
        string serializedContext,
        ITenantJobContextSerializer serializer,
        ITenantProvider tenantProvider,
        ILogger<TenantJobExecutionScope> logger,
        bool ownsContext = true)
    {
        _envelope = envelope ?? throw new ArgumentNullException(nameof(envelope));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serializedContext = serializedContext ?? throw new ArgumentNullException(nameof(serializedContext));
        _ownsContext = ownsContext;

        SetAsyncLocal(envelope);
    }

    public static TenantJobExecutionScope FromCurrentContext(
        ITenantJobContextSerializer serializer,
        ITenantProvider tenantProvider,
        ILogger<TenantJobExecutionScope> logger)
    {
        var tenant = tenantProvider.GetTenant();
        var envelope = new TenantExecutionEnvelope(
            tenant.TenantId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            ExecutionSource.BackgroundJob)
        {
            Timestamp = DateTime.UtcNow
        };

        var serialized = serializer.Serialize(envelope);

        return new TenantJobExecutionScope(envelope, serialized, serializer, tenantProvider, logger);
    }

    public static TenantJobExecutionScope FromEnvelope(
        TenantExecutionEnvelope envelope,
        ITenantJobContextSerializer serializer,
        ITenantProvider tenantProvider,
        ILogger<TenantJobExecutionScope> logger)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        if (serializer.IsStale(serializer.Serialize(envelope), DefaultMaxContextAge))
        {
            throw new StaleTenantRestoreException(
                string.Create(CultureInfo.InvariantCulture,
                    $"Tenant context for '{envelope.TenantId}' is stale and cannot be restored."),
                envelope.TenantId);
        }

        var serialized = serializer.Serialize(envelope);
        return new TenantJobExecutionScope(envelope, serialized, serializer, tenantProvider, logger);
    }

    public static TenantJobExecutionScope FromSerialized(
        string serializedContext,
        ITenantJobContextSerializer serializer,
        ITenantProvider tenantProvider,
        ILogger<TenantJobExecutionScope> logger)
    {
        if (serializer.IsStale(serializedContext, DefaultMaxContextAge))
        {
            throw new StaleTenantRestoreException(
                "Tenant context is stale and cannot be restored for execution.");
        }

        var envelope = serializer.Deserialize(serializedContext);
        return new TenantJobExecutionScope(envelope, serializedContext, serializer, tenantProvider, logger);
    }

    public TenantExecutionEnvelope GetCurrentEnvelope()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(TenantJobExecutionScope));
        }

        return _envelope;
    }

    public Task<TenantExecutionEnvelope> GetEnvelopeAsync(CancellationToken ct)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(TenantJobExecutionScope));
        }

        return Task.FromResult(_envelope);
    }

    public string GetSerializedContext() => _serializedContext;

    public void ValidateTenantId(string expectedTenantId)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(TenantJobExecutionScope));
        }

        if (string.IsNullOrWhiteSpace(expectedTenantId))
        {
            throw new StaleTenantRestoreException("Expected tenant ID cannot be null or empty.");
        }

        if (!string.Equals(_envelope.TenantId, expectedTenantId, StringComparison.OrdinalIgnoreCase))
        {
            throw new StaleTenantRestoreException(
                string.Create(CultureInfo.InvariantCulture,
                    $"Tenant mismatch: expected '{expectedTenantId}', got '{_envelope.TenantId}'."),
                _envelope.TenantId);
        }
    }

    public TenantExecutionEnvelope GetEnvelopeWithIncrementedRetry()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(TenantJobExecutionScope));
        }

        return _envelope.With(RetryAttempt: _envelope.RetryAttempt + 1);
    }

    private static void SetAsyncLocal(TenantExecutionEnvelope envelope)
    {
        var slot = _asyncLocalSlots.GetOrAdd(
            Thread.CurrentThread.ManagedThreadId.ToString(CultureInfo.InvariantCulture),
            _ => new AsyncLocal<TenantExecutionEnvelope>());

        slot.Value = envelope;
    }

    private static TenantExecutionEnvelope? GetAsyncLocal()
    {
        if (_asyncLocalSlots.TryGetValue(
            Thread.CurrentThread.ManagedThreadId.ToString(CultureInfo.InvariantCulture),
            out var slot))
        {
            return slot.Value;
        }

        return null;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_ownsContext && _logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(
                "Disposing tenant scope for {TenantId}",
                _envelope.TenantId);
        }
    }
}
