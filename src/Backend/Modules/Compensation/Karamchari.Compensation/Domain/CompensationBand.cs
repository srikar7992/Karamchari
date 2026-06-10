// -----------------------------------------------------------------------
// <copyright file="CompensationBand.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Compensation.Domain;

/// <summary>
/// Salary/compensation band for a career level and job family.
/// Defines minimum, midpoint, and maximum compensation for a given band type.
/// Compa-ratio = (employee actual) / (band midpoint) * 100.
/// </summary>
public sealed class CompensationBand : AggregateRoot<Guid>, ITenantOwned
{
    private CompensationBand() { /* EF materialization */ }

    private CompensationBand(
        Guid id,
        string tenantId,
        string name,
        string jobFamily,
        string careerLevelCode,
        CompensationBandType bandType,
        string currencyCode,
        decimal minimumAnnual,
        decimal midpointAnnual,
        decimal maximumAnnual,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo) : base(id)
    {
        TenantId = tenantId;
        Name = name;
        JobFamily = jobFamily;
        CareerLevelCode = careerLevelCode;
        BandType = bandType;
        CurrencyCode = currencyCode;
        MinimumAnnual = minimumAnnual;
        MidpointAnnual = midpointAnnual;
        MaximumAnnual = maximumAnnual;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        IsActive = true;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Name { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string JobFamily { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string CareerLevelCode { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public CompensationBandType BandType { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string CurrencyCode { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal MinimumAnnual { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal MidpointAnnual { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal MaximumAnnual { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool IsActive { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static CompensationBand Create(
        string tenantId,
        string name,
        string jobFamily,
        string careerLevelCode,
        CompensationBandType bandType,
        string currencyCode,
        decimal minimumAnnual,
        decimal midpointAnnual,
        decimal maximumAnnual,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(jobFamily);
        ArgumentException.ThrowIfNullOrWhiteSpace(careerLevelCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minimumAnnual);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(midpointAnnual, minimumAnnual);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maximumAnnual, midpointAnnual);
        if (effectiveTo.HasValue && effectiveTo.Value < effectiveFrom)
            throw new ArgumentException("EffectiveTo cannot be before EffectiveFrom.");

        return new CompensationBand(
            Guid.NewGuid(), tenantId, name.Trim(), jobFamily.Trim(),
            careerLevelCode.Trim(), bandType, currencyCode.ToUpperInvariant(),
            minimumAnnual, midpointAnnual, maximumAnnual, effectiveFrom, effectiveTo);
    }

    /// <summary>Computes the compa-ratio for a given salary (employee actual / midpoint * 100).</summary>
    public decimal ComputeCompaRatio(decimal actualAnnualSalary) =>
        MidpointAnnual > 0 ? Math.Round(actualAnnualSalary / MidpointAnnual * 100, 2) : 0m;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        EffectiveTo ??= DateOnly.FromDateTime(DateTime.UtcNow);
    }
}
