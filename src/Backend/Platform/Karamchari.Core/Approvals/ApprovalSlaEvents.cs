using System;

namespace Karamchari.Core.Approvals;

/// <summary>
/// Integration event emitted when an approval request is due soon.
/// </summary>
public sealed record ApprovalDueSoon(
    Guid ApprovalInstanceId,
    string TenantId,
    Guid ResourceId,
    Guid ApproverRoleId,
    DateTimeOffset DueUtc,
    DateTimeOffset ThresholdUtc);

/// <summary>
/// Integration event emitted when an approval request becomes overdue.
/// </summary>
public sealed record ApprovalOverdue(
    Guid ApprovalInstanceId,
    string TenantId,
    Guid ResourceId,
    Guid ApproverRoleId,
    DateTimeOffset DueUtc);

/// <summary>
/// Integration event emitted when an approval request is escalated due to SLA breach.
/// </summary>
public sealed record ApprovalEscalated(
    Guid ApprovalInstanceId,
    string TenantId,
    Guid ResourceId,
    Guid OriginalApproverRoleId,
    Guid EscalatedToRoleId,
    string Reason);
