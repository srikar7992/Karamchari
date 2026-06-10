// -----------------------------------------------------------------------
// <copyright file="Department.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.HR.Domain.Departments.Events;

namespace Karamchari.HR.Domain.Departments;

/// <summary>
/// Represents an organizational department within the HR system.
/// </summary>
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

    /// <summary>
    /// Gets the tenant identifier.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the department name.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the department description.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the department is active.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Creates a new department instance.
    /// </summary>
    /// <param name="name">The department name.</param>
    /// <param name="description">The department description.</param>
    /// <returns>A new <see cref="Department"/> instance.</returns>
    public static Department Create(string name, string? description)
    {
        var department = new Department(Guid.NewGuid(), name, description);
        department.RaiseDomainEvent(new DepartmentCreated(
            department.Id,
            department.Name,
            department.Description));

        return department;
    }

    /// <summary>
    /// Renames the department.
    /// </summary>
    /// <param name="name">The new name.</param>
    public void Rename(string name) => Name = NormalizeRequired(name, nameof(name));

    /// <summary>
    /// Changes the department description.
    /// </summary>
    /// <param name="description">The new description.</param>
    public void ChangeDescription(string? description) => Description = NormalizeOptional(description);

    /// <summary>
    /// Deactivates the department.
    /// </summary>
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
