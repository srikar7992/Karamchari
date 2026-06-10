// -----------------------------------------------------------------------
// <copyright file="ProjectionDeadLetter.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Core.Projections;

/// <summary>
/// States for a projection dead-letter.
/// </summary>
public enum DeadLetterStatus
{
    /// <summary>Pending resolution triage</summary>
    Pending,
    /// <summary>Resolved by operational retry/fix</summary>
    Resolved,
    /// <summary>Explicitly ignored</summary>
    Ignored
}

/// <summary>
/// Logs failed projection updates, facilitating offline retry execution and diagnostics.
/// </summary>
public sealed class ProjectionDeadLetter : AggregateRoot<Guid>, ITenantOwned
{
    private ProjectionDeadLetter()
    {
        TenantId = string.Empty;
        ProjectionName = string.Empty;
        ExceptionMessage = string.Empty;
        EventType = string.Empty;
        EventPayloadJson = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionDeadLetter"/> class.
    /// </summary>
    public ProjectionDeadLetter(
        Guid id,
        string tenantId,
        Guid eventId,
        string eventType,
        string eventPayloadJson,
        string projectionName,
        string exceptionMessage) : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectionName);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);

        TenantId = tenantId;
        EventId = eventId;
        EventType = eventType;
        EventPayloadJson = eventPayloadJson ?? string.Empty;
        ProjectionName = projectionName;
        ExceptionMessage = exceptionMessage ?? string.Empty;
        FailedUtc = DateTimeOffset.UtcNow;
        RetryCount = 0;
        Status = DeadLetterStatus.Pending;
    }

    /// <inheritdoc />
    public string TenantId { get; private set; }

    /// <summary>
    /// Gets the event identifier that failed to process.
    /// </summary>
    public Guid EventId { get; private set; }

    /// <summary>
    /// Gets the type name of the failed event.
    /// </summary>
    public string EventType { get; private set; }

    /// <summary>
    /// Gets the event payload JSON captured at the time of failure.
    /// </summary>
    public string EventPayloadJson { get; private set; }

    /// <summary>
    /// Gets the projection registration name.
    /// </summary>
    public string ProjectionName { get; private set; }

    /// <summary>
    /// Gets the detailed error message exception trace.
    /// </summary>
    public string ExceptionMessage { get; private set; }

    /// <summary>
    /// Gets the timestamp when error occurred.
    /// </summary>
    public DateTimeOffset FailedUtc { get; private set; }

    /// <summary>
    /// Gets the number of reprocessing attempts.
    /// </summary>
    public int RetryCount { get; private set; }

    /// <summary>
    /// Gets the current dead-letter triage status.
    /// </summary>
    public DeadLetterStatus Status { get; private set; }

    /// <summary>
    /// Gets the timestamp when resolved.
    /// </summary>
    public DateTimeOffset? ResolvedUtc { get; private set; }

    /// <summary>
    /// Gets the operator identifier who resolved this failure.
    /// </summary>
    public string? ResolvedBy { get; private set; }

    /// <summary>
    /// Increments retry count and sets timestamp.
    /// </summary>
    public void IncrementRetry()
    {
        RetryCount++;
        FailedUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Marks the dead letter as resolved.
    /// </summary>
    public void Resolve(string resolvedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resolvedBy);

        Status = DeadLetterStatus.Resolved;
        ResolvedUtc = DateTimeOffset.UtcNow;
        ResolvedBy = resolvedBy;
    }

    /// <summary>
    /// Marks the dead letter as ignored.
    /// </summary>
    public void Ignore(string resolvedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resolvedBy);

        Status = DeadLetterStatus.Ignored;
        ResolvedUtc = DateTimeOffset.UtcNow;
        ResolvedBy = resolvedBy;
    }
}
