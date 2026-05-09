using MassTransit;

namespace Karamchari.Payroll.StateMachines;

public sealed class PayrollCorrectionState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = string.Empty;

    public string TenantId { get; set; } = string.Empty;
    public Guid EmployeeId { get; set; }
    public string CorrectionType { get; set; } = string.Empty;
    public string CorrectionScope { get; set; } = string.Empty;
    public string AffectedPeriodName { get; set; } = string.Empty;
    public string ChangeDetailsJson { get; set; } = string.Empty;

    public decimal? DifferentialAmount { get; set; }
    public string? LinkedArrearId { get; set; }
    public string? ApprovedBy { get; set; }

    // Idempotency guard for recalculation
    public bool RecalculationTriggered { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
