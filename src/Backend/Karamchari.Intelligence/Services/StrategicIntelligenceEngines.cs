using Karamchari.Intelligence.Domain.Signals;

namespace Karamchari.Intelligence.Services;

public interface IOrganizationalHealthEngine
{
    Task<OrganizationalHealthSignal> EvaluateHealthAsync(string tenantId, string orgUnitId, CancellationToken ct = default);
}

public interface IWorkforceRiskEngine
{
    Task<WorkforceRiskSignal> DetectRisksAsync(string tenantId, string subjectId, RiskCategory category, CancellationToken ct = default);
}

internal sealed class OrganizationalHealthEngine : IOrganizationalHealthEngine
{
    private readonly Persistence.IntelligenceDbContext _db;

    public OrganizationalHealthEngine(Persistence.IntelligenceDbContext db)
    {
        _db = db;
    }

    public async Task<OrganizationalHealthSignal> EvaluateHealthAsync(string tenantId, string orgUnitId, CancellationToken ct = default)
    {
        // 1. In a real scenario, this would aggregate multiple child IntelligenceSignals
        // like "BurnoutIndex", "StaffingLevel", "RecruitmentVelocity".
        
        // Mocking the aggregation logic for Phase 1F structural completeness
        var burnoutLevel = WorkforcePressureIndex.Create(4); 
        var staffingStress = WorkforcePressureIndex.Create(7);
        
        var contributors = new List<ContributingFactor>
        {
            new("High Overtime Pressure", 0.4m, EvidenceType.SystemCalculated, "Operational attendance logs show sustained 20% OT."),
            new("Hiring Velocity Lag", 0.3m, EvidenceType.SystemCalculated, "ATS data shows Time-to-Fill exceeding 60 days.")
        };

        var confidence = ConfidenceEvaluationEngine.Evaluate(contributors, DateTimeOffset.UtcNow.AddHours(-2), DateTimeOffset.UtcNow);
        
        var explanation = ScoreExplanation.Compile(
            contributors, 
            new List<PenalizingFactor> { new("Succession Gaps", 0.15m, "No identified successors for 3 critical roles in this unit.") },
            new List<MissingEvidence> { new("Employee Sentiment", "Engagement survey results are > 180 days old.") },
            "Organizational health is moderate but staffing stress is high due to recruitment delays.");

        var health = OrganizationalHealthSignal.Evaluate(
            tenantId, orgUnitId, 68.5m, burnoutLevel, staffingStress, confidence, explanation);

        _db.OrganizationalHealthSignals.Add(health);
        await _db.SaveChangesAsync(ct);

        return health;
    }
}

internal sealed class WorkforceRiskEngine : IWorkforceRiskEngine
{
    private readonly Persistence.IntelligenceDbContext _db;

    public WorkforceRiskEngine(Persistence.IntelligenceDbContext db)
    {
        _db = db;
    }

    public async Task<WorkforceRiskSignal> DetectRisksAsync(string tenantId, string subjectId, RiskCategory category, CancellationToken ct = default)
    {
        var severity = WorkforcePressureIndex.Create(8); // Critical example

        var contributors = new List<ContributingFactor>
        {
            new("Key Talent Flight Risk", 0.6m, EvidenceType.SystemCalculated, "Behavioral signals correlate with historical attrition patterns."),
            new("Critical Skill Concentration", 0.4m, EvidenceType.SystemCalculated, "Only one employee possesses the required capability for 'Core Infrastructure'.")
        };

        var confidence = ConfidenceEvaluationEngine.Evaluate(contributors, DateTimeOffset.UtcNow.AddMinutes(-30), DateTimeOffset.UtcNow);

        var explanation = ScoreExplanation.Compile(
            contributors,
            new List<PenalizingFactor>(),
            new List<MissingEvidence>(),
            $"Critical {category} risk detected for {subjectId} due to extreme talent concentration.");

        var risk = WorkforceRiskSignal.Detect(
            tenantId, subjectId, category, severity, confidence, explanation);

        _db.WorkforceRiskSignals.Add(risk);
        await _db.SaveChangesAsync(ct);

        return risk;
    }
}
