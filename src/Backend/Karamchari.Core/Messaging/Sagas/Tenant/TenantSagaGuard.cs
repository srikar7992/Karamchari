using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Karamchari.Core.Messaging.Sagas.Tenant;

/// <summary>
///     Guard class for saga tenant safety, providing validation methods
///     to prevent cross-tenant correlation and ensure saga integrity.
/// </summary>
public sealed class TenantSagaGuard
{
    private static readonly ILogger _logger = NullLogger.Instance;

    /// <summary>
    ///     Validates that the message tenant matches the saga tenant.
    /// </summary>
    /// <param name="messageTenantId">The tenant identifier from the incoming message.</param>
    /// <param name="sagaTenantId">The tenant identifier of the saga instance.</param>
    /// <exception cref="TenantSagaCorrelationException">
    ///     Thrown when tenant IDs do not match.
    /// </exception>
    public static void ValidateTenantOwnership(string messageTenantId, string sagaTenantId)
    {
        if (string.IsNullOrEmpty(messageTenantId))
        {
            _logger.LogWarning(
                "TenantSagaGuard: Message tenant ID is null or empty. Cross-tenant attack suspected.");
            throw new TenantSagaCorrelationException(
                "Message tenant ID is null or empty",
                messageTenantId,
                sagaTenantId,
                null,
                null,
                TenantCorrelationViolationType.MissingTenantId);
        }

        if (string.IsNullOrEmpty(sagaTenantId))
        {
            _logger.LogWarning(
                "TenantSagaGuard: Saga tenant ID is null or empty. Invalid saga state.");
            throw new TenantSagaCorrelationException(
                "Saga tenant ID is null or empty",
                messageTenantId,
                sagaTenantId,
                null,
                null,
                TenantCorrelationViolationType.SagaTenantNotSet);
        }

        if (!string.Equals(messageTenantId, sagaTenantId, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError(
                "TenantSagaGuard: Cross-tenant correlation attempt detected. " +
                "Message tenant: {MessageTenantId}, Saga tenant: {SagaTenantId}",
                messageTenantId,
                sagaTenantId);
            throw new TenantSagaCorrelationException(
                string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "Cross-tenant correlation attempt detected. Message tenant '{0}' does not match saga tenant '{1}'",
                    messageTenantId,
                    sagaTenantId),
                messageTenantId,
                sagaTenantId,
                null,
                null,
                TenantCorrelationViolationType.CrossTenantAttempt);
        }
    }

    /// <summary>
    ///     Validates that the message correlation ID matches the saga correlation ID.
    /// </summary>
    /// <param name="messageCorrelationId">The correlation identifier from the incoming message.</param>
    /// <param name="sagaCorrelationId">The correlation identifier of the saga instance.</param>
    /// <exception cref="TenantSagaCorrelationException">
    ///     Thrown when correlation IDs do not match.
    /// </exception>
    public static void ValidateCorrelation(string messageCorrelationId, string sagaCorrelationId)
    {
        if (string.IsNullOrEmpty(messageCorrelationId))
        {
            _logger.LogWarning(
                "TenantSagaGuard: Message correlation ID is null or empty.");
            throw new TenantSagaCorrelationException(
                "Message correlation ID is null or empty",
                null,
                null,
                null,
                null,
                TenantCorrelationViolationType.CorrelationIdMismatch);
        }

        if (!string.Equals(messageCorrelationId, sagaCorrelationId, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError(
                "TenantSagaGuard: Correlation ID mismatch. Message: {MessageCorrelationId}, Saga: {SagaCorrelationId}",
                messageCorrelationId,
                sagaCorrelationId);
            throw new TenantSagaCorrelationException(
                string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "Correlation ID mismatch. Message correlation '{0}' does not match saga correlation '{1}'",
                    messageCorrelationId,
                    sagaCorrelationId),
                null,
                null,
                Guid.TryParse(messageCorrelationId, out var msgId) ? msgId : null,
                Guid.TryParse(sagaCorrelationId, out var sagaId) ? sagaId : null,
                TenantCorrelationViolationType.CorrelationIdMismatch);
        }
    }

    /// <summary>
    ///     Determines whether a message represents a cross-tenant correlation attempt.
    /// </summary>
    /// <param name="messageTenantId">The tenant identifier from the incoming message.</param>
    /// <param name="sagaTenantId">The tenant identifier of the saga instance.</param>
    /// <returns>
    ///     <c>true</c> if the message tenant differs from the saga tenant; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsCrossTenantAttempt(string messageTenantId, string sagaTenantId)
    {
        if (string.IsNullOrEmpty(messageTenantId) || string.IsNullOrEmpty(sagaTenantId))
        {
            return true;
        }

        return !string.Equals(messageTenantId, sagaTenantId, StringComparison.OrdinalIgnoreCase);
    }
}
