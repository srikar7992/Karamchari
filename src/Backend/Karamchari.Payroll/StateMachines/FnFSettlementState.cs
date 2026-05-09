using MassTransit;

namespace Karamchari.Payroll.StateMachines;

public sealed class FnFSettlementState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = string.Empty;

    public string TenantId { get; set; } = string.Empty;
    public Guid EmployeeId { get; set; }
    public string ExitType { get; set; } = string.Empty;
    public DateOnly LastWorkingDay { get; set; }
    public string InitiatedBy { get; set; } = string.Empty;

    public decimal NetSettlementAmount { get; set; }
    public int ApprovalAttempts { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTimeOffset? ApprovedAtUtc { get; set; }

    public int DisbursementAttempts { get; set; }
    public string? DisbursementBatchId { get; set; }

    public bool IsOnHold { get; set; }

    // EF Core optimistic concurrency
    public byte[] RowVersion { get; set; } = [];
}
