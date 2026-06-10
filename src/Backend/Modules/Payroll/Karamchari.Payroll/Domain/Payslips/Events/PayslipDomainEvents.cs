// -----------------------------------------------------------------------
// <copyright file="PayslipDomainEvents.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Payroll.Domain.Payslips.Events;

public sealed record PayslipGeneratedEvent(Guid PayslipId, string TenantId, Guid EmployeeId, Guid PayrollRunId, decimal NetPay) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
