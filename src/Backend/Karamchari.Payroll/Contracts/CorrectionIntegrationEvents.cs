namespace Karamchari.Payroll.Contracts;

public sealed record CorrectionApprovedIntegrationEvent
{
    public Guid CorrectionId { get; init; }
    public Guid TenantId { get; init; }
    public Guid EmployeeId { get; init; }
    public string CorrectionType { get; init; } = string.Empty;
    public string AffectedPeriodName { get; init; } = string.Empty;
    public string CorrectionScope { get; init; } = string.Empty;
    public DateTimeOffset OccurredOnUtc { get; init; }
}

public sealed record CorrectionProcessedIntegrationEvent
{
    public Guid CorrectionId { get; init; }
    public Guid TenantId { get; init; }
    public Guid EmployeeId { get; init; }
    public decimal DifferentialAmount { get; init; }
    public DateTimeOffset OccurredOnUtc { get; init; }
}

public sealed record TriggerCorrectionRecalculationCommand
{
    public Guid CorrectionId { get; init; }
    public Guid TenantId { get; init; }
    public Guid EmployeeId { get; init; }
    public string AffectedPeriodName { get; init; } = string.Empty;
    public string CorrectionScope { get; init; } = string.Empty;
    public string ChangeDetailsJson { get; init; } = string.Empty;
}
