using Karamchari.Core.Multitenancy;
using Karamchari.Core.Multitenancy.Execution;
using MassTransit;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Messaging.Tenant;

/// <summary>
/// A scoped service that recreates the TenantExecutionContext from message headers when a consumer starts processing.
/// Provides fault tolerance for missing context and supports consumer retry scenarios with telemetry.
/// </summary>
public sealed class TenantMessageConsumerScope : IDisposable
{
    private readonly ILogger<TenantMessageConsumerScope> _logger;
    private readonly IMemoryCache _telemetryCache;
    private TenantExecutionContext? _executionContext;
    private IDisposable? _scope;
    private bool _disposed;

    private record TelemetryEntry(string ConsumerName, DateTime StartTime, string TenantId, int AttemptCount);

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantMessageConsumerScope"/> class.
    /// </summary>
    /// <param name="logger">The logger for tenant message consumer scope events.</param>
    /// <param name="telemetryCache">The memory cache for telemetry storage.</param>
    public TenantMessageConsumerScope(
        ILogger<TenantMessageConsumerScope> logger,
        IMemoryCache telemetryCache)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _telemetryCache = telemetryCache ?? throw new ArgumentNullException(nameof(telemetryCache));
    }

    /// <summary>
    /// Gets the current tenant execution context for this consumer scope.
    /// </summary>
    public TenantExecutionContext? CurrentContext => _executionContext;

    /// <summary>
    /// Gets the current tenant identifier for this consumer scope.
    /// </summary>
    public string? CurrentTenantId => _executionContext?.TenantId;

    /// <summary>
    /// Recreates the TenantExecutionContext from message headers and validates it against the expected tenant.
    /// </summary>
    /// <param name="consumeContext">The MassTransit consume context.</param>
    /// <param name="expectedTenantId">Optional expected tenant identifier for validation.</param>
    /// <returns>The recreated TenantExecutionContext.</returns>
    /// <exception cref="MalformedTenantMessageException">Thrown when tenant metadata is malformed or missing.</exception>
    public TenantExecutionContext? RecreateFromMessage(ConsumeContext consumeContext, string? expectedTenantId = null)
    {
        ThrowIfDisposed();

        _logger.LogInformation(
            "Recreating tenant context from message {MessageId}, ConversationId {ConversationId}",
            consumeContext.MessageId, consumeContext.ConversationId);

        if (!consumeContext.Headers.TryGetHeader(TenantMessageHeaderKeys.TenantId, out var tenantIdObj) ||
            tenantIdObj is not string tenantId || string.IsNullOrWhiteSpace(tenantId))
        {
            _logger.LogWarning("Failed to recreate tenant context: Missing TenantId in message {MessageId}", consumeContext.MessageId);
            throw new MalformedTenantMessageException(
                "Cannot recreate tenant context: message is missing tenant identifier",
                null, TenantMessageFailureReason.MissingTenantId);
        }

        TenantExecutionEnvelope? envelope = null;
        if (consumeContext.Headers.TryGetHeader(TenantMessageHeaderKeys.ExecutionEnvelope, out var envelopeObj) &&
            envelopeObj is string envelopeJson)
        {
            envelope = TenantExecutionEnvelope.FromJson(envelopeJson);
        }

        if (envelope == null)
        {
            consumeContext.Headers.TryGetHeader(TenantMessageHeaderKeys.CorrelationId, out var correlationIdObj);
            var correlationId = correlationIdObj as string ?? consumeContext.ConversationId?.ToString() ?? Guid.NewGuid().ToString();
            var requestId = consumeContext.MessageId?.ToString() ?? Guid.NewGuid().ToString();

            envelope = new TenantExecutionEnvelope(tenantId, correlationId, requestId, ExecutionSource.MessageConsumer, TenantSource.Messaging);
        }

        _executionContext = new TenantExecutionContext(envelope);

        if (!string.IsNullOrWhiteSpace(expectedTenantId) &&
            !string.Equals(_executionContext.TenantId, expectedTenantId, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Tenant mismatch detected: expected {ExpectedTenantId}, got {ActualTenantId}",
                expectedTenantId, _executionContext.TenantId);

            throw new MalformedTenantMessageException(
                $"Tenant identifier mismatch: expected '{expectedTenantId}', got '{_executionContext.TenantId}'",
                _executionContext.TenantId, TenantMessageFailureReason.TenantMismatch);
        }

        _scope = _executionContext.Establish();

        RecordTelemetry(consumeContext);

        _logger.LogInformation(
            "Tenant context recreated successfully for Tenant {TenantId}, CorrelationId {CorrelationId}, Source {Source}",
            _executionContext.TenantId, _executionContext.CorrelationId, _executionContext.Source);

        return _executionContext;
    }

    /// <summary>
    /// Validates that the current tenant matches the expected tenant identifier.
    /// </summary>
    /// <param name="expectedTenantId">The expected tenant identifier.</param>
    /// <param name="throwOnMismatch">Whether to throw an exception on mismatch (default: true).</param>
    /// <returns>True if the tenant matches or is null when expected is null; false otherwise.</returns>
    public bool ValidateTenantMatch(string? expectedTenantId, bool throwOnMismatch = true)
    {
        ThrowIfDisposed();

        if (expectedTenantId == null)
        {
            return true;
        }

        if (_executionContext == null)
        {
            _logger.LogWarning("Cannot validate tenant: no execution context available");

            if (throwOnMismatch)
            {
                throw new MalformedTenantMessageException(
                    "Cannot validate tenant: no execution context available",
                    null, TenantMessageFailureReason.MalformedEnvelope);
            }

            return false;
        }

        if (!string.Equals(_executionContext.TenantId, expectedTenantId, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Tenant validation failed: expected {ExpectedTenantId}, actual {ActualTenantId}",
                expectedTenantId, _executionContext.TenantId);

            if (throwOnMismatch)
            {
                throw new MalformedTenantMessageException(
                    $"Tenant mismatch: expected '{expectedTenantId}', got '{_executionContext.TenantId}'",
                    _executionContext.TenantId, TenantMessageFailureReason.TenantMismatch);
            }

            return false;
        }

        return true;
    }

    /// <summary>
    /// Clears the current execution context and restores the previous scope.
    /// </summary>
    public void ClearContext()
    {
        ThrowIfDisposed();

        if (_scope != null)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Restoring previous tenant execution scope");
            }

            _scope.Dispose();
            _scope = null;
            _executionContext = null;
        }
    }

    /// <summary>
    /// Gets the current retry attempt count from the execution context.
    /// </summary>
    public int CurrentRetryAttempt => _executionContext?.Envelope.RetryAttempt ?? 0;

    /// <summary>
    /// Checks if the current message is a retry attempt.
    /// </summary>
    public bool IsRetryAttempt => CurrentRetryAttempt > 0;

    /// <summary>
    /// Records telemetry for the current consumer operation.
    /// </summary>
    private void RecordTelemetry(ConsumeContext consumeContext)
    {
        if (!_logger.IsEnabled(LogLevel.Debug)) return;

        var consumerName = consumeContext.ReceiveContext.InputAddress?.ToString() ?? "Unknown";
        var entry = new TelemetryEntry(
            consumerName,
            DateTime.UtcNow,
            _executionContext?.TenantId ?? "Unknown",
            CurrentRetryAttempt);

        var cacheKey = $"telemetry:consumer:{Guid.NewGuid()}";
        _telemetryCache.Set(cacheKey, entry, TimeSpan.FromMinutes(30));
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(TenantMessageConsumerScope));
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            ClearContext();
            _disposed = true;
        }
    }
}

/// <summary>
/// Factory for creating TenantMessageConsumerScope instances.
/// </summary>
public interface ITenantMessageConsumerScopeFactory
{
    /// <summary>
    /// Creates a new scoped instance of TenantMessageConsumerScope.
    /// </summary>
    TenantMessageConsumerScope CreateScope();
}

/// <summary>
/// Default implementation of TenantMessageConsumerScopeFactory.
/// </summary>
public sealed class TenantMessageConsumerScopeFactory : ITenantMessageConsumerScopeFactory
{
    private readonly ILogger<TenantMessageConsumerScope> _logger;
    private readonly IMemoryCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantMessageConsumerScopeFactory"/> class.
    /// </summary>
    /// <param name="logger">The logger for tenant message consumer scope events.</param>
    /// <param name="cache">The memory cache for telemetry storage.</param>
    public TenantMessageConsumerScopeFactory(
        ILogger<TenantMessageConsumerScope> logger,
        IMemoryCache cache)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    /// <inheritdoc />
    public TenantMessageConsumerScope CreateScope()
    {
        return new TenantMessageConsumerScope(_logger, _cache);
    }
}

/// <summary>
/// Extension methods for registering TenantMessageConsumerScope with DI.
/// </summary>
public static class TenantMessageConsumerScopeExtensions
{
    /// <summary>
    /// Registers TenantMessageConsumerScope and its factory with the service collection.
    /// </summary>
    public static IServiceCollection AddTenantMessageConsumerScope(this IServiceCollection services)
    {
        services.AddScoped<TenantMessageConsumerScope>();
        services.AddScoped<ITenantMessageConsumerScopeFactory, TenantMessageConsumerScopeFactory>();

        return services;
    }
}
