using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.Payroll.Domain.Adjustments.Events;

namespace Karamchari.Payroll.Domain.Adjustments;

/// <summary>
/// Retroactive payroll adjustment. Represents the difference between what was paid
/// in a closed payroll run and what should have been paid.
/// Never reopens the source payroll — applied in the next open run.
/// Immutable once applied.
/// </summary>
public sealed class RetroPayrollAdjustment : AggregateRoot<Guid>, ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public Guid EmployeeId { get; private set; }
    public Guid SourcePayrollRunId { get; private set; }
    public decimal AmountPaid { get; private set; }
    public decimal AmountShouldBePaid { get; private set; }
    public decimal Difference { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public AdjustmentStatus Status { get; private set; }
    public Guid? AppliedInRunId { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? AppliedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    private RetroPayrollAdjustment() { }

    public static RetroPayrollAdjustment Create(
        string tenantId,
        Guid employeeId,
        Guid sourcePayrollRunId,
        decimal amountPaid,
        decimal amountShouldBePaid,
        string reason,
        string createdBy)
    {
        var difference = amountShouldBePaid - amountPaid;

        var adjustment = new RetroPayrollAdjustment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            SourcePayrollRunId = sourcePayrollRunId,
            AmountPaid = amountPaid,
            AmountShouldBePaid = amountShouldBePaid,
            Difference = difference,
            Reason = reason,
            Status = AdjustmentStatus.Pending,
            CreatedBy = createdBy,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        adjustment.RaiseDomainEvent(new RetroAdjustmentCreatedEvent(adjustment.Id, tenantId, employeeId, sourcePayrollRunId, difference));
        return adjustment;
    }

    public void Apply(Guid appliedInRunId)
    {
        if (Status != AdjustmentStatus.Pending)
            throw new InvalidOperationException($"Cannot apply retro adjustment in status {Status}.");

        AppliedInRunId = appliedInRunId;
        Status = AdjustmentStatus.Applied;
        AppliedAtUtc = DateTimeOffset.UtcNow;
    }
}
