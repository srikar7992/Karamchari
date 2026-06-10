// -----------------------------------------------------------------------
// <copyright file="ScheduleQualityService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Intelligence.Domain.Workforce;
using Karamchari.Intelligence.Persistence;
using Karamchari.Intelligence.Services.Scoring;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.Intelligence.Services;

/// <summary>
/// Nightly upsert of <see cref="EmployeeScheduleQuality"/> for all employees in a tenant.
/// Reads schedule-ergonomic signal records and runs <see cref="ScheduleQualityCalculator"/>.
/// Called by <see cref="WorkforceIntelligenceRecomputeJob"/> once per tenant.
/// </summary>
public sealed class ScheduleQualityService
{
    private readonly IntelligenceDbContext _db;
    private readonly ILogger<ScheduleQualityService> _logger;

    public ScheduleQualityService(IntelligenceDbContext db, ILogger<ScheduleQualityService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Recalculates schedule quality scores for every employee in <paramref name="tenantId"/>
    /// that has at least one schedule-quality signal.
    /// </summary>
    public async Task RecalculateTenantScheduleQualityAsync(string tenantId, CancellationToken ct = default)
    {
        var scheduleSignalTypes = new[]
        {
            WorkforceSignalType.NightToMorningTransitions7d,
            WorkforceSignalType.ShortRestViolations7d,
            WorkforceSignalType.WeekendShiftClustering,
            WorkforceSignalType.SplitShiftCount30d
        };

        var employeeIds = await _db.WorkforceSignalRecords
            .Where(s => s.TenantId == tenantId && scheduleSignalTypes.Contains(s.SignalType))
            .Select(s => s.EmployeeId)
            .Distinct()
            .ToListAsync(ct);

        if (employeeIds.Count == 0) return;

        _logger.LogInformation(
            "Recalculating schedule quality for {Count} employees in tenant {TenantId}",
            employeeIds.Count, tenantId);

        foreach (var employeeId in employeeIds)
        {
            try
            {
                await UpsertScheduleQualityAsync(tenantId, employeeId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Schedule quality recalculation failed for employee {EmployeeId}", employeeId);
            }
        }
    }

    private async Task UpsertScheduleQualityAsync(
        string tenantId, Guid employeeId, CancellationToken ct)
    {
        decimal Latest(WorkforceSignalType t)
        {
            return _db.WorkforceSignalRecords
                .Where(s => s.TenantId == tenantId && s.EmployeeId == employeeId && s.SignalType == t)
                .OrderByDescending(s => s.SignalDate)
                .Select(s => s.Value)
                .FirstOrDefault();
        }

        var input = new ScheduleQualityInput(
            NightToMorningTransitions7d: (int)Latest(WorkforceSignalType.NightToMorningTransitions7d),
            ShortRestViolations7d: (int)Latest(WorkforceSignalType.ShortRestViolations7d),
            WeekendShiftClustering: Latest(WorkforceSignalType.WeekendShiftClustering),
            SplitShiftCount30d: (int)Latest(WorkforceSignalType.SplitShiftCount30d));

        var result = ScheduleQualityCalculator.Calculate(input);

        var existing = await _db.EmployeeScheduleQualities
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.EmployeeId == employeeId, ct);

        if (existing == null)
        {
            _db.EmployeeScheduleQualities.Add(EmployeeScheduleQuality.Create(
                tenantId, employeeId,
                result.QualityScore, result.PoorScheduleRiskLevel,
                result.RotationStressComponent, result.RestViolationComponent,
                result.WeekendClusterComponent, result.SplitShiftComponent));
        }
        else
        {
            existing.Refresh(result.QualityScore, result.PoorScheduleRiskLevel,
                result.RotationStressComponent, result.RestViolationComponent,
                result.WeekendClusterComponent, result.SplitShiftComponent);
        }

        await _db.SaveChangesAsync(ct);
    }
}
