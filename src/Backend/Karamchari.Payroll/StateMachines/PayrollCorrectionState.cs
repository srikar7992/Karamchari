using MassTransit;

namespace Karamchari.Payroll.StateMachines;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed class PayrollCorrectionState : SagaStateMachineInstance
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid CorrelationId { get; set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string CurrentState { get; set; } = string.Empty;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid EmployeeId { get; set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string CorrectionType { get; set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string CorrectionScope { get; set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string AffectedPeriodName { get; set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string ChangeDetailsJson { get; set; } = string.Empty;

    public decimal? DifferentialAmount { get; set; }
    public string? LinkedArrearId { get; set; }
    public string? ApprovedBy { get; set; }

    // Idempotency guard for recalculation
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool RecalculationTriggered { get; set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public byte[] RowVersion { get; set; } = [];
}
