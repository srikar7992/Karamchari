using Karamchari.Core.Multitenancy;

namespace Karamchari.Benefits.Domain.Deductions;

public sealed class BenefitDeductionRecord : ITenantOwned
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string TenantId { get; init; } = string.Empty;
    public Guid EmployeeId { get; init; }
    public Guid PlanId { get; init; }
    public string PayPeriod { get; init; } = string.Empty;
    public decimal EmployeeDeduction { get; init; }
    public decimal EmployerContribution { get; init; }
}
