namespace Karamchari.Payroll.Contracts;

public sealed record VariablePayApprovedIntegrationEvent
{
    public Guid AllocationId { get; init; }
    public Guid TenantId { get; init; }
    public Guid EmployeeId { get; init; }
    public string VariablePayType { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string? PayoutPeriodName { get; init; }
    public DateTimeOffset OccurredOnUtc { get; init; }
}

public sealed record VariablePayPaidOutIntegrationEvent
{
    public Guid AllocationId { get; init; }
    public Guid TenantId { get; init; }
    public Guid EmployeeId { get; init; }
    public decimal PaidAmount { get; init; }
    public DateTimeOffset OccurredOnUtc { get; init; }
}

public sealed record SalaryRevisionApprovedIntegrationEvent
{
    public Guid RevisionId { get; init; }
    public Guid TenantId { get; init; }
    public Guid EmployeeId { get; init; }
    public decimal PreviousCTC { get; init; }
    public decimal NewCTC { get; init; }
    public DateOnly EffectiveFrom { get; init; }
    public bool RequiresArrears { get; init; }
    public DateTimeOffset OccurredOnUtc { get; init; }
}
