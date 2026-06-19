// -----------------------------------------------------------------------
// <copyright file="BenefitPlanQueryService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Benefits.Services;

using Karamchari.Benefits.Contracts.Dtos;
using Karamchari.Benefits.Domain;
using Karamchari.Benefits.Persistence;
using Karamchari.Core.Multitenancy;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Read-only queries over the benefit plan catalog.
/// GetAvailablePlansAsync returns only Active plans (employee self-service).
/// GetAllPlansAsync returns all statuses (HR admin).
/// Tiers are automatically loaded because BenefitPlanTier is an EF-owned collection.
/// </summary>
public sealed class BenefitPlanQueryService(BenefitsDbContext db, ITenantProvider tenantProvider)
{
    public async Task<IReadOnlyList<BenefitPlanDto>> GetAvailablePlansAsync(
        BenefitCategory? category = null,
        CancellationToken ct = default)
    {
        var tenantId = tenantProvider.GetCurrentTenantId();

        var query = db.Plans
            .Where(p => p.TenantId == tenantId && p.Status == BenefitPlanStatus.Active);

        if (category.HasValue)
            query = query.Where(p => p.Category == category.Value);

        var plans = await query
            .OrderBy(p => p.Category)
            .ThenBy(p => p.PlanName)
            .ToListAsync(ct);

        return plans.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<BenefitPlanDto>> GetAllPlansAsync(
        BenefitCategory? category = null,
        CancellationToken ct = default)
    {
        var tenantId = tenantProvider.GetCurrentTenantId();

        var query = db.Plans.Where(p => p.TenantId == tenantId);

        if (category.HasValue)
            query = query.Where(p => p.Category == category.Value);

        var plans = await query
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Status)
            .ThenBy(p => p.PlanName)
            .ToListAsync(ct);

        return plans.Select(ToDto).ToList();
    }

    public async Task<BenefitPlanDto?> GetPlanByIdAsync(Guid planId, CancellationToken ct = default)
    {
        var tenantId = tenantProvider.GetCurrentTenantId();
        var id = new BenefitPlanId(planId);
        var plan = await db.Plans
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId, ct);
        return plan is null ? null : ToDto(plan);
    }

    private static BenefitPlanDto ToDto(BenefitPlan plan) => new(
        plan.Id.Value,
        plan.PlanName,
        plan.Description,
        plan.Category.ToString(),
        plan.Status.ToString(),
        plan.ProviderName,
        plan.ExternalPlanCode,
        plan.PlanYearStart,
        plan.PlanYearEnd,
        plan.WaitingPeriodDays,
        plan.Tiers.Select(t => new BenefitPlanTierDto(
            t.Tier.ToString(),
            t.EmployeeMonthlyPremium,
            t.EmployerMonthlyPremium,
            t.TotalMonthlyPremium)).ToList());
}
