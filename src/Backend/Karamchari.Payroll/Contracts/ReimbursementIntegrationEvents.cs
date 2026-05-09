namespace Karamchari.Payroll.Contracts;

public sealed record ReimbursementApprovedIntegrationEvent
{
    public Guid ClaimId { get; init; }
    public Guid TenantId { get; init; }
    public Guid EmployeeId { get; init; }
    public decimal ApprovedAmount { get; init; }
    public string Category { get; init; } = string.Empty;
    public DateTimeOffset OccurredOnUtc { get; init; }
}

public sealed record ReimbursementPaidOutIntegrationEvent
{
    public Guid ClaimId { get; init; }
    public Guid TenantId { get; init; }
    public Guid EmployeeId { get; init; }
    public decimal Amount { get; init; }
    public string PayoutPeriodName { get; init; } = string.Empty;
    public DateTimeOffset OccurredOnUtc { get; init; }
}
