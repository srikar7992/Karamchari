// -----------------------------------------------------------------------
// <copyright file="SalaryComponent.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Payroll.Domain.SalaryStructures;

using Karamchari.Core.Multitenancy;

/// <summary>
/// Represents a master definition of a salary component (e.g., "Basic", "HRA", "PF").
/// </summary>
public sealed class SalaryComponent : ITenantOwned
{
    private SalaryComponent(Guid id, string name, ComponentType type, RoundingStrategy rounding)
    {
        Id = id;
        Name = name;
        Type = type;
        Rounding = rounding;
    }

    private SalaryComponent()
    {
        Name = string.Empty;
        TenantId = string.Empty;
    }

    /// <summary>Gets the tenant identifier.</summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Gets the unique identifier for the component.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the display name of the component.</summary>
    public string Name { get; private set; }

    /// <summary>Gets the type of component.</summary>
    public ComponentType Type { get; private set; }

    /// <summary>Gets the rounding strategy to be applied to calculations.</summary>
    public RoundingStrategy Rounding { get; private set; }

    /// <summary>
    /// Creates a new salary component.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="type">The type.</param>
    /// <param name="rounding">The rounding strategy.</param>
    /// <returns>A new <see cref="SalaryComponent"/> instance.</returns>
    public static SalaryComponent Create(string name, ComponentType type, RoundingStrategy rounding = RoundingStrategy.NearestRupee)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new SalaryComponent(Guid.NewGuid(), name, type, rounding);
    }
}
