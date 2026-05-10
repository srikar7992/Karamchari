using Karamchari.Core.Multitenancy;
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
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(telemetryCache);

        _logger = logger;
        _telemetryCache = telemetryCache;
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

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Recreating tenant context from message {MessageId}, ConversationId {ConversationId}",
                consumeContext.MessageId, consumeContext.ConversationId);
        }

        consumeContext.Headers.TryGetHeader(TenantMessageHeaderKeys.TenantId, out var tenantIdObj);
        var tenantId = tenantIdObj as string;

        if (string.IsNullOrWhiteSpace(tenantId))
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(
                    "Failed to recreate tenant context: Missing tenant identifier in message {MessageId}",
                    consumeContext.MessageId);
            }

            throw new MalformedTenantMessageException(
                "Cannot recreate tenant context: message is missing tenant identifier",
                null, TenantMessageFailureReason.MissingTenantId);
        }

        _executionContext = TenantExecutionContext.CreateFromHeaders(null, consumeContext);

        if (_executionContext == null)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(
                    "Failed to recreate tenant context: Unable to construct context from headers for Tenant {TenantId}",
                    tenantId);
            }

            throw new MalformedTenantMessageException(
                "Cannot recreate tenant context: unable to construct context from headers",
                tenantId, TenantMessageFailureReason.MalformedEnvelope);
        }

        if (!string.IsNullOrWhiteSpace(expectedTenantId) &&
            !string.Equals(_executionContext.TenantId, expectedTenantId, StringComparison.OrdinalIgnoreCase))
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(
                    "Tenant mismatch detected: expected {ExpectedTenantId}, got {ActualTenantId}",
                    expectedTenantId, _executionContext.TenantId);
            }

            throw new MalformedTenantMessageException(
                $"Tenant identifier mismatch: expected '{expectedTenantId}', got '{_executionContext.TenantId}'",
                _executionContext.TenantId, TenantMessageFailureReason.TenantMismatch);
        }

        _executionContext.SetAsCurrent();

        RecordTelemetry(consumeContext);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Tenant context recreated successfully for Tenant {TenantId}, CorrelationId {CorrelationId}, Source {Source}",
                _executionContext.TenantId, _executionContext.CorrelationId, _executionContext.ExecutionSource ?? "Unknown");
        }

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
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning("Cannot validate tenant: no execution context available");
            }

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
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(
                    "Tenant validation failed: expected {ExpectedTenantId}, actual {ActualTenantId}",
                    expectedTenantId, _executionContext.TenantId);
            }

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
    /// Clears the current execution context.
    /// </summary>
    public void ClearContext()
    {
        ThrowIfDisposed();

        if (_executionContext != null)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Clearing tenant execution context for Tenant {TenantId}", _executionContext.TenantId);
            }

            TenantExecutionContext.ClearCurrent();
            _executionContext = null;
        }
    }

    /// <summary>
    /// Gets the current retry attempt count from the execution context.
    /// </summary>
    public int CurrentRetryAttempt => _executionContext?.RetryAttempt ?? 0;

    /// <summary>
    /// Checks if the current message is a retry attempt.
    /// </summary>
    public bool IsRetryAttempt => _executionContext?.RetryAttempt > 0;

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
            _executionContext?.RetryAttempt ?? 0);

        var cacheKey = $"telemetry:{Guid.NewGuid()}";
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
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(cache);

        _logger = logger;
        _cache = cache;
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
