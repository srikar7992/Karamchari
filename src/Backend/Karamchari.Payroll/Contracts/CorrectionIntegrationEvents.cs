namespace Karamchari.Payroll.Contracts;

public sealed record CorrectionApprovedIntegrationEvent
{
    public Guid CorrectionId { get; init; }
    public string TenantId { get; init; } = string.Empty;
    public Guid EmployeeId { get; init; }
    public string CorrectionType { get; init; } = string.Empty;
    public string AffectedPeriodName { get; init; } = string.Empty;
    public string CorrectionScope { get; init; } = string.Empty;
    public DateTimeOffset OccurredOnUtc { get; init; }
}

public sealed record CorrectionProcessedIntegrationEvent
{
    public Guid CorrectionId { get; init; }
    public string TenantId { get; init; } = string.Empty;
    public Guid EmployeeId { get; init; }
    public decimal DifferentialAmount { get; init; }
    public DateTimeOffset OccurredOnUtc { get; init; }
}

public sealed record TriggerCorrectionRecalculationCommand
{
    public Guid CorrectionId { get; init; }
    public string TenantId { get; init; } = string.Empty;
    public Guid EmployeeId { get; init; }
    public string AffectedPeriodName { get; init; } = string.Empty;
    public string CorrectionScope { get; init; } = string.Empty;
    public string ChangeDetailsJson { get; init; } = string.Empty;
}
