// -----------------------------------------------------------------------
// <copyright file="PayrollSimulationEngine.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Payroll.Data;
using Karamchari.Payroll.Domain.Simulation;
using Karamchari.Payroll.Services;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Payroll.Services.Simulation;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public record SimulationParameters(
    SimulationType Type,
    IReadOnlyList<Guid>? EmployeeIds,       // null = all active employees
    decimal? GlobalCTCAdjustmentPercent,     // e.g., +5% increment simulation
    IReadOnlyDictionary<Guid, decimal>? PerEmployeeCTC,  // per-employee overrides
    string? ForPeriodName);

/// <summary>
/// Executes payroll simulations without touching real payroll state.
/// All projections are ephemeral â€” stored only in PayrollSimulation aggregate.
/// Never raises domain events that could trigger downstream workflows.
/// </summary>
public sealed class PayrollSimulationEngine
{
    private readonly PayrollDbContext _db;
    private readonly CTCBreakdownService _ctcBreakdown;

    public PayrollSimulationEngine(PayrollDbContext db, CTCBreakdownService ctcBreakdown)
    {
        _db = db;
        _ctcBreakdown = ctcBreakdown;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public async Task<PayrollSimulation> RunAsync(
        string tenantId,
        SimulationParameters parameters,
        string requestedBy,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        var simulation = PayrollSimulation.Start(
            tenantId,
            parameters.Type,
            System.Text.Json.JsonSerializer.Serialize(parameters),
            requestedBy);

        try
        {
            var profileQuery = _db.PayrollProfiles
                .AsNoTracking()
                .Where(p => p.IsActive);

            if (parameters.EmployeeIds?.Count > 0)
                profileQuery = profileQuery.Where(p => parameters.EmployeeIds.Contains(p.EmployeeId));

            var profiles = await profileQuery.ToListAsync(ct);

            // Load current ledger for delta calculation
            var employeeIds = profiles.Select(p => p.EmployeeId).ToList();
            var latestLedger = await _db.PayrollLedger
                .AsNoTracking()
                .Where(l => employeeIds.Contains(l.EmployeeId))
                .GroupBy(l => l.EmployeeId)
                .Select(g => g.OrderByDescending(l => l.Year).ThenByDescending(l => l.Month).First())
                .ToListAsync(ct);

            var ledgerMap = latestLedger.ToDictionary(l => l.EmployeeId);

            var results = new List<SimulationEmployeeResult>();

            foreach (var profile in profiles)
            {
                var currentCTC = profile.BaseSalary * 12m;
                decimal projectedCTC = currentCTC;

                if (parameters.PerEmployeeCTC?.TryGetValue(profile.EmployeeId, out var perEmpCTC) == true)
                    projectedCTC = perEmpCTC;
                else if (parameters.GlobalCTCAdjustmentPercent.HasValue)
                    projectedCTC = currentCTC * (1 + parameters.GlobalCTCAdjustmentPercent.Value / 100m);

                // Simplified projection â€” real implementation routes through full CTC pipeline
                var projectedMonthlyGross = projectedCTC / 12m;
                var projectedNet = projectedMonthlyGross * 0.85m;
                var projectedTds = projectedMonthlyGross * 0.10m;

                ledgerMap.TryGetValue(profile.EmployeeId, out var existing);
                var currentGross = existing?.MonthlyGross ?? profile.BaseSalary;
                var currentNet = existing?.NetPay ?? profile.BaseSalary * 0.85m;
                var currentTds = existing?.TdsDeducted ?? profile.BaseSalary * 0.10m;

                results.Add(new SimulationEmployeeResult(
                    profile.EmployeeId,
                    profile.EmployeeName,
                    currentGross,
                    projectedMonthlyGross,
                    currentNet,
                    projectedNet,
                    currentTds,
                    projectedTds,
                    new Dictionary<string, decimal>
                    {
                        ["Basic"] = projectedMonthlyGross * 0.40m,
                        ["HRA"] = projectedMonthlyGross * 0.20m,
                        ["SpecialAllowance"] = projectedMonthlyGross * 0.40m
                    }));
            }

            simulation.Complete(results);
        }
        catch (Exception ex)
        {
            simulation.Fail(ex.Message);
        }

        return simulation;
    }
}
