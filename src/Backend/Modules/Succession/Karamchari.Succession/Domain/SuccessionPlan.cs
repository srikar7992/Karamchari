using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.Succession.Domain.Events;

namespace Karamchari.Succession.Domain;

// ── Strong-typed IDs ─────────────────────────────────────────────────────────
public record CriticalRoleId(Guid Value);
public record SuccessionPlanId(Guid Value);
public record SuccessorCandidateId(Guid Value);

// ── Enums ─────────────────────────────────────────────────────────────────────
public enum SuccessionRisk
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4,
}

public enum ReadinessLevel
{
    ReadyNow = 1,
    ReadyWithinSixMonths = 2,
    ReadyWithinTwelveMonths = 3,
    ReadyBeyondTwelveMonths = 4,
    NotReady = 5,
}

public enum BenchStrengthRating
{
    Critical = 1,     // no successors at all
    Inadequate = 2,   // near-term successors exist but none ready now
    Adequate = 3,     // has ready-now but below required count
    Strong = 4,       // meets or exceeds required successor count
}

public enum SuccessorStatus
{
    Active = 1,
    Withdrawn = 2,
    Promoted = 3,
}

// ── CriticalRole ─────────────────────────────────────────────────────────────
public sealed class CriticalRole : AggregateRoot<CriticalRoleId>, ITenantOwned
{
    private CriticalRole() { TenantId = string.Empty; RoleTitle = string.Empty; }

    private CriticalRole(CriticalRoleId id, string tenantId, string roleTitle, string? department,
        string? currentIncumbentEmployeeId, string? rationale,
        int requiredSuccessors, SuccessionRisk retirementRisk, SuccessionRisk attritionRisk) : base(id)
    {
        TenantId = tenantId;
        RoleTitle = roleTitle;
        Department = department;
        CurrentIncumbentEmployeeId = currentIncumbentEmployeeId;
        Rationale = rationale;
        RequiredSuccessors = requiredSuccessors;
        IncumbentRetirementRisk = retirementRisk;
        IncumbentAttritionRisk = attritionRisk;
        Risk = SuccessionRisk.Critical;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string TenantId { get; private set; }
    public string RoleTitle { get; private set; }
    public string? Department { get; private set; }
    public string? CurrentIncumbentEmployeeId { get; private set; }
    public string? Rationale { get; private set; }
    public int RequiredSuccessors { get; private set; }
    public SuccessionRisk IncumbentRetirementRisk { get; private set; }
    public SuccessionRisk IncumbentAttritionRisk { get; private set; }
    public SuccessionRisk Risk { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static CriticalRole Register(string tenantId, string roleTitle, string? department,
        string? currentIncumbentEmployeeId, string? rationale,
        int requiredSuccessors = 1,
        SuccessionRisk retirementRisk = SuccessionRisk.Medium,
        SuccessionRisk attritionRisk = SuccessionRisk.Low)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(roleTitle);
        if (requiredSuccessors < 1) requiredSuccessors = 1;
        return new CriticalRole(new CriticalRoleId(Guid.NewGuid()), tenantId, roleTitle,
            department, currentIncumbentEmployeeId, rationale,
            requiredSuccessors, retirementRisk, attritionRisk);
    }

    public void UpdateIncumbent(string? employeeId)
    {
        CurrentIncumbentEmployeeId = employeeId;
    }

    public void UpdateRiskFactors(SuccessionRisk retirementRisk, SuccessionRisk attritionRisk, int requiredSuccessors)
    {
        IncumbentRetirementRisk = retirementRisk;
        IncumbentAttritionRisk = attritionRisk;
        RequiredSuccessors = requiredSuccessors < 1 ? 1 : requiredSuccessors;
    }

    /// <summary>Recomputes successor availability risk from plan candidate readiness.</summary>
    public void Recompute(int readyNowCount, int nearTermCount)
    {
        Risk = (readyNowCount, nearTermCount) switch
        {
            ( >= 2, _) => SuccessionRisk.Low,
            (1, _) => SuccessionRisk.Medium,
            (0, >= 1) => SuccessionRisk.High,
            _ => SuccessionRisk.Critical,
        };
    }

    /// <summary>Vacancy risk = worst of retirement risk, attrition risk, successor availability risk.</summary>
    public SuccessionRisk ComputeVacancyRisk()
    {
        var worstIncumbent = IncumbentRetirementRisk > IncumbentAttritionRisk
            ? IncumbentRetirementRisk
            : IncumbentAttritionRisk;
        return Risk > worstIncumbent ? Risk : worstIncumbent;
    }

    public void Deactivate() => IsActive = false;
}

// ── SuccessorCandidate ────────────────────────────────────────────────────────
public sealed class SuccessorCandidate : Entity<SuccessorCandidateId>
{
    private SuccessorCandidate() { DevelopmentGaps = []; }

    internal SuccessorCandidate(SuccessorCandidateId id, SuccessionPlanId planId,
        Guid employeeId, ReadinessLevel readiness, int performanceScore,
        int potentialScore, string? developmentNotes) : base(id)
    {
        PlanId = planId;
        EmployeeId = employeeId;
        Readiness = readiness;
        PerformanceScore = performanceScore;
        PotentialScore = potentialScore;
        DevelopmentNotes = developmentNotes;
        Status = SuccessorStatus.Active;
        AddedAt = DateTimeOffset.UtcNow;
        DevelopmentGaps = [];
    }

    public SuccessionPlanId PlanId { get; private set; } = null!;
    public Guid EmployeeId { get; private set; }
    public ReadinessLevel Readiness { get; private set; }
    public int PerformanceScore { get; private set; }
    public int PotentialScore { get; private set; }
    public string? DevelopmentNotes { get; private set; }
    public SuccessorStatus Status { get; private set; }
    public DateTimeOffset AddedAt { get; private set; }
    public List<string> DevelopmentGaps { get; private set; }

    public (int Performance, int Potential) NineBoxPosition()
    {
        static int Bucket(int score) => score switch { <= 2 => 1, 3 or 4 => 2, _ => 3 };
        return (Bucket(PerformanceScore), Bucket(PotentialScore));
    }

    internal void UpdateReadiness(ReadinessLevel readiness, string? notes)
    {
        Readiness = readiness;
        DevelopmentNotes = notes;
    }

    internal void AddGap(string gap)
    {
        if (!DevelopmentGaps.Contains(gap, StringComparer.OrdinalIgnoreCase))
            DevelopmentGaps.Add(gap);
    }

    internal void Promote() => Status = SuccessorStatus.Promoted;
    internal void Withdraw() => Status = SuccessorStatus.Withdrawn;
}

// ── SuccessionPlan ────────────────────────────────────────────────────────────
public sealed class SuccessionPlan : AggregateRoot<SuccessionPlanId>, ITenantOwned
{
    private readonly List<SuccessorCandidate> _candidates = [];

    private SuccessionPlan() { TenantId = string.Empty; }

    private SuccessionPlan(SuccessionPlanId id, string tenantId, CriticalRoleId roleId,
        Guid createdByEmployeeId) : base(id)
    {
        TenantId = tenantId;
        RoleId = roleId;
        CreatedByEmployeeId = createdByEmployeeId;
        CreatedAt = DateTimeOffset.UtcNow;
        LastReviewedAt = DateTimeOffset.UtcNow;
    }

    public string TenantId { get; private set; }
    public CriticalRoleId RoleId { get; private set; } = null!;
    public Guid CreatedByEmployeeId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset LastReviewedAt { get; private set; }
    public IReadOnlyList<SuccessorCandidate> Candidates => _candidates.AsReadOnly();

    public static SuccessionPlan Create(string tenantId, CriticalRoleId roleId, Guid createdByEmployeeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        return new SuccessionPlan(new SuccessionPlanId(Guid.NewGuid()), tenantId, roleId, createdByEmployeeId);
    }

    public SuccessorCandidate AddCandidate(Guid employeeId, ReadinessLevel readiness,
        int performanceScore, int potentialScore, string? notes)
    {
        if (_candidates.Any(c => c.EmployeeId == employeeId && c.Status == SuccessorStatus.Active))
            throw new InvalidOperationException("Employee already active candidate for this plan.");

        var candidate = new SuccessorCandidate(
            new SuccessorCandidateId(Guid.NewGuid()), Id, employeeId,
            readiness, performanceScore, potentialScore, notes);
        _candidates.Add(candidate);
        RaiseDomainEvent(new SuccessorAddedEvent(TenantId, Id, RoleId, employeeId, readiness));
        return candidate;
    }

    public void UpdateCandidateReadiness(SuccessorCandidateId candidateId,
        ReadinessLevel readiness, string? notes)
    {
        var c = _candidates.FirstOrDefault(x => x.Id == candidateId)
            ?? throw new InvalidOperationException("Candidate not found.");
        c.UpdateReadiness(readiness, notes);
        LastReviewedAt = DateTimeOffset.UtcNow;
    }

    public void AddDevelopmentGap(SuccessorCandidateId candidateId, string gap)
    {
        var c = _candidates.FirstOrDefault(x => x.Id == candidateId)
            ?? throw new InvalidOperationException("Candidate not found.");
        c.AddGap(gap);
    }

    public void WithdrawCandidate(SuccessorCandidateId candidateId)
    {
        var c = _candidates.FirstOrDefault(x => x.Id == candidateId)
            ?? throw new InvalidOperationException("Candidate not found.");
        c.Withdraw();
    }

    public void MarkCandidatePromoted(SuccessorCandidateId candidateId)
    {
        var c = _candidates.FirstOrDefault(x => x.Id == candidateId)
            ?? throw new InvalidOperationException("Candidate not found.");
        c.Promote();
        RaiseDomainEvent(new SuccessorPromotedEvent(TenantId, Id, RoleId, c.EmployeeId));
    }

    public void MarkReviewed() => LastReviewedAt = DateTimeOffset.UtcNow;

    /// <summary>Count of ReadyNow active successors.</summary>
    public int BenchStrength =>
        _candidates.Count(c => c.Status == SuccessorStatus.Active && c.Readiness == ReadinessLevel.ReadyNow);

    /// <summary>Bench strength rating vs required successor count.</summary>
    public BenchStrengthRating ComputeBenchStrengthRating(int requiredSuccessors)
    {
        var active = _candidates.Where(c => c.Status == SuccessorStatus.Active).ToList();
        var readyNow = active.Count(c => c.Readiness == ReadinessLevel.ReadyNow);
        var nearTerm = active.Count(c =>
            c.Readiness == ReadinessLevel.ReadyWithinSixMonths ||
            c.Readiness == ReadinessLevel.ReadyWithinTwelveMonths);

        if (readyNow == 0 && nearTerm == 0) return BenchStrengthRating.Critical;
        if (readyNow == 0) return BenchStrengthRating.Inadequate;
        if (readyNow >= requiredSuccessors) return BenchStrengthRating.Strong;
        return BenchStrengthRating.Adequate;
    }

    /// <summary>Readiness breakdown: count per level for active candidates.</summary>
    public ReadinessBreakdown GetReadinessBreakdown()
    {
        var active = _candidates.Where(c => c.Status == SuccessorStatus.Active).ToList();
        return new ReadinessBreakdown(
            ReadyNow: active.Count(c => c.Readiness == ReadinessLevel.ReadyNow),
            WithinSixMonths: active.Count(c => c.Readiness == ReadinessLevel.ReadyWithinSixMonths),
            WithinTwelveMonths: active.Count(c => c.Readiness == ReadinessLevel.ReadyWithinTwelveMonths),
            BeyondTwelveMonths: active.Count(c => c.Readiness == ReadinessLevel.ReadyBeyondTwelveMonths),
            NotReady: active.Count(c => c.Readiness == ReadinessLevel.NotReady),
            Total: active.Count);
    }

    public SuccessionRisk ComputeRisk()
    {
        var readyNow = _candidates.Count(c =>
            c.Status == SuccessorStatus.Active && c.Readiness == ReadinessLevel.ReadyNow);
        var nearTerm = _candidates.Count(c =>
            c.Status == SuccessorStatus.Active &&
            (c.Readiness == ReadinessLevel.ReadyWithinSixMonths ||
             c.Readiness == ReadinessLevel.ReadyWithinTwelveMonths));
        return (readyNow, nearTerm) switch
        {
            ( >= 2, _) => SuccessionRisk.Low,
            (1, _) => SuccessionRisk.Medium,
            (0, >= 1) => SuccessionRisk.High,
            _ => SuccessionRisk.Critical,
        };
    }
}

/// <summary>Per-role readiness distribution for executive dashboards.</summary>
public sealed record ReadinessBreakdown(
    int ReadyNow,
    int WithinSixMonths,
    int WithinTwelveMonths,
    int BeyondTwelveMonths,
    int NotReady,
    int Total);
