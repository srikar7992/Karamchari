namespace Karamchari.HR.Domain.Organization;

using System;
using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

/// <summary>
/// Represents the allocated annual compensation budget for a specific position.
/// </summary>
public sealed class PositionBudget : AggregateRoot<Guid>, ITenantOwned
{
    private PositionBudget() { TenantId = string.Empty; Currency = "USD"; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PositionBudget"/> class.
    /// </summary>
    /// <param name="id">The unique identifier of the position budget.</param>
    /// <param name="tenantId">The unique identifier of the tenant context.</param>
    /// <param name="positionId">The ID of the position the budget is allocated for.</param>
    /// <param name="annualBudgetAmount">The annual budgeted currency amount.</param>
    /// <param name="currency">The currency code (e.g. USD, EUR).</param>
    /// <param name="effectiveFrom">The date and time from which the budget allocation is effective.</param>
    /// <param name="effectiveTo">The date and time until which the budget allocation is effective, if defined.</param>
    public PositionBudget(
        Guid id,
        string tenantId,
        Guid positionId,
        decimal annualBudgetAmount,
        string currency,
        DateTimeOffset effectiveFrom,
        DateTimeOffset? effectiveTo) : base(id)
    {
        TenantId = tenantId;
        PositionId = positionId;
        AnnualBudgetAmount = annualBudgetAmount;
        Currency = currency ?? "USD";
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
    }

    /// <summary>Gets the tenant ID for multi-tenant isolation.</summary>
    public string TenantId { get; private set; }
    /// <summary>Gets the ID of the position the budget is allocated for.</summary>
    public Guid PositionId { get; private set; }
    /// <summary>Gets the annual budgeted currency amount.</summary>
    public decimal AnnualBudgetAmount { get; private set; }
    /// <summary>Gets the currency code (e.g. USD, EUR).</summary>
    public string Currency { get; private set; }
    /// <summary>Gets the date and time from which the budget allocation is effective.</summary>
    public DateTimeOffset EffectiveFrom { get; private set; }
    /// <summary>Gets the date and time until which the budget allocation is effective, if defined.</summary>
    public DateTimeOffset? EffectiveTo { get; private set; }
}
