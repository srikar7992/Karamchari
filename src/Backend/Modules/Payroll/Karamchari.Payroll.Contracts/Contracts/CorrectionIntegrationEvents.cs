namespace Karamchari.Payroll.Contracts;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record CorrectionApprovedIntegrationEvent
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid CorrectionId { get; init; }
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
    public string CorrectionType { get; init; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string AffectedPeriodName { get; init; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string CorrectionScope { get; init; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset OccurredOnUtc { get; init; }
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record CorrectionProcessedIntegrationEvent
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid CorrectionId { get; init; }
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
    public decimal DifferentialAmount { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset OccurredOnUtc { get; init; }
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record TriggerCorrectionRecalculationCommand
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid CorrectionId { get; init; }
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
    public string AffectedPeriodName { get; init; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string CorrectionScope { get; init; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string ChangeDetailsJson { get; init; } = string.Empty;
}
