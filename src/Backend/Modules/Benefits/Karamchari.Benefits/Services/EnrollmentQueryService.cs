// -----------------------------------------------------------------------
// <copyright file="EnrollmentQueryService.cs" company="Karamchari">
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
/// Read-only queries over employee benefit enrollments.
/// GetCurrentEnrollmentAsync is used by employees to view their own enrollment.
/// GetActiveEnrollmentsAsync is used by HR admins to review pending/submitted enrollments.
/// </summary>
public sealed class EnrollmentQueryService(BenefitsDbContext db, ITenantProvider tenantProvider)
{
    /// <summary>
    /// Returns the employee's enrollment for the current plan year (the year containing today).
    /// Excludes terminated enrollments.
    /// </summary>
    public async Task<EnrollmentDto?> GetCurrentEnrollmentAsync(Guid employeeId, CancellationToken ct = default)
    {
        var tenantId = tenantProvider.GetCurrentTenantId();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var enrollment = await db.Enrollments
            .Include(e => e.Elections)
            .Include(e => e.Dependents)
            .Include(e => e.LifeEvents)
            .FirstOrDefaultAsync(e =>
                e.TenantId == tenantId &&
                e.EmployeeId == employeeId &&
                e.PlanYearStart <= today &&
                e.PlanYearEnd >= today &&
                e.Status != EnrollmentStatus.Terminated, ct);

        return enrollment is null ? null : ToDto(enrollment);
    }

    /// <summary>
    /// Returns all non-terminated enrollments for the tenant, ordered by employee name.
    /// Used by HR admin for the enrollment management dashboard.
    /// </summary>
    public async Task<IReadOnlyList<EnrollmentSummaryDto>> GetActiveEnrollmentsAsync(CancellationToken ct = default)
    {
        var tenantId = tenantProvider.GetCurrentTenantId();

        var enrollments = await db.Enrollments
            .Include(e => e.Elections)
            .Where(e => e.TenantId == tenantId && e.Status != EnrollmentStatus.Terminated)
            .OrderBy(e => e.EmployeeName)
            .ToListAsync(ct);

        return enrollments.Select(ToSummaryDto).ToList();
    }

    private static EnrollmentDto ToDto(BenefitEnrollment e)
    {
        var activeElections = e.ActiveElections.ToList();
        var activeDependents = e.ActiveDependents.ToList();
        return new EnrollmentDto(
            e.Id.Value,
            e.EmployeeId,
            e.EmployeeName,
            e.Status.ToString(),
            e.PlanYearStart,
            e.PlanYearEnd,
            e.HireDate,
            activeElections.Select(el => new BenefitElectionDto(
                el.Id.Value,
                el.PlanId.Value,
                el.Category.ToString(),
                el.CoverageTier.ToString(),
                el.EmployeeMonthlyPremium,
                el.EmployerMonthlyPremium,
                el.TotalMonthlyPremium,
                el.EffectiveDate)).ToList(),
            activeDependents.Select(d => new BenefitDependentDto(
                d.Id.Value,
                d.FirstName,
                d.LastName,
                d.DateOfBirth,
                d.Relationship.ToString(),
                d.IsEnrolledMedical,
                d.IsEnrolledDental,
                d.IsEnrolledVision)).ToList(),
            e.LifeEvents.Select(le => new LifeEventDto(
                le.Id.Value,
                le.EventType.ToString(),
                le.EventDate,
                le.Status.ToString(),
                le.WindowClosesAt,
                le.IsWindowOpen,
                le.DocumentStorageReference,
                le.RejectionReason)).ToList(),
            activeElections.Sum(el => el.EmployeeMonthlyPremium),
            activeElections.Sum(el => el.EmployerMonthlyPremium));
    }

    private static EnrollmentSummaryDto ToSummaryDto(BenefitEnrollment e)
    {
        var activeElections = e.ActiveElections.ToList();
        return new EnrollmentSummaryDto(
            e.Id.Value,
            e.EmployeeId,
            e.EmployeeName,
            e.Status.ToString(),
            e.PlanYearStart,
            activeElections.Count,
            activeElections.Sum(el => el.EmployeeMonthlyPremium));
    }
}
