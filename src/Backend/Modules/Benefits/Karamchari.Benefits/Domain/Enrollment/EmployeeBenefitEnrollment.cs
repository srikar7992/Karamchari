using Karamchari.Benefits.Domain.Events;
using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Benefits.Domain.Enrollment;

public sealed class EmployeeBenefitEnrollment : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<Dependent> _dependents = [];
    private readonly List<Beneficiary> _beneficiaries = [];

    private EmployeeBenefitEnrollment() { }

    public string TenantId { get; private set; } = string.Empty;
    public Guid EmployeeId { get; private set; }
    public Guid PlanId { get; private set; }
    public string TierType { get; private set; } = string.Empty;
    public DateOnly EffectiveDate { get; private set; }
    public bool IsWaived { get; private set; }
    public IReadOnlyList<Dependent> Dependents => _dependents.AsReadOnly();
    public IReadOnlyList<Beneficiary> Beneficiaries => _beneficiaries.AsReadOnly();

    public static EmployeeBenefitEnrollment Enroll(
        string tenantId, Guid employeeId, Guid planId, string tierType, DateOnly effectiveDate)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            PlanId = planId,
            TierType = tierType,
            EffectiveDate = effectiveDate
        };

    public static EmployeeBenefitEnrollment Waive(
        string tenantId, Guid employeeId, Guid planId, DateOnly effectiveDate)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            PlanId = planId,
            EffectiveDate = effectiveDate,
            IsWaived = true
        };

    public void AddDependent(Dependent d) => _dependents.Add(d);
    public void AddBeneficiary(Beneficiary b)
    {
        if (_beneficiaries.Sum(x => x.AllocationPercent) + b.AllocationPercent > 100)
            throw new InvalidOperationException("Total beneficiary allocation cannot exceed 100%.");
        _beneficiaries.Add(b);
    }
}
