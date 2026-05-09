using MassTransit;

namespace Karamchari.Payroll.StateMachines;

public sealed class DisbursementBatchState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = string.Empty;

    public Guid TenantId { get; set; }
    public Guid RunId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public string BankProvider { get; set; } = string.Empty;
    public string InitiatedBy { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }
    public int TotalEntries { get; set; }
    public int RetryCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }

    public string? BankAcknowledgementId { get; set; }
    public string? FailureReason { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
