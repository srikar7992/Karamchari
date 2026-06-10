// -----------------------------------------------------------------------
// <copyright file="EmployeePayrollResultDomainEvents.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Payroll.Domain.Results.Events;

public sealed record EmployeePayrollCalculatedEvent(
    Guid ResultId,
    string TenantId,
    Guid PayrollRunId,
    Guid EmployeeId,
    decimal GrossPay,
    decimal NetPay) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record EmployeePayrollApprovedEvent(
    Guid ResultId,
    string TenantId,
    Guid PayrollRunId,
    Guid EmployeeId,
    string ApprovedBy) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record EmployeePayrollLockedEvent(
    Guid ResultId,
    string TenantId,
    Guid PayrollRunId,
    Guid EmployeeId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
