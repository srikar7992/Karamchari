// -----------------------------------------------------------------------
// <copyright file="WorkflowContracts.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Core.Domain.Workflows;

/// <summary>
/// Canonical record for a workflow state transition.
/// </summary>
public sealed record WorkflowTransition(
    string FromState,
    string ToState,
    Guid ActorId,
    DateTimeOffset Timestamp,
    string? Reason = null,
    string? MetadataJson = null);

/// <summary>
/// Canonical record for an approval decision.
/// </summary>
public sealed record ApprovalDecision(
    Guid StepId,
    Guid ApproverId,
    bool Approved,
    string? Note = null,
    DateTimeOffset DecidedAt = default);

/// <summary>
/// Canonical record for a workflow audit entry.
/// Matches Step 4 requirements.
/// </summary>
public sealed record WorkflowAuditEntry(
    string TenantId,
    string CorrelationId,
    Guid WorkflowInstanceId,
    string Action,
    Guid ActorId,
    DateTimeOffset Timestamp,
    string FromState,
    string ToState,
    string? Notes = null,
    string? StateSnapshotJson = null);
