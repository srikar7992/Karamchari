// -----------------------------------------------------------------------
// <copyright file="VariablePayDomainEvents.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Payroll.Domain.VariablePay.Events;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record VariablePayApprovedEvent(
    Guid AllocationId,
    string TenantId,
    Guid EmployeeId,
    VariablePayType Type,
    decimal Amount,
    string ApprovedBy) : IDomainEvent
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid EventId { get; } = Guid.NewGuid();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record VariablePayPaidOutEvent(
    Guid AllocationId,
    string TenantId,
    Guid EmployeeId,
    VariablePayType Type,
    decimal PaidAmount) : IDomainEvent
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid EventId { get; } = Guid.NewGuid();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
