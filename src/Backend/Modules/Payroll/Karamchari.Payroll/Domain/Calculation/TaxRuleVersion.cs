// -----------------------------------------------------------------------
// <copyright file="TaxRuleVersion.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Multitenancy;

namespace Karamchari.Payroll.Domain.Calculation;

/// <summary>
/// Versioned tax rule definition. Never update — insert new version for each FY.
/// JsonDefinition contains slabs, surcharges, rebates in a schema-stable format.
/// </summary>
public sealed class TaxRuleVersion : ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public Guid Id { get; private set; }
    public int FinancialYear { get; private set; }
    public string Regime { get; private set; } = string.Empty;
    public DateOnly EffectiveFrom { get; private set; }
    public string JsonDefinition { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private TaxRuleVersion() { }

    public static TaxRuleVersion Create(
        string tenantId,
        int financialYear,
        string regime,
        DateOnly effectiveFrom,
        string jsonDefinition)
    {
        if (string.IsNullOrWhiteSpace(jsonDefinition))
            throw new ArgumentException("JsonDefinition must not be empty.");

        return new TaxRuleVersion
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FinancialYear = financialYear,
            Regime = regime,
            EffectiveFrom = effectiveFrom,
            JsonDefinition = jsonDefinition,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    public void Deactivate() => IsActive = false;
}
