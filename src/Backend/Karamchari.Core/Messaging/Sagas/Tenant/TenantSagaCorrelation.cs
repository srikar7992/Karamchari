using MassTransit;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Messaging.Sagas.Tenant;

/// <summary>
///     Correlation service for saga tenant isolation, ensuring that saga messages
///     are correctly routed and validated based on tenant context.
/// </summary>
public interface ITenantSagaCorrelationService
{
    /// <summary>
    ///     Validates that a message can be correlated with a saga instance.
    /// </summary>
    /// <typeparam name="TSaga">The saga type.</typeparam>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="saga">The saga instance.</param>
    /// <param name="message">The message context.</param>
    /// <returns>A validation result indicating success or failure.</returns>
    TenantSagaCorrelationResult ValidateCorrelation<TSaga, TMessage>(
        TSaga saga,
        ConsumeContext<TMessage> message)
        where TSaga : TenantAwareSagaState
        where TMessage : class;

    /// <summary>
    ///     Validates that the tenant context is properly set on a saga.
    /// </summary>
    /// <typeparam name="TSaga">The saga type.</typeparam>
    /// <param name="saga">The saga instance.</param>
    /// <returns>A validation result.</returns>
    TenantSagaCorrelationResult ValidateTenantContext<TSaga>(TSaga saga)
        where TSaga : TenantAwareSagaState;

    /// <summary>
    ///     Gets safe routing metadata for a message based on tenant context.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="message">The message context.</param>
    /// <returns>Routing metadata for safe message handling.</returns>
    TenantSagaRoutingMetadata GetRoutingMetadata<TMessage>(ConsumeContext<TMessage> message)
        where TMessage : class;
}

/// <summary>
///     Routing metadata for tenant-aware saga message handling.
/// </summary>
public sealed class TenantSagaRoutingMetadata
{
    /// <summary>
    ///     Gets the tenant identifier for routing.
    /// </summary>
    public string TenantId { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the correlation identifier for routing.
    /// </summary>
    public Guid CorrelationId { get; init; }

    /// <summary>
    ///     Gets whether the routing is safe (no cross-tenant risk).
    /// </summary>
    public bool IsSafe { get; init; }

    /// <summary>
    ///     Gets any warnings associated with the routing.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}

/// <summary>
///     Result of tenant saga correlation validation.
/// </summary>
public sealed class TenantSagaCorrelationResult
{
    /// <summary>
    ///     Gets a value indicating whether validation passed.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    ///     Gets the type of violation if validation failed.
    /// </summary>
    public TenantCorrelationViolationType ViolationType { get; init; }

    /// <summary>
    ///     Gets the error message if validation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    ///     Gets the tenant identifier from the message.
    /// </summary>
    public string? MessageTenantId { get; init; }

    /// <summary>
    ///     Gets the tenant identifier from the saga.
    /// </summary>
    public string? SagaTenantId { get; init; }

    /// <summary>
    ///     Gets the correlation identifier from the message.
    /// </summary>
    public Guid? MessageCorrelationId { get; init; }

    /// <summary>
    ///     Gets the correlation identifier from the saga.
    /// </summary>
    public Guid? SagaCorrelationId { get; init; }

    /// <summary>
    ///     Creates a successful validation result.
    /// </summary>
    public static TenantSagaCorrelationResult Success() => new() { IsValid = true };

    /// <summary>
    ///     Creates a failed validation result.
    /// </summary>
    public static TenantSagaCorrelationResult Failure(
        TenantCorrelationViolationType violationType,
        string errorMessage,
        string? messageTenantId = null,
        string? sagaTenantId = null,
        Guid? messageCorrelationId = null,
        Guid? sagaCorrelationId = null)
        => new()
        {
            IsValid = false,
            ViolationType = violationType,
            ErrorMessage = errorMessage,
            MessageTenantId = messageTenantId,
            SagaTenantId = sagaTenantId,
            MessageCorrelationId = messageCorrelationId,
            SagaCorrelationId = sagaCorrelationId
        };

    /// <summary>
    ///     Throws an exception if validation failed.
    /// </summary>
    public void ThrowIfInvalid()
    {
        if (!IsValid)
        {
            throw new TenantSagaCorrelationException(
                ErrorMessage ?? "Tenant saga correlation validation failed",
                MessageTenantId,
                SagaTenantId,
                MessageCorrelationId,
                SagaCorrelationId,
                ViolationType);
        }
    }
}

/// <summary>
///     Default implementation of the tenant saga correlation service.
/// </summary>
public sealed class TenantSagaCorrelationService : ITenantSagaCorrelationService
{
    private readonly ILogger<TenantSagaCorrelationService> _logger;
    private static readonly string[] _tenantHeaderKeys = { "TenantId", "tenant_id", "X-Tenant-Id" };

    public TenantSagaCorrelationService(ILogger<TenantSagaCorrelationService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public TenantSagaCorrelationResult ValidateCorrelation<TSaga, TMessage>(
        TSaga saga,
        ConsumeContext<TMessage> message)
        where TSaga : TenantAwareSagaState
        where TMessage : class
    {
        var messageTenantId = ExtractTenantId(message);
        var sagaTenantId = saga.TenantId;

        if (string.IsNullOrEmpty(messageTenantId))
        {
            _logger.LogWarning(
                "TenantSagaCorrelationService: Message missing tenant ID for {MessageType}",
                typeof(TMessage).Name);
            return TenantSagaCorrelationResult.Failure(
                TenantCorrelationViolationType.MissingTenantId,
                "Message is missing tenant identifier",
                messageTenantId,
                sagaTenantId);
        }

        if (string.IsNullOrEmpty(sagaTenantId))
        {
            _logger.LogWarning(
                "TenantSagaCorrelationService: Saga has no tenant context set");
            return TenantSagaCorrelationResult.Failure(
                TenantCorrelationViolationType.SagaTenantNotSet,
                "Saga tenant context is not set",
                messageTenantId,
                sagaTenantId);
        }

        if (TenantSagaGuard.IsCrossTenantAttempt(messageTenantId, sagaTenantId))
        {
            _logger.LogError(
                "TenantSagaCorrelationService: Cross-tenant correlation blocked. " +
                "Message tenant: {MessageTenantId}, Saga tenant: {SagaTenantId}",
                messageTenantId,
                sagaTenantId);
            return TenantSagaCorrelationResult.Failure(
                TenantCorrelationViolationType.CrossTenantAttempt,
                $"Cross-tenant correlation blocked. Message tenant '{messageTenantId}' differs from saga tenant '{sagaTenantId}'",
                messageTenantId,
                sagaTenantId,
                Guid.TryParse(GetType().Name, out var _) ? message.CorrelationId : null,
                saga.CorrelationId);
        }

        if (message.CorrelationId != saga.CorrelationId && saga.CorrelationId != Guid.Empty)
        {
            _logger.LogWarning(
                "TenantSagaCorrelationService: Correlation ID mismatch. " +
                "Message: {MessageCorrelationId}, Saga: {SagaCorrelationId}",
                message.CorrelationId,
                saga.CorrelationId);
            return TenantSagaCorrelationResult.Failure(
                TenantCorrelationViolationType.CorrelationIdMismatch,
                $"Correlation ID mismatch. Message correlation '{message.CorrelationId}' differs from saga correlation '{saga.CorrelationId}'",
                messageTenantId,
                sagaTenantId,
                message.CorrelationId,
                saga.CorrelationId);
        }

        _logger.LogDebug(
            "TenantSagaCorrelationService: Correlation validated successfully for saga {SagaType} tenant {TenantId}",
            typeof(TSaga).Name,
            sagaTenantId);
        return TenantSagaCorrelationResult.Success();
    }

    /// <inheritdoc />
    public TenantSagaCorrelationResult ValidateTenantContext<TSaga>(TSaga saga)
        where TSaga : TenantAwareSagaState
    {
        if (string.IsNullOrEmpty(saga.TenantId))
        {
            _logger.LogWarning(
                "TenantSagaCorrelationService: Saga {SagaType} has no tenant context",
                typeof(TSaga).Name);
            return TenantSagaCorrelationResult.Failure(
                TenantCorrelationViolationType.SagaTenantNotSet,
                "Saga tenant context is not set",
                null,
                saga.TenantId);
        }

        return TenantSagaCorrelationResult.Success();
    }

    /// <inheritdoc />
    public TenantSagaRoutingMetadata GetRoutingMetadata<TMessage>(ConsumeContext<TMessage> message)
        where TMessage : class
    {
        var tenantId = ExtractTenantId(message);
        var warnings = new List<string>();

        if (string.IsNullOrEmpty(tenantId))
        {
            warnings.Add("Message does not contain tenant identifier in headers");
        }

        return new TenantSagaRoutingMetadata
        {
            TenantId = tenantId ?? string.Empty,
            CorrelationId = message.CorrelationId ?? Guid.Empty,
            IsSafe = !string.IsNullOrEmpty(tenantId),
            Warnings = warnings
        };
    }

    private static string? ExtractTenantId<TMessage>(ConsumeContext<TMessage> message) where TMessage : class
    {
        foreach (var headerKey in _tenantHeaderKeys)
        {
            if (message.Headers.TryGetHeader(headerKey, out var value) && value is string tenantId)
            {
                return tenantId;
            }
        }

        if (message.Message is ITenantScoped scoped && !string.IsNullOrEmpty(scoped.TenantId))
        {
            return scoped.TenantId;
        }

        return null;
    }
}

/// <summary>
///     Interface for tenant-scoped messages.
/// </summary>
public interface ITenantScoped
{
    /// <summary>
    ///     Gets the tenant identifier for this message.
    /// </summary>
    string TenantId { get; }
}
