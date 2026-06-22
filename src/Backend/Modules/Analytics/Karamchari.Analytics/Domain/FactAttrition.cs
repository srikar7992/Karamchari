using Karamchari.Core.Multitenancy;
namespace Karamchari.Analytics.Domain;

public sealed class FactAttrition : ITenantOwned
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string TenantId { get; init; } = string.Empty;
    public int DateKey { get; init; }
    public Guid EmployeeId { get; init; }
    public Guid DepartmentId { get; init; }
    public string TerminationType { get; init; } = string.Empty;
    public int TenureMonths { get; init; }
}
