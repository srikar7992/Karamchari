using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Payroll.Domain.Adjustments.Events;

public sealed record PayrollAdjustmentCreatedEvent(Guid AdjustmentId, string TenantId, Guid EmployeeId, decimal Amount, AdjustmentType Type) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record PayrollAdjustmentAppliedEvent(Guid AdjustmentId, string TenantId, Guid EmployeeId, Guid AppliedInRunId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}

public sealed record RetroAdjustmentCreatedEvent(Guid AdjustmentId, string TenantId, Guid EmployeeId, Guid SourcePayrollRunId, decimal Difference) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
