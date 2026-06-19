// -----------------------------------------------------------------------
// <copyright file="BenefitPlan.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Benefits.Domain;

using Karamchari.Benefits.Domain.Events;
using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

/// <summary>
/// A cost-per-tier entry within a benefit plan.
/// Represents a single coverage option (e.g., "Family" at $320/mo employee + $480/mo employer).
/// </summary>
public sealed class BenefitPlanTier
{
    private BenefitPlanTier() { }

    internal BenefitPlanTier(CoverageTier tier, decimal employeeMonthlyPremium, decimal employerMonthlyPremium)
    {
        if (employeeMonthlyPremium < 0) throw new ArgumentOutOfRangeException(nameof(employeeMonthlyPremium));
        if (employerMonthlyPremium < 0) throw new ArgumentOutOfRangeException(nameof(employerMonthlyPremium));
        Tier = tier;
        EmployeeMonthlyPremium = employeeMonthlyPremium;
        EmployerMonthlyPremium = employerMonthlyPremium;
    }

    public CoverageTier Tier { get; private set; }
    public decimal EmployeeMonthlyPremium { get; private set; }
    public decimal EmployerMonthlyPremium { get; private set; }
    public decimal TotalMonthlyPremium => EmployeeMonthlyPremium + EmployerMonthlyPremium;

    internal void UpdateCosts(decimal employeePremium, decimal employerPremium)
    {
        if (employeePremium < 0) throw new ArgumentOutOfRangeException(nameof(employeePremium));
        if (employerPremium < 0) throw new ArgumentOutOfRangeException(nameof(employerPremium));
        EmployeeMonthlyPremium = employeePremium;
        EmployerMonthlyPremium = employerPremium;
    }
}

/// <summary>
/// Aggregate root for a benefit plan in the tenant's catalog.
/// A plan describes what is offered (Medical, Dental, etc.), by whom (ProviderName),
/// under what terms (waiting period, plan year), and at what cost per coverage tier.
///
/// Employees elect plans during an enrollment window; the plan catalog is managed by HR admins.
/// One tenant may have multiple plans per category (e.g., two medical options at different cost points).
/// </summary>
public sealed class BenefitPlan : AggregateRoot<BenefitPlanId>, ITenantOwned
{
    private readonly List<BenefitPlanTier> _tiers = [];

    private BenefitPlan() { TenantId = string.Empty; }

    private BenefitPlan(
        BenefitPlanId id,
        string tenantId,
        string planName,
        string? description,
        BenefitCategory category,
        string providerName,
        string? externalPlanCode,
        DateOnly planYearStart,
        DateOnly planYearEnd,
        int waitingPeriodDays) : base(id)
    {
        TenantId = tenantId;
        PlanName = planName;
        Description = description;
        Category = category;
        ProviderName = providerName;
        ExternalPlanCode = externalPlanCode;
        PlanYearStart = planYearStart;
        PlanYearEnd = planYearEnd;
        WaitingPeriodDays = waitingPeriodDays;
        Status = BenefitPlanStatus.Draft;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public string TenantId { get; private set; }
    public string PlanName { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public BenefitCategory Category { get; private set; }
    public BenefitPlanStatus Status { get; private set; }

    /// <summary>Carrier/insurer name (e.g., "Cigna", "United Healthcare").</summary>
    public string ProviderName { get; private set; } = string.Empty;

    /// <summary>Carrier's plan identifier used in carrier export files.</summary>
    public string? ExternalPlanCode { get; private set; }

    /// <summary>First day of the plan year this version of the plan covers.</summary>
    public DateOnly PlanYearStart { get; private set; }

    /// <summary>Last day of the plan year this version of the plan covers.</summary>
    public DateOnly PlanYearEnd { get; private set; }

    /// <summary>
    /// Days after hire date before the employee becomes eligible.
    /// Zero means eligible on day one.
    /// </summary>
    public int WaitingPeriodDays { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? PublishedAtUtc { get; private set; }

    public IReadOnlyList<BenefitPlanTier> Tiers => _tiers.AsReadOnly();

    /// <summary>Computes the date an employee becomes eligible based on their hire date.</summary>
    public DateOnly EligibleFrom(DateOnly hireDate) => hireDate.AddDays(WaitingPeriodDays);

    public static BenefitPlan Create(
        string tenantId,
        string planName,
        string? description,
        BenefitCategory category,
        string providerName,
        string? externalPlanCode,
        DateOnly planYearStart,
        DateOnly planYearEnd,
        int waitingPeriodDays = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(planName);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);

        if (planYearEnd <= planYearStart)
            throw new ArgumentException("Plan year end must be after plan year start.");
        if (waitingPeriodDays < 0)
            throw new ArgumentOutOfRangeException(nameof(waitingPeriodDays), "Waiting period cannot be negative.");

        return new BenefitPlan(
            new BenefitPlanId(Guid.NewGuid()),
            tenantId, planName, description, category,
            providerName, externalPlanCode,
            planYearStart, planYearEnd, waitingPeriodDays);
    }

    /// <summary>
    /// Adds or updates a coverage tier with its premium costs.
    /// Calling this for an existing tier updates the costs in place.
    /// Plans in Active or higher status allow tier cost updates to support
    /// mid-year rate adjustments, but the change is recorded on the tier directly.
    /// </summary>
    public void SetTierCost(CoverageTier tier, decimal employeeMonthly, decimal employerMonthly)
    {
        if (Status == BenefitPlanStatus.Discontinued)
            throw new InvalidOperationException("Cannot modify a discontinued plan.");

        var existing = _tiers.FirstOrDefault(t => t.Tier == tier);
        if (existing is not null)
            existing.UpdateCosts(employeeMonthly, employerMonthly);
        else
            _tiers.Add(new BenefitPlanTier(tier, employeeMonthly, employerMonthly));
    }

    /// <summary>
    /// Publishes the plan, making it visible to employees during enrollment.
    /// Requires at least one coverage tier to be configured.
    /// </summary>
    public void Publish()
    {
        if (Status != BenefitPlanStatus.Draft)
            throw new InvalidOperationException($"Only Draft plans can be published; current status is {Status}.");
        if (_tiers.Count == 0)
            throw new InvalidOperationException("A plan must have at least one coverage tier before it can be published.");

        Status = BenefitPlanStatus.Active;
        PublishedAtUtc = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new BenefitPlanPublishedEvent(TenantId, Id, Category, PlanName));
    }

    public void Deactivate()
    {
        if (Status == BenefitPlanStatus.Discontinued)
            throw new InvalidOperationException("Cannot deactivate a discontinued plan.");
        Status = BenefitPlanStatus.Inactive;
    }

    public void Discontinue()
    {
        Status = BenefitPlanStatus.Discontinued;
    }

    /// <summary>Returns the tier cost entry for a given tier, or null if not configured.</summary>
    public BenefitPlanTier? GetTier(CoverageTier tier) =>
        _tiers.FirstOrDefault(t => t.Tier == tier);
}
