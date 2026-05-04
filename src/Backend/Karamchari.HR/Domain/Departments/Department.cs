using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.HR.Domain.Departments.Events;

namespace Karamchari.HR.Domain.Departments;

public sealed class Department : AggregateRoot<Guid>, ITenantOwned
{
    private Department(Guid id, string name, string? description)
        : base(id)
    {
        Name = NormalizeRequired(name, nameof(name));
        Description = NormalizeOptional(description);
    }

    private Department()
    {
        TenantId = string.Empty;
        Name = string.Empty;
    }

    public string TenantId { get; private set; } = string.Empty;

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public bool IsActive { get; private set; } = true;

    public static Department Create(string name, string? description)
    {
        var department = new Department(Guid.NewGuid(), name, description);
        department.RaiseDomainEvent(new DepartmentCreated(
            department.Id,
            department.Name,
            department.Description));

        return department;
    }

    public void Rename(string name) => Name = NormalizeRequired(name, nameof(name));

    public void ChangeDescription(string? description) => Description = NormalizeOptional(description);

    public void Deactivate() => IsActive = false;

    private static string NormalizeRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
