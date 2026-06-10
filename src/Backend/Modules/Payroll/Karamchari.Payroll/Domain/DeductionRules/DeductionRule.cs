// -----------------------------------------------------------------------
// <copyright file="DeductionRule.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Multitenancy;

namespace Karamchari.Payroll.Domain.DeductionRules;

/// <summary>
/// Statutory deduction rule definition (e.g., PF = 12% capped at ₹1,800/month).
/// Rules are versioned by EffectiveFrom. Never update — insert new version.
/// </summary>
public sealed class DeductionRule : ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public Guid Id { get; private set; }
    public StatutoryDeductionType Type { get; private set; }
    public decimal Percentage { get; private set; }
    public decimal CapAmount { get; private set; }
    public decimal MinGrossForApplicability { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private DeductionRule() { }

    public static DeductionRule Create(
        string tenantId,
        StatutoryDeductionType type,
        decimal percentage,
        decimal capAmount,
        decimal minGrossForApplicability,
        DateOnly effectiveFrom)
    {
        return new DeductionRule
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Type = type,
            Percentage = percentage,
            CapAmount = capAmount,
            MinGrossForApplicability = minGrossForApplicability,
            EffectiveFrom = effectiveFrom,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    public decimal Calculate(decimal grossAmount)
    {
        if (grossAmount < MinGrossForApplicability)
            return 0m;

        var amount = grossAmount * (Percentage / 100m);
        return CapAmount > 0 ? Math.Min(amount, CapAmount) : amount;
    }

    public void Supersede() => IsActive = false;
}
