using Karamchari.Core.Domain.Primitives;
using Karamchari.FinancialOps.Contracts.Primitives;

namespace Karamchari.FinancialOps.Domain.Ledger;

/// <summary>
/// Represents an immutable operational event record for a financial operation.
/// Used for internal event sourcing of state transitions and auditability.
/// </summary>
public sealed class FinancialOperationEvent : Entity<Guid>
{
    private FinancialOperationEvent(
        Guid id,
        string tenantId,
        Guid aggregateId,
        string aggregateType,
        string eventName,
        string fromState,
        string toState,
        string reason,
        string triggeredBy,
        FinancialCorrelationChain correlationChain,
        string? metadataJson)
        : base(id)
    {
        TenantId = tenantId;
        AggregateId = aggregateId;
        AggregateType = aggregateType;
        EventName = eventName;
        FromState = fromState;
        ToState = toState;
        Reason = reason;
        TriggeredBy = triggeredBy;
        CorrelationChain = correlationChain;
        MetadataJson = metadataJson;
        TimestampUtc = DateTimeOffset.UtcNow;
    }

    private FinancialOperationEvent()
    {
        // EF Core
        TenantId = string.Empty;
        AggregateType = string.Empty;
        EventName = string.Empty;
        FromState = string.Empty;
        ToState = string.Empty;
        Reason = string.Empty;
        TriggeredBy = string.Empty;
        CorrelationChain = null!;
    }

    /// <summary>Gets the tenant identifier.</summary>
    public string TenantId { get; private set; }

    /// <summary>Gets the identifier of the aggregate (e.g. ExpenseClaimId).</summary>
    public Guid AggregateId { get; private set; }

    /// <summary>Gets the type name of the aggregate.</summary>
    public string AggregateType { get; private set; }

    /// <summary>Gets the name of the event.</summary>
    public string EventName { get; private set; }

    /// <summary>Gets the state before the transition.</summary>
    public string FromState { get; private set; }

    /// <summary>Gets the state after the transition.</summary>
    public string ToState { get; private set; }

    /// <summary>Gets the human-readable reason for the transition.</summary>
    public string Reason { get; private set; }

    /// <summary>Gets who triggered the transition.</summary>
    public string TriggeredBy { get; private set; }

    /// <summary>Gets the correlation chain.</summary>
    public FinancialCorrelationChain CorrelationChain { get; private set; }

    /// <summary>Gets optional JSON metadata.</summary>
    public string? MetadataJson { get; private set; }

    /// <summary>Gets the timestamp.</summary>
    public DateTimeOffset TimestampUtc { get; private set; }

    /// <summary>Creates a new transition event.</summary>
    public static FinancialOperationEvent RecordTransition(
        string tenantId,
        Guid aggregateId,
        string aggregateType,
        string eventName,
        string fromState,
        string toState,
        string reason,
        string triggeredBy,
        FinancialCorrelationChain correlationChain,
        string? metadataJson = null)
    {
        return new FinancialOperationEvent(
            Guid.NewGuid(),
            tenantId,
            aggregateId,
            aggregateType,
            eventName,
            fromState,
            toState,
            reason,
            triggeredBy,
            correlationChain,
            metadataJson);
    }
}
