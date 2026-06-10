// -----------------------------------------------------------------------
// <copyright file="SupplyForecastCalculator.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Forecasting.Domain.Workforce;
using Karamchari.Forecasting.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Forecasting.Services;

/// <summary>
/// Pre-loaded supply-side data for a single (tenant, location, skill) triplet.
/// Load once via <see cref="SupplyForecastCalculator.LoadBulkDataAsync"/>; pass to
/// <see cref="SupplyForecastCalculator.CalculateFromBulkData"/> for each horizon.
/// Reduces 7 × N DB round-trips (one set per horizon) to 5 queries total per projection.
/// </summary>
public sealed record SupplyBulkData(
    List<DateOnly> SkillExpiryDates,
    List<(DateOnly StartDate, double Probability)> FutureHires,
    List<DateOnly> ContractEndDates,
    List<DateOnly> RetirementDates,
    List<(DateOnly StartDate, DateOnly EndDate)> ApprovedLeaves);

/// <summary>
/// Computes enriched supply forecasts by layering seven factors over total headcount:
///
/// Additions:
/// + Future hires (offer accepted, not yet onboarded) starting on or before forecast date
///
/// Deductions:
/// - Skill expiry (certification expires before forecast date)
/// - Attrition (expected departures, quarterly rate scaled by horizon days / 90)
/// - Contract expiry (fixed-term employees whose contract ends before forecast date)
/// - Retirement (employees expected to retire before forecast date)
/// - Approved leave (known leave records covering the forecast date)
/// - Unplanned absence (composite day-of-week + month rate)
///
/// Result: ExpectedHeadcount = max(0, Total + FutureHires - SkillExpiry - Attrition - ContractExpiry - Retirement - Leave - Absence)
///
/// Overtime and cross-skill capacity are NOT subtracted from supply; they are applied in HiringGap
/// to reduce net external hiring need. Supply itself reflects actual expected availability.
/// </summary>
public sealed class SupplyForecastCalculator
{
    private readonly ForecastingDbContext _db;

    public SupplyForecastCalculator(ForecastingDbContext db) => _db = db;

    public async Task<SupplyForecast> CalculateAsync(
        string tenantId,
        WorkforcePlanningProjection projection,
        DateOnly forecastDate,
        ForecastHorizon horizon,
        CancellationToken ct)
    {
        // 1. Skill expiry — employees who will lose certification eligibility by forecast date.
        // Boundary: ExpiryDate < forecastDate (strict less-than).
        // Employee whose cert expires ON the forecast day is still valid that day.
        var skillExpiryCount = await _db.WfpSkillExpiries
            .CountAsync(e =>
                e.TenantId == tenantId &&
                e.LocationCode == projection.LocationCode &&
                e.SkillCode == projection.SkillCode &&
                !e.IsRevoked &&
                e.ExpiryDate < forecastDate,
                ct);

        // 2. Attrition — scale quarterly rate by horizon
        var horizonDays = (int)horizon;
        var expectedAttrition = (int)Math.Round(
            projection.TotalEmployees *
            (projection.HistoricalAttritionRatePercent / 100m) *
            (horizonDays / 90.0m));

        // 3. Approved leave — known future leave covering forecast date
        var approvedLeaveCount = await _db.WfpApprovedLeaveRecords
            .CountAsync(r =>
                r.TenantId == tenantId &&
                r.LocationCode == projection.LocationCode &&
                r.SkillCode == projection.SkillCode &&
                !r.IsCancelled &&
                r.StartDate <= forecastDate &&
                r.EndDate >= forecastDate,
                ct);

        // 4. Unplanned absence — composite DayOfWeek+Month rate, falling back through day-only, then flat average
        var absenceRate = projection.GetAbsenceRate(forecastDate);

        // 5. Future hires — offer accepted but not yet onboarded, expected to start on or before forecast date.
        // Boundary: ExpectedStartDate <= forecastDate (inclusive).
        // Hire starting ON the forecast day is available that day.
        // SUM(JoiningProbability) instead of COUNT so that a 0.75-probability hire contributes 0.75 headcount.
        // This prevents over-optimistic supply when offer drop-off rates are configured.
        var futureHireSum = await _db.WfpFutureHires
            .Where(h =>
                h.TenantId == tenantId &&
                h.LocationCode == projection.LocationCode &&
                h.SkillCode == projection.SkillCode &&
                !h.IsOnboarded &&
                h.ExpectedStartDate <= forecastDate)
            .SumAsync(h => (double)h.JoiningProbability, ct);
        var futureHireCount = (int)Math.Round(futureHireSum);

        // 6. Contract expiry — fixed-term employees whose contract ends before forecast date.
        // Boundary: ContractEndDate < forecastDate (strict less-than).
        // Employee whose contract ends ON the forecast day is still employed that day.
        var contractExpiryCount = await _db.WfpContractExpiries
            .CountAsync(c =>
                c.TenantId == tenantId &&
                c.LocationCode == projection.LocationCode &&
                c.SkillCode == projection.SkillCode &&
                !c.IsTerminated &&
                c.ContractEndDate < forecastDate,
                ct);

        // 7. Retirement — employees expected to retire before forecast date.
        // Boundary: ExpectedRetirementDate < forecastDate (strict less-than).
        // Employee retiring ON the forecast day is still counted that day.
        var retirementCount = await _db.WfpRetirementRisks
            .CountAsync(r =>
                r.TenantId == tenantId &&
                r.LocationCode == projection.LocationCode &&
                r.SkillCode == projection.SkillCode &&
                !r.IsRetired &&
                r.ExpectedRetirementDate < forecastDate,
                ct);

        var adjustedTotal = Math.Max(0,
            projection.TotalEmployees
            + futureHireCount
            - skillExpiryCount
            - expectedAttrition
            - contractExpiryCount
            - retirementCount);
        var expectedAbsent = (int)Math.Round(adjustedTotal * absenceRate / 100m);

        return SupplyForecast.Create(
            tenantId,
            forecastDate,
            projection.LocationCode,
            projection.SkillCode,
            projection.TotalEmployees,
            approvedLeaveCount,
            skillExpiryCount,
            expectedAttrition,
            expectedAbsent,
            horizon,
            projection.AttendanceReliabilityScore,
            futureHireCount,
            contractExpiryCount,
            retirementCount);
    }

    // ── Bulk path ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Loads all supply-relevant rows for one (tenant, location, skill) in 5 queries.
    /// Pass the result to <see cref="CalculateFromBulkData"/> for each horizon — all
    /// subsequent filtering is done in memory, eliminating N×7 sequential DB round-trips.
    /// </summary>
    public async Task<SupplyBulkData> LoadBulkDataAsync(
        string tenantId, string locationCode, string skillCode, CancellationToken ct)
    {
        var expiryDates = await _db.WfpSkillExpiries
            .Where(e => e.TenantId == tenantId && e.LocationCode == locationCode &&
                        e.SkillCode == skillCode && !e.IsRevoked)
            .Select(e => e.ExpiryDate)
            .ToListAsync(ct);

        var futureHires = await _db.WfpFutureHires
            .Where(h => h.TenantId == tenantId && h.LocationCode == locationCode &&
                        h.SkillCode == skillCode && !h.IsOnboarded)
            .Select(h => new { h.ExpectedStartDate, Probability = (double)h.JoiningProbability })
            .ToListAsync(ct);

        var contractEndDates = await _db.WfpContractExpiries
            .Where(c => c.TenantId == tenantId && c.LocationCode == locationCode &&
                        c.SkillCode == skillCode && !c.IsTerminated)
            .Select(c => c.ContractEndDate)
            .ToListAsync(ct);

        var retirementDates = await _db.WfpRetirementRisks
            .Where(r => r.TenantId == tenantId && r.LocationCode == locationCode &&
                        r.SkillCode == skillCode && !r.IsRetired)
            .Select(r => r.ExpectedRetirementDate)
            .ToListAsync(ct);

        var leaves = await _db.WfpApprovedLeaveRecords
            .Where(r => r.TenantId == tenantId && r.LocationCode == locationCode &&
                        r.SkillCode == skillCode && !r.IsCancelled)
            .Select(r => new { r.StartDate, r.EndDate })
            .ToListAsync(ct);

        return new SupplyBulkData(
            expiryDates,
            futureHires.Select(h => (h.ExpectedStartDate, h.Probability)).ToList(),
            contractEndDates,
            retirementDates,
            leaves.Select(l => (l.StartDate, l.EndDate)).ToList());
    }

    /// <summary>
    /// Computes a supply forecast for one forecastDate using pre-loaded bulk data.
    /// No DB queries — all filtering is done in memory. Boundary semantics are
    /// identical to <see cref="CalculateAsync"/>; verified by existing boundary predicate tests.
    /// </summary>
    public SupplyForecast CalculateFromBulkData(
        string tenantId,
        WorkforcePlanningProjection projection,
        DateOnly forecastDate,
        ForecastHorizon horizon,
        SupplyBulkData data)
    {
        // Boundary: ExpiryDate < forecastDate (exclusive) — cert expiring ON the day still valid
        var skillExpiryCount = data.SkillExpiryDates.Count(d => d < forecastDate);

        var horizonDays = (int)horizon;
        var expectedAttrition = (int)Math.Round(
            projection.TotalEmployees *
            (projection.HistoricalAttritionRatePercent / 100m) *
            (horizonDays / 90.0m));

        var approvedLeaveCount = data.ApprovedLeaves
            .Count(l => l.StartDate <= forecastDate && l.EndDate >= forecastDate);

        var absenceRate = projection.GetAbsenceRate(forecastDate);

        // Boundary: ExpectedStartDate <= forecastDate (inclusive) — hire starting ON the day available
        var futureHireSum = data.FutureHires
            .Where(h => h.StartDate <= forecastDate)
            .Sum(h => h.Probability);
        var futureHireCount = (int)Math.Round(futureHireSum);

        // Boundary: ContractEndDate < forecastDate (exclusive) — contract ending ON the day still employed
        var contractExpiryCount = data.ContractEndDates.Count(d => d < forecastDate);

        // Boundary: ExpectedRetirementDate < forecastDate (exclusive) — retiring ON the day still counted
        var retirementCount = data.RetirementDates.Count(d => d < forecastDate);

        var adjustedTotal = Math.Max(0,
            projection.TotalEmployees
            + futureHireCount
            - skillExpiryCount
            - expectedAttrition
            - contractExpiryCount
            - retirementCount);
        var expectedAbsent = (int)Math.Round(adjustedTotal * absenceRate / 100m);

        return SupplyForecast.Create(
            tenantId,
            forecastDate,
            projection.LocationCode,
            projection.SkillCode,
            projection.TotalEmployees,
            approvedLeaveCount,
            skillExpiryCount,
            expectedAttrition,
            expectedAbsent,
            horizon,
            projection.AttendanceReliabilityScore,
            futureHireCount,
            contractExpiryCount,
            retirementCount);
    }
}
