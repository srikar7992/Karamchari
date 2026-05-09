namespace Karamchari.Payroll.Contracts;

public sealed record ArrearCalculationApprovedIntegrationEvent
{
    public Guid ArrearId { get; init; }
    public Guid TenantId { get; init; }
    public Guid EmployeeId { get; init; }
    public decimal TotalNetDelta { get; init; }
    public string TriggerType { get; init; } = string.Empty;
    public DateTimeOffset OccurredOnUtc { get; init; }
}

public sealed record ArrearProcessedIntegrationEvent
{
    public Guid ArrearId { get; init; }
    public Guid TenantId { get; init; }
    public Guid EmployeeId { get; init; }
    public decimal TotalNetDelta { get; init; }
    public string PayoutPeriodName { get; init; } = string.Empty;
    public DateTimeOffset OccurredOnUtc { get; init; }
}

public sealed record TriggerArrearCalculationCommand
{
    public Guid TenantId { get; init; }
    public Guid EmployeeId { get; init; }
    public string TriggerType { get; init; } = string.Empty;
    public string TriggerReference { get; init; } = string.Empty;
    public DateOnly EffectiveFrom { get; init; }
    public DateOnly EffectiveTo { get; init; }
    public string InitiatedBy { get; init; } = string.Empty;
}
