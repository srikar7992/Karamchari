namespace Karamchari.Payroll.Contracts;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record ReimbursementApprovedIntegrationEvent
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid ClaimId { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; init; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid EmployeeId { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal ApprovedAmount { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Category { get; init; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset OccurredOnUtc { get; init; }
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record ReimbursementPaidOutIntegrationEvent
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid ClaimId { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; init; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid EmployeeId { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal Amount { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string PayoutPeriodName { get; init; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset OccurredOnUtc { get; init; }
}
