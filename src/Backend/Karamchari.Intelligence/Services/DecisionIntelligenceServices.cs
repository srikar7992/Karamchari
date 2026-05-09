using Karamchari.Intelligence.Domain.Primitives;
using Karamchari.Intelligence.Domain.Signals;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Intelligence.Services;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public interface IExecutiveDecisionService
{
    /// <inheritdoc/>
    Task<ExecutiveInsight> GenerateInsightAsync(string tenantId, string title, string category, IEnumerable<Guid> signalIds, CancellationToken ct = default);
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public interface IWorkforcePlanningService
{
    /// <inheritdoc/>
    Task<StrategicWorkforceScenario> RunSimulationAsync(string tenantId, string name, string description, string parametersJson, string userId, CancellationToken ct = default);
}

internal sealed class ExecutiveDecisionService : IExecutiveDecisionService
{
    private readonly Persistence.IntelligenceDbContext _db;

    public ExecutiveDecisionService(Persistence.IntelligenceDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public async Task<ExecutiveInsight> GenerateInsightAsync(string tenantId, string title, string category, IEnumerable<Guid> signalIds, CancellationToken ct = default)
    {
        // 1. Fetch contributing signals
        var signals = await _db.IntelligenceSignals
            .Where(s => signalIds.Contains(s.Id))
            .ToListAsync(ct);

        // 2. Aggregate confidence
        var avgConfidence = signals.Count > 0 ? signals.Average(s => s.Confidence.Score) : 50m;
        var confidence = SignalConfidence.Create(avgConfidence, "Aggregated System Insight");

        // 3. Generate narrative (Placeholder for future LLM / Narrative Template Engine)
        var narrative = $"Based on {signals.Count} signals, there is a strategic {category} opportunity. Overall confidence is {confidence.Level}.";

        var insight = ExecutiveInsight.Generate(
            tenantId, title, narrative, category, "Medium", signalIds, confidence);

        _db.ExecutiveInsights.Add(insight);
        await _db.SaveChangesAsync(ct);

        return insight;
    }
}

internal sealed class WorkforcePlanningService : IWorkforcePlanningService
{
    private readonly Persistence.IntelligenceDbContext _db;

    public WorkforcePlanningService(Persistence.IntelligenceDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public async Task<StrategicWorkforceScenario> RunSimulationAsync(string tenantId, string name, string description, string parametersJson, string userId, CancellationToken ct = default)
    {
        var scenario = StrategicWorkforceScenario.Create(tenantId, name, description, parametersJson, userId);

        // Here, a separate simulation engine would consume these parameters 
        // and produce WorkforceForecast projections.

        _db.StrategicWorkforceScenarios.Add(scenario);
        await _db.SaveChangesAsync(ct);

        return scenario;
    }
}
