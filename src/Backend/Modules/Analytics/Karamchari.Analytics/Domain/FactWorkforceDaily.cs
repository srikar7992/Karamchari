using Karamchari.Core.Multitenancy;
namespace Karamchari.Analytics.Domain;

public sealed class FactWorkforceDaily : ITenantOwned
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string TenantId { get; init; } = string.Empty;
    public int DateKey { get; init; }
    public Guid EmployeeId { get; init; }
    public Guid DepartmentId { get; init; }
    public bool IsActive { get; init; }
    public decimal BasicSalary { get; set; }
    public decimal TotalCTC { get; set; }
    public decimal LeaveBalance { get; set; }
}
