using System.Globalization;

namespace Karamchari.Core.Messaging.Sagas.Tenant;

/// <summary>
///     Custom exception thrown when tenant correlation validation fails.
/// </summary>
[Serializable]
public sealed class TenantSagaCorrelationException : Exception
{
    /// <summary>
    ///     Gets the tenant identifier from the incoming message.
    /// </summary>
    public string? MessageTenantId { get; }

    /// <summary>
    ///     Gets the tenant identifier of the saga instance.
    /// </summary>
    public string? SagaTenantId { get; }

    /// <summary>
    ///     Gets the correlation identifier from the message.
    /// </summary>
    public Guid? MessageCorrelationId { get; }

    /// <summary>
    ///     Gets the correlation identifier of the saga instance.
    /// </summary>
    public Guid? SagaCorrelationId { get; }

    /// <summary>
    ///     Gets the type of correlation violation that occurred.
    /// </summary>
    public TenantCorrelationViolationType ViolationType { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="TenantSagaCorrelationException"/> class.
    /// </summary>
    public TenantSagaCorrelationException()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="TenantSagaCorrelationException"/> class with a message.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public TenantSagaCorrelationException(string message)
        : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="TenantSagaCorrelationException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public TenantSagaCorrelationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="TenantSagaCorrelationException"/> class with full context.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="messageTenantId">The tenant ID from the message.</param>
    /// <param name="sagaTenantId">The tenant ID of the saga.</param>
    /// <param name="messageCorrelationId">The correlation ID from the message.</param>
    /// <param name="sagaCorrelationId">The correlation ID of the saga.</param>
    /// <param name="violationType">The type of violation.</param>
    public TenantSagaCorrelationException(
        string message,
        string? messageTenantId,
        string? sagaTenantId,
        Guid? messageCorrelationId,
        Guid? sagaCorrelationId,
        TenantCorrelationViolationType violationType)
        : base(message)
    {
        MessageTenantId = messageTenantId;
        SagaTenantId = sagaTenantId;
        MessageCorrelationId = messageCorrelationId;
        SagaCorrelationId = sagaCorrelationId;
        ViolationType = violationType;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var parts = new List<string> { base.ToString() };

        if (ViolationType != TenantCorrelationViolationType.None)
        {
            parts.Add(string.Format(CultureInfo.InvariantCulture, "ViolationType: {0}", ViolationType));
        }

        if (!string.IsNullOrEmpty(MessageTenantId))
        {
            parts.Add(string.Format(CultureInfo.InvariantCulture, "MessageTenantId: {0}", MessageTenantId));
        }

        if (!string.IsNullOrEmpty(SagaTenantId))
        {
            parts.Add(string.Format(CultureInfo.InvariantCulture, "SagaTenantId: {0}", SagaTenantId));
        }

        if (MessageCorrelationId.HasValue)
        {
            parts.Add(string.Format(CultureInfo.InvariantCulture, "MessageCorrelationId: {0}", MessageCorrelationId));
        }

        if (SagaCorrelationId.HasValue)
        {
            parts.Add(string.Format(CultureInfo.InvariantCulture, "SagaCorrelationId: {0}", SagaCorrelationId));
        }

        return string.Join(Environment.NewLine, parts);
    }
}

/// <summary>
///     Types of tenant correlation violations.
/// </summary>
public enum TenantCorrelationViolationType
{
    /// <summary>
    ///     No violation detected.
    /// </summary>
    None,

    /// <summary>
    ///     Cross-tenant correlation attempt detected.
    /// </summary>
    CrossTenantAttempt,

    /// <summary>
    ///     Correlation ID mismatch detected.
    /// </summary>
    CorrelationIdMismatch,

    /// <summary>
    ///     Missing tenant identifier in message.
    /// </summary>
    MissingTenantId,

    /// <summary>
    ///     Saga tenant not set.
    /// </summary>
    SagaTenantNotSet
}
