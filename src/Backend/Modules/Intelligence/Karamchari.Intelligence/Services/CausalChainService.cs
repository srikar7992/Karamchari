// -----------------------------------------------------------------------
// <copyright file="CausalChainService.cs" company="Karamchari">
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
/// Detects and upserts per-employee <see cref="EmployeeCausalChain"/> records.
/// Reads existing burnout/attrition scores, signals, and schedule quality to determine
/// which links in the risk propagation chain (Coverage → OT → Leave → Schedule → Burnout → Attrition)
/// are currently active.
///
/// Called by <see cref="WorkforceIntelligenceRecomputeJob"/> once per tenant per nightly pass,
/// after burnout/attrition scoring and schedule quality are complete.
/// </summary>
public sealed class CausalChainService
{
    private readonly IntelligenceDbContext _db;
    private readonly ILogger<CausalChainService> _logger;

    public CausalChainService(IntelligenceDbContext db, ILogger<CausalChainService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Detects causal chains for all scored employees in the tenant.
    /// </summary>
    public async Task RecalculateTenantCausalChainsAsync(string tenantId, CancellationToken ct = default)
    {
        var employeeIds = await _db.WorkforceBurnoutScores
            .Where(b => b.TenantId == tenantId)
            .Select(b => b.EmployeeId)
            .ToListAsync(ct);

        if (employeeIds.Count == 0) return;

        _logger.LogInformation(
            "Detecting causal chains for {Count} employees in tenant {TenantId}",
            employeeIds.Count, tenantId);

        // Load all relevant scores + signals into memory to avoid N+1
        var burnoutScores = await _db.WorkforceBurnoutScores
            .Where(b => b.TenantId == tenantId)
            .Select(b => new { b.EmployeeId, b.Score })
            .ToDictionaryAsync(b => b.EmployeeId, b => b.Score, ct);

        var attritionScores = await _db.WorkforceAttritionScores
            .Where(a => a.TenantId == tenantId)
            .Select(a => new { a.EmployeeId, a.Score })
            .ToDictionaryAsync(a => a.EmployeeId, a => a.Score, ct);

        var scheduleQualities = await _db.EmployeeScheduleQualities
            .Where(q => q.TenantId == tenantId)
            .Select(q => new { q.EmployeeId, q.QualityScore })
            .ToDictionaryAsync(q => q.EmployeeId, q => q.QualityScore, ct);

        // Latest value per employee for each relevant signal type
        var signalTypes = new[]
        {
            WorkforceSignalType.CriticalShiftCoverageRatio,
            WorkforceSignalType.OvertimeHours28d,
            WorkforceSignalType.DaysWithoutLeave
        };

        var signals = await _db.WorkforceSignalRecords
            .Where(s => s.TenantId == tenantId && signalTypes.Contains(s.SignalType))
            .Select(s => new { s.EmployeeId, s.SignalType, s.Value, s.SignalDate })
            .ToListAsync(ct);

        var latestSignal = signals
            .GroupBy(s => new { s.EmployeeId, s.SignalType })
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(s => s.SignalDate).First().Value);

        decimal GetSignal(Guid empId, WorkforceSignalType type)
        {
            latestSignal.TryGetValue(new { EmployeeId = empId, SignalType = type }, out var v);
            return v;
        }

        foreach (var employeeId in employeeIds)
        {
            try
            {
                var input = new CausalChainInput(
                    CriticalShiftCoverageRatio: GetSignal(employeeId, WorkforceSignalType.CriticalShiftCoverageRatio),
                    OvertimeHours28d: GetSignal(employeeId, WorkforceSignalType.OvertimeHours28d),
                    DaysWithoutLeave: (int)GetSignal(employeeId, WorkforceSignalType.DaysWithoutLeave),
                    ScheduleQualityScore: scheduleQualities.TryGetValue(employeeId, out var sq) ? sq : -1m,
                    BurnoutScore: burnoutScores.TryGetValue(employeeId, out var bs) ? bs : 0m,
                    AttritionScore: attritionScores.TryGetValue(employeeId, out var as2) ? as2 : 0m);

                var result = CausalChainDetector.Detect(input);

                var existing = await _db.EmployeeCausalChains
                    .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.EmployeeId == employeeId, ct);

                if (existing == null)
                {
                    _db.EmployeeCausalChains.Add(
                        EmployeeCausalChain.Create(tenantId, employeeId, result.ActiveLinks));
                }
                else
                {
                    existing.Refresh(result.ActiveLinks);
                }

                await _db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Causal chain detection failed for employee {EmployeeId}", employeeId);
            }
        }
    }
}
