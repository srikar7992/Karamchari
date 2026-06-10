// -----------------------------------------------------------------------
// <copyright file="ScenarioEvaluationService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using Karamchari.PlatformIntelligence.Domain.Platform;
using Karamchari.PlatformIntelligence.Domain.Scenarios;
using Karamchari.PlatformIntelligence.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.PlatformIntelligence.Services;

public sealed class ScenarioEvaluationService
{
    private readonly PlatformIntelligenceDbContext _db;
    private readonly CrossDomainSignalAggregator _aggregator;
    private readonly ILogger<ScenarioEvaluationService> _logger;

    public ScenarioEvaluationService(
        PlatformIntelligenceDbContext db,
        CrossDomainSignalAggregator aggregator,
        ILogger<ScenarioEvaluationService> logger)
    {
        _db = db;
        _aggregator = aggregator;
        _logger = logger;
    }

    public async Task<WorkforceScenario> CreateScenarioAsync(
        string tenantId,
        string name,
        string description,
        string constraintsJson,
        string createdBy,
        CancellationToken ct = default)
    {
        var scenario = WorkforceScenario.Create(tenantId, name, description, constraintsJson, createdBy);
        _db.WorkforceScenarios.Add(scenario);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Created scenario {ScenarioId} for tenant {TenantId}", scenario.Id, tenantId);
        return scenario;
    }

    public async Task<WorkforceScenario> EvaluateScenarioAsync(Guid scenarioId, CancellationToken ct = default)
    {
        var scenario = await _db.WorkforceScenarios.FindAsync([scenarioId], ct)
            ?? throw new InvalidOperationException($"Scenario {scenarioId} not found");

        scenario.MarkEvaluating();

        var signals = await _aggregator.AggregateAsync(scenario.TenantId, ct);

        ScenarioConstraints? constraints = null;
        try
        {
            constraints = JsonSerializer.Deserialize<ScenarioConstraints>(scenario.ConstraintsJson);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize constraints for scenario {ScenarioId}", scenarioId);
        }

        var coverageScore = 100m - signals.CoverageFragilityScore;
        var overtimeRate = signals.BurnoutPressureScore / 100m * 20m;
        var estimatedCost = signals.CriticalFragilitySites * 150_000m + signals.TalentRiskCriticalCount * 50_000m;

        var meetsCoverage = constraints is null || coverageScore >= constraints.MinCoveragePercent;
        var meetsOvertime = constraints is null || overtimeRate <= constraints.MaxOvertimePercent;
        var openViolations = signals.OpenCriticalViolations + signals.OpenHighViolations;
        var meetsCompliance = constraints is null || openViolations <= constraints.MaxComplianceViolations;
        var overallFeasible = meetsCoverage && meetsOvertime && meetsCompliance;

        var notes = new List<string>();
        if (!meetsCoverage) notes.Add($"Coverage score {coverageScore:F1} is below required minimum {constraints?.MinCoveragePercent}");
        if (!meetsOvertime) notes.Add($"Estimated overtime rate {overtimeRate:F1}% exceeds maximum {constraints?.MaxOvertimePercent}%");
        if (!meetsCompliance) notes.Add($"Open violations ({openViolations}) exceed allowed maximum {constraints?.MaxComplianceViolations}");

        var resultJson = JsonSerializer.Serialize(new
        {
            MeetsCoverage = meetsCoverage,
            MeetsOvertime = meetsOvertime,
            MeetsCompliance = meetsCompliance,
            OverallFeasible = overallFeasible,
            Notes = notes
        });

        scenario.MarkEvaluated(resultJson, (decimal)(double)coverageScore, (decimal)(double)overtimeRate, (decimal)(double)estimatedCost);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Evaluated scenario {ScenarioId}: feasible={Feasible}", scenarioId, overallFeasible);
        return scenario;
    }

    public async Task<WorkforceScenario?> FindOptimalScenarioAsync(string tenantId, CancellationToken ct = default)
    {
        var evaluated = await _db.WorkforceScenarios
            .Where(x => x.TenantId == tenantId && x.Status == ScenarioStatus.Evaluated)
            .ToListAsync(ct);

        if (evaluated.Count == 0)
            return null;

        // Clear any previous optimal
        var previousOptimal = await _db.WorkforceScenarios
            .Where(x => x.TenantId == tenantId && x.IsOptimal)
            .ToListAsync(ct);

        foreach (var prev in previousOptimal)
            prev.MarkEvaluated(prev.ResultJson ?? string.Empty, prev.CoverageScore ?? 0m, prev.OvertimeRate ?? 0m, prev.EstimatedCost ?? 0m);

        WorkforceScenario? best = null;
        double bestScore = double.MinValue;

        foreach (var scenario in evaluated)
        {
            var coverage = (double)(scenario.CoverageScore ?? 0m);
            var ot = (double)(scenario.OvertimeRate ?? 0m);
            var complianceBonus = ot < 10.0 ? 100.0 : 50.0;
            var score = coverage * 0.4 + (100.0 - ot * 100.0) * 0.3 + complianceBonus * 0.3;

            if (score > bestScore)
            {
                bestScore = score;
                best = scenario;
            }
        }

        best?.MarkOptimal();
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Optimal scenario for tenant {TenantId}: {ScenarioId}", tenantId, best?.Id);
        return best;
    }

    private sealed record ScenarioConstraints(
        decimal MinCoveragePercent,
        decimal MaxOvertimePercent,
        int MaxComplianceViolations);
}
