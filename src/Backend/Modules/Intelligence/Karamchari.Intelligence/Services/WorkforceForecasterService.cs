// -----------------------------------------------------------------------
// <copyright file="WorkforceForecasterService.cs" company="Karamchari">
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
/// Computes velocity and linear forecasts for burnout and attrition scores.
/// Reads score snapshots written by WorkforceSignalService, then upserts WorkforceForecast rows.
/// Called by WorkforceIntelligenceRecomputeJob after the main score pass.
/// </summary>
public sealed class WorkforceForecasterService
{
    private const int DefaultForecastHorizonDays = 21;
    private const int VelocityWindowDays = 30;

    private readonly IntelligenceDbContext _db;
    private readonly ILogger<WorkforceForecasterService> _logger;

    public WorkforceForecasterService(
        IntelligenceDbContext db,
        ILogger<WorkforceForecasterService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Computes and upserts burnout + attrition forecasts for all employees in the tenant.
    /// </summary>
    public async Task RecalculateTenantForecastsAsync(string tenantId, CancellationToken ct = default)
    {
        var employeeIds = await _db.WorkforceScoreSnapshots
            .Where(s => s.TenantId == tenantId)
            .Select(s => s.EmployeeId)
            .Distinct()
            .ToListAsync(ct);

        foreach (var employeeId in employeeIds)
        {
            try
            {
                await RecalculateEmployeeForecastAsync(tenantId, employeeId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Forecast failed for employee {EmployeeId}", employeeId);
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task RecalculateEmployeeForecastAsync(
        string tenantId,
        Guid employeeId,
        CancellationToken ct = default)
    {
        var snapshots = await _db.WorkforceScoreSnapshots
            .Where(s => s.TenantId == tenantId && s.EmployeeId == employeeId
                     && s.CalculatedAt >= DateTime.UtcNow.AddDays(-VelocityWindowDays))
            .OrderBy(s => s.CalculatedAt)
            .ToListAsync(ct);

        await UpsertForecastAsync(tenantId, employeeId, "Burnout",
            snapshots.Where(s => s.ScoreType == "Burnout")
                     .Select(s => (s.CalculatedAt, s.Score))
                     .ToList(), ct);

        await UpsertForecastAsync(tenantId, employeeId, "Attrition",
            snapshots.Where(s => s.ScoreType == "Attrition")
                     .Select(s => (s.CalculatedAt, s.Score))
                     .ToList(), ct);
    }

    private async Task UpsertForecastAsync(
        string tenantId,
        Guid employeeId,
        string scoreType,
        List<(DateTime At, decimal Score)> history,
        CancellationToken ct)
    {
        var result = LinearForecastEngine.Compute(history, DefaultForecastHorizonDays);

        var existing = await _db.WorkforceForecasts
            .FirstOrDefaultAsync(f => f.TenantId == tenantId
                                   && f.EmployeeId == employeeId
                                   && f.ScoreType == scoreType, ct);

        if (existing == null)
        {
            _db.WorkforceForecasts.Add(WorkforceForecast.Create(
                tenantId, employeeId, scoreType,
                result.CurrentScore, result.ProjectedScore,
                result.ForecastHorizonDays, result.PointsPerDay,
                result.DaysUntilCritical, result.Trend,
                result.DataPointsUsed, result.Confidence));
        }
        else
        {
            existing.Refresh(
                result.CurrentScore, result.ProjectedScore,
                result.ForecastHorizonDays, result.PointsPerDay,
                result.DaysUntilCritical, result.Trend,
                result.DataPointsUsed, result.Confidence);
        }
    }
}
