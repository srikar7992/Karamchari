// -----------------------------------------------------------------------
// <copyright file="ScheduledDomainAction.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Domain.Scheduling;

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

/// <summary>
/// Unified record of a domain action scheduled for future execution.
/// Replaces ad-hoc BackgroundService proliferation.
///
/// Action types: AccrualRun, CompOffExpiry, CarryForwardRun, DelegationExpiry, PolicyActivation.
/// Executor polls this table daily and dispatches actions by type to registered handlers.
/// </summary>
public sealed class ScheduledDomainAction : Entity<Guid>, ITenantOwned
{
    private ScheduledDomainAction(Guid id, string actionType, string payload,
        DateOnly scheduledFor) : base(id)
    {
        ActionType = actionType;
        Payload = payload;
        ScheduledFor = scheduledFor;
        Status = ScheduledActionStatus.Pending;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    private ScheduledDomainAction()
    {
        TenantId = string.Empty;
        ActionType = string.Empty;
        Payload = string.Empty;
    }

    public string TenantId { get; private set; } = string.Empty;

    /// <summary>"AccrualRun" | "CompOffExpiry" | "CarryForwardRun" | "DelegationExpiry" | "PolicyActivation"</summary>
    public string ActionType { get; private set; }

    /// <summary>JSON payload specific to the action type.</summary>
    public string Payload { get; private set; }

    public DateOnly ScheduledFor { get; private set; }
    public ScheduledActionStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? ExecutedAtUtc { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int RetryCount { get; private set; }

    public static ScheduledDomainAction Create(string actionType, string payload, DateOnly scheduledFor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actionType);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
        return new ScheduledDomainAction(Guid.NewGuid(), actionType, payload, scheduledFor);
    }

    public void MarkExecuted()
    {
        Status = ScheduledActionStatus.Executed;
        ExecutedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkFailed(string error)
    {
        ErrorMessage = error;
        RetryCount++;
        Status = RetryCount >= 3
            ? ScheduledActionStatus.DeadLettered
            : ScheduledActionStatus.Pending;
    }

    public void Cancel() => Status = ScheduledActionStatus.Cancelled;
}

public enum ScheduledActionStatus
{
    Pending = 1,
    Executing = 2,
    Executed = 3,
    DeadLettered = 4,
    Cancelled = 5
}
