using System.Collections.Concurrent;
using System.Globalization;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Multitenancy.Execution;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.BackgroundJobs.Tenant;

/// <summary>
/// Represents a scoped tenant execution context for background jobs.
/// Manages tenant context lifecycle including creation, validation, and disposal.
/// Implements IBackgroundTenantScope to provide tenant isolation for background job processing.
/// </summary>
public sealed class TenantJobExecutionScope : IBackgroundTenantScope
{
    /// <summary>
    /// Thread-local storage slots for tenant execution envelopes, keyed by managed thread ID.
    /// </summary>
    private static readonly ConcurrentDictionary<string, AsyncLocal<TenantExecutionEnvelope>> _asyncLocalSlots = new();

    /// <summary>
    /// Default maximum age for tenant context before it is considered stale.
    /// </summary>
    private static readonly TimeSpan DefaultMaxContextAge = TimeSpan.FromHours(24);

    /// <summary>
    /// Serializer used to convert tenant envelopes to and from serialized format.
    /// </summary>
    private readonly ITenantJobContextSerializer _serializer;

    /// <summary>
    /// Provider used to resolve tenant information when needed.
    /// </summary>
    private readonly ITenantProvider _tenantProvider;

    /// <summary>
    /// Logger instance for logging scope operations.
    /// </summary>
    private readonly ILogger<TenantJobExecutionScope> _logger;

    /// <summary>
    /// The tenant execution envelope managed by this scope.
    /// </summary>
    private readonly TenantExecutionEnvelope _envelope;

    /// <summary>
    /// The serialized context string used for persistence or transmission.
    /// </summary>
    private readonly string _serializedContext;

    /// <summary>
    /// Indicates whether this scope owns the tenant context and should dispose of it.
    /// </summary>
    private readonly bool _ownsContext;

    /// <summary>
    /// Tracks whether the scope has been disposed.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Gets the current tenant identifier from the managed envelope.
    /// </summary>
    public string CurrentTenantId => _envelope.TenantId;

    /// <summary>
    /// Gets a value indicating whether the current tenant context is valid and not disposed.
    /// </summary>
    public bool IsValid => _envelope != null && !_disposed;

    /// <summary>
    /// Initializes a new instance of the TenantJobExecutionScope class.
    /// </summary>
    /// <param name="envelope">The tenant execution envelope to manage.</param>
    /// <param name="serializedContext">The serialized context string for persistence.</param>
    /// <param name="serializer">The tenant job context serializer for serialization operations.</param>
    /// <param name="tenantProvider">The tenant provider for resolving tenant information.</param>
    /// <param name="logger">The logger instance for logging operations.</param>
    /// <param name="ownsContext">Indicates whether this scope owns the context and should dispose of it.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
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

    /// <summary>
    /// Creates a new tenant job execution scope from the current tenant context.
    /// </summary>
    /// <param name="serializer">The tenant job context serializer for serializing the envelope.</param>
    /// <param name="tenantProvider">The tenant provider to get the current tenant.</param>
    /// <param name="logger">The logger instance for logging operations.</param>
    /// <returns>A new tenant job execution scope initialized with the current tenant context.</returns>
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

    /// <summary>
    /// Creates a tenant job execution scope from an existing tenant execution envelope.
    /// Validates that the envelope is not stale before creating the scope.
    /// </summary>
    /// <param name="envelope">The tenant execution envelope to create the scope from.</param>
    /// <param name="serializer">The tenant job context serializer for serialization and validation operations.</param>
    /// <param name="tenantProvider">The tenant provider for resolving tenant information.</param>
    /// <param name="logger">The logger instance for logging operations.</param>
    /// <returns>A new tenant job execution scope initialized with the provided envelope.</returns>
    /// <exception cref="ArgumentNullException">Thrown when envelope is null.</exception>
    /// <exception cref="StaleTenantRestoreException">Thrown when the tenant context is stale and cannot be restored.</exception>
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
