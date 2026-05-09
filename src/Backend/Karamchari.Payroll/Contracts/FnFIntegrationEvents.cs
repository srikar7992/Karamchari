namespace Karamchari.Payroll.Contracts;

// Integration events published to Azure Service Bus for cross-module consumption

public sealed record FnFSettlementInitiatedIntegrationEvent
{
    public Guid SettlementId { get; init; }
    public string TenantId { get; init; } = string.Empty;
    public Guid EmployeeId { get; init; }
    public string ExitType { get; init; } = string.Empty;
    public DateOnly LastWorkingDay { get; init; }
    public DateTimeOffset OccurredOnUtc { get; init; }
}

public sealed record FnFSettlementApprovedIntegrationEvent
{
    public Guid SettlementId { get; init; }
    public string TenantId { get; init; } = string.Empty;
    public Guid EmployeeId { get; init; }
    public decimal NetSettlementAmount { get; init; }
    public string ApprovedBy { get; init; } = string.Empty;
    public DateTimeOffset OccurredOnUtc { get; init; }
}

public sealed record FnFSettlementDisbursedIntegrationEvent
{
    public Guid SettlementId { get; init; }
    public string TenantId { get; init; } = string.Empty;
    public Guid EmployeeId { get; init; }
    public decimal Amount { get; init; }
    public DateTimeOffset OccurredOnUtc { get; init; }
}

// Commands published internally
public sealed record InitiateFnFCalculationCommand
{
    public Guid SettlementId { get; init; }
    public string TenantId { get; init; } = string.Empty;
    public Guid EmployeeId { get; init; }
}

public sealed record DisburseFnFCommand
{
    public Guid SettlementId { get; init; }
    public string TenantId { get; init; } = string.Empty;
}
