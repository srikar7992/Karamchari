// -----------------------------------------------------------------------
// <copyright file="TenantJobExecutionScope.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Globalization;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Multitenancy.Execution;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.BackgroundJobs.Tenant;

/// <summary>
/// A scoped container for tenant background job execution.
/// Manages the restoration and validation of tenant execution context for jobs.
/// </summary>
public sealed partial class TenantJobExecutionScope : IBackgroundTenantScope
{
    /// <summary>
    /// Default maximum age for tenant context before it is considered stale.
    /// </summary>
    private static readonly TimeSpan DefaultMaxContextAge = TimeSpan.FromHours(24);

    private readonly TenantExecutionEnvelope _envelope;
    private readonly string _serializedContext;
    private readonly ITenantJobContextSerializer _serializer;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<TenantJobExecutionScope> _logger;
    private readonly IDisposable? _scope;
    private bool _disposed;

    private TenantJobExecutionScope(
        TenantExecutionEnvelope envelope,
        string serializedContext,
        ITenantJobContextSerializer serializer,
        ITenantProvider tenantProvider,
        ILogger<TenantJobExecutionScope> logger,
        bool ownsContext = true)
    {
        _envelope = envelope ?? throw new ArgumentNullException(nameof(envelope));
        _serializedContext = serializedContext ?? throw new ArgumentNullException(nameof(serializedContext));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (ownsContext)
        {
            var context = new TenantExecutionContext(_envelope);
            _scope = context.Establish();

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(
                    "Established tenant job execution scope for {TenantId}, CorrelationId {CorrelationId}",
                    _envelope.TenantId,
                    _envelope.CorrelationId);
            }
        }
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
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            ExecutionSource.BackgroundJob,
            TenantSource.Background)
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

    /// <summary>
    /// Creates a scope from a serialized context string.
    /// </summary>
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

    /// <inheritdoc/>
    public string CurrentTenantId => _envelope.TenantId;

    /// <inheritdoc/>
    public bool IsValid => !_disposed && !string.IsNullOrWhiteSpace(_envelope.TenantId);

    /// <inheritdoc/>
    public TenantExecutionEnvelope GetCurrentEnvelope()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(TenantJobExecutionScope));
        }

        return _envelope;
    }

    /// <inheritdoc/>
    public Task<TenantExecutionEnvelope> GetEnvelopeAsync(CancellationToken ct)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(TenantJobExecutionScope));
        }

        return Task.FromResult(_envelope);
    }

    /// <inheritdoc/>
    public string GetSerializedContext() => _serializedContext;

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public TenantExecutionEnvelope GetEnvelopeWithIncrementedRetry()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(TenantJobExecutionScope));
        }

        return _envelope.With(retryAttempt: _envelope.RetryAttempt + 1, source: TenantSource.Background);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _scope?.Dispose();

        LogDisposingScope(_logger, _envelope.TenantId);
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Established tenant job execution scope for {TenantId}, CorrelationId {CorrelationId}")]
    static partial void LogEstablishedScope(ILogger logger, string tenantId, string correlationId);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "Disposing tenant scope for {TenantId}")]
    static partial void LogDisposingScope(ILogger logger, string tenantId);
}
