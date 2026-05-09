namespace Karamchari.Payroll.Contracts;

public sealed record LoanCreatedIntegrationEvent
{
    public Guid LoanId { get; init; }
    public Guid TenantId { get; init; }
    public Guid EmployeeId { get; init; }
    public string LoanType { get; init; } = string.Empty;
    public decimal PrincipalAmount { get; init; }
    public int TenureMonths { get; init; }
    public DateTimeOffset OccurredOnUtc { get; init; }
}

public sealed record LoanClosedIntegrationEvent
{
    public Guid LoanId { get; init; }
    public Guid TenantId { get; init; }
    public Guid EmployeeId { get; init; }
    public string ClosureType { get; init; } = string.Empty;
    public DateTimeOffset OccurredOnUtc { get; init; }
}

public sealed record DeductLoanInstallmentCommand
{
    public Guid LoanId { get; init; }
    public Guid TenantId { get; init; }
    public Guid EmployeeId { get; init; }
    public string PeriodName { get; init; } = string.Empty;
    public int Year { get; init; }
    public int Month { get; init; }
}
