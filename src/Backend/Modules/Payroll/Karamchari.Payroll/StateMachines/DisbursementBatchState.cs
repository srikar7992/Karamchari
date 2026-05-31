using MassTransit;

namespace Karamchari.Payroll.StateMachines;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed class DisbursementBatchState : SagaStateMachineInstance
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
    public Guid RunId { get; set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string PeriodName { get; set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string BankProvider { get; set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string InitiatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal TotalAmount { get; set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int TotalEntries { get; set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int RetryCount { get; set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int SuccessCount { get; set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int FailedCount { get; set; }

    public string? BankAcknowledgementId { get; set; }
    public string? FailureReason { get; set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public byte[] RowVersion { get; set; } = [];
}
