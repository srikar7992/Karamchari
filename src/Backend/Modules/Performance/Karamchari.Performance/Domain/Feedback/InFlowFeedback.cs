using System;
using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Performance.Domain.Feedback;

/// <summary>
/// Domain aggregate root for feedback logged passively through Slack/Teams chat channels.
/// </summary>
public sealed class InFlowFeedback : AggregateRoot<Guid>, ITenantOwned
{
    private InFlowFeedback()
    {
        TenantId = string.Empty;
        SourceChannel = string.Empty;
        Content = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InFlowFeedback"/> class.
    /// </summary>
    public InFlowFeedback(
        Guid id,
        string tenantId,
        Guid senderEmployeeId,
        Guid receiverEmployeeId,
        string sourceChannel,
        string content) : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceChannel);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        TenantId = tenantId;
        SenderEmployeeId = senderEmployeeId;
        ReceiverEmployeeId = receiverEmployeeId;
        SourceChannel = sourceChannel;
        Content = content;
        Timestamp = DateTimeOffset.UtcNow;
    }

    /// <inheritdoc />
    public string TenantId { get; private set; }

    /// <summary>
    /// Gets the employee ID who initiated the feedback.
    /// </summary>
    public Guid SenderEmployeeId { get; private set; }

    /// <summary>
    /// Gets the employee ID receiving the feedback.
    /// </summary>
    public Guid ReceiverEmployeeId { get; private set; }

    /// <summary>
    /// Gets the ingestion channel name (e.g. Slack, Teams).
    /// </summary>
    public string SourceChannel { get; private set; }

    /// <summary>
    /// Gets the body content text.
    /// </summary>
    public string Content { get; private set; }

    /// <summary>
    /// Gets the record timestamp.
    /// </summary>
    public DateTimeOffset Timestamp { get; private set; }
}
