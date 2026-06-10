// -----------------------------------------------------------------------
// <copyright file="SagaStateBase.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;
using MassTransit;

namespace Karamchari.Core.Messaging.Sagas;

/// <summary>
/// Base class for all Long-Running Process (Saga) states.
/// Enforces MassTransit correlation and optimistic concurrency via RowVersion.
/// </summary>
public abstract class SagaStateBase : SagaStateMachineInstance
{
    /// <summary>Global correlation ID linking all events in this workflow.</summary>
    [Key]
    public Guid CorrelationId { get; set; }

    /// <summary>The current state of the state machine.</summary>
    public string CurrentState { get; set; } = string.Empty;

    /// <summary>The tenant owning this workflow.</summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>Timestamp when the saga started.</summary>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Timestamp of the last state transition.</summary>
    public DateTimeOffset? UpdatedAtUtc { get; set; }

    /// <summary>
    /// Optimistic concurrency token. Essential for distributed orchestration
    /// where multiple events might arrive simultaneously.
    /// </summary>
    public byte[] RowVersion { get; set; } = [];
}
