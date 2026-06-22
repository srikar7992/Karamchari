// -----------------------------------------------------------------------
// <copyright file="EnrollmentCommandService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Benefits.Services;

using Karamchari.Benefits.Domain;
using Karamchari.Benefits.Persistence;
using Karamchari.Core.Multitenancy;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Write operations for the employee's own enrollment: electing plans, managing
/// dependents, and submitting for HR review.
///
/// All mutations load the enrollment with all navigation collections included so
/// the aggregate can enforce its invariants (e.g., status guard on Elect).
/// </summary>
public sealed class EnrollmentCommandService(BenefitsDbContext db, ITenantProvider tenantProvider)
{
    /// <summary>
    /// Records the employee's election of a plan+tier for a coverage category.
    /// Tier cost is snapshotted from the current plan rates at the time of election.
    /// Replaces any prior pending election for the same category (last-write-wins before Submit).
    /// </summary>
    public async Task<Guid> ElectBenefitAsync(
        Guid employeeId,
        Guid planId,
        CoverageTier tier,
        DateOnly effectiveDate,
        CancellationToken ct = default)
    {
        var tenantId = tenantProvider.GetCurrentTenantId();
        var enrollment = await LoadMutableEnrollmentAsync(employeeId, tenantId, ct);

        var planKey = new BenefitPlanId(planId);
        var plan = await db.Plans
            .FirstOrDefaultAsync(p => p.Id == planKey && p.TenantId == tenantId, ct)
            ?? throw new InvalidOperationException($"Benefit plan {planId} not found.");

        var tierCost = plan.GetTier(tier)
            ?? throw new InvalidOperationException(
                $"Plan '{plan.PlanName}' does not offer the {tier} coverage tier.");

        var election = enrollment.Elect(
            plan.Id, plan.Category, tier,
            tierCost.EmployeeMonthlyPremium, tierCost.EmployerMonthlyPremium, effectiveDate);

        await db.SaveChangesAsync(ct);
        return election.Id.Value;
    }

    /// <summary>
    /// Marks a coverage category as explicitly waived (employee has other coverage).
    /// Removes any pending election for the category.
    /// </summary>
    public async Task WaiveCategoryAsync(Guid employeeId, BenefitCategory category, CancellationToken ct = default)
    {
        var tenantId = tenantProvider.GetCurrentTenantId();
        var enrollment = await LoadMutableEnrollmentAsync(employeeId, tenantId, ct);
        enrollment.WaiveCategory(category);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Adds a dependent and returns the new dependent ID.</summary>
    public async Task<Guid> AddDependentAsync(
        Guid employeeId,
        string firstName,
        string lastName,
        DateOnly dateOfBirth,
        DependentRelationship relationship,
        bool enrollMedical = false,
        bool enrollDental = false,
        bool enrollVision = false,
        CancellationToken ct = default)
    {
        var tenantId = tenantProvider.GetCurrentTenantId();
        var enrollment = await LoadMutableEnrollmentAsync(employeeId, tenantId, ct);
        var dependent = enrollment.AddDependent(
            firstName, lastName, dateOfBirth, relationship, enrollMedical, enrollDental, enrollVision);
        await db.SaveChangesAsync(ct);
        return dependent.Id.Value;
    }

    /// <summary>Soft-deletes a dependent (sets IsRemoved = true for audit purposes).</summary>
    public async Task RemoveDependentAsync(Guid employeeId, Guid dependentId, CancellationToken ct = default)
    {
        var tenantId = tenantProvider.GetCurrentTenantId();
        var enrollment = await LoadMutableEnrollmentAsync(employeeId, tenantId, ct);
        enrollment.RemoveDependent(new BenefitDependentId(dependentId));
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Submits the employee's elections for HR review.
    /// An empty election list is valid — the employee is waiving all coverage.
    /// </summary>
    public async Task SubmitEnrollmentAsync(Guid employeeId, CancellationToken ct = default)
    {
        var tenantId = tenantProvider.GetCurrentTenantId();
        var enrollment = await LoadMutableEnrollmentAsync(employeeId, tenantId, ct);
        enrollment.Submit();
        await db.SaveChangesAsync(ct);
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private async Task<BenefitEnrollment> LoadMutableEnrollmentAsync(
        Guid employeeId, string tenantId, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await db.Enrollments
            .Include(e => e.Elections)
            .Include(e => e.Dependents)
            .Include(e => e.LifeEvents)
            .FirstOrDefaultAsync(e =>
                e.TenantId == tenantId &&
                e.EmployeeId == employeeId &&
                e.PlanYearStart <= today &&
                e.PlanYearEnd >= today &&
                e.Status != EnrollmentStatus.Terminated, ct)
            ?? throw new InvalidOperationException(
                $"No active benefits enrollment found for employee {employeeId} in the current plan year.");
    }
}
