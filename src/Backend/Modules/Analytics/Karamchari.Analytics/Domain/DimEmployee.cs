using Karamchari.Core.Multitenancy;
namespace Karamchari.Analytics.Domain;

public sealed class DimEmployee : ITenantOwned
{
    public Guid EmployeeId { get; init; }
    public string TenantId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public string Grade { get; set; } = string.Empty;
    public DateOnly HireDate { get; set; }
    public DateOnly? TerminationDate { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset LastUpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
