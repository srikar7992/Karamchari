using MassTransit;

namespace Karamchari.Payroll.StateMachines;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed class FnFSettlementState : SagaStateMachineInstance
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
    public string ExitType { get; set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateOnly LastWorkingDay { get; set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string InitiatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal NetSettlementAmount { get; set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int ApprovalAttempts { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTimeOffset? ApprovedAtUtc { get; set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int DisbursementAttempts { get; set; }
    public string? DisbursementBatchId { get; set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool IsOnHold { get; set; }

    // EF Core optimistic concurrency
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public byte[] RowVersion { get; set; } = [];
}
