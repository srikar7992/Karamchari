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
    Critical = 4,    // no ready successor
}

public enum ReadinessLevel
{
    ReadyNow = 1,
    OneToTwoYears = 2,
    ThreeToFiveYears = 3,
    NotReady = 4,
}

public enum SuccessorStatus
{
    Active = 1,
    Withdrawn = 2,
    Promoted = 3,
}

// ── CriticalRole ─────────────────────────────────────────────────────────────
/// <summary>
/// Represents a role the tenant has designated as business-critical.
/// Risk level is auto-recomputed when successors change.
/// </summary>
public sealed class CriticalRole : AggregateRoot<CriticalRoleId>, ITenantOwned
{
    private CriticalRole() { TenantId = string.Empty; RoleTitle = string.Empty; }

    private CriticalRole(CriticalRoleId id, string tenantId, string roleTitle, string? department,
        string? currentIncumbentEmployeeId, string? rationale) : base(id)
    {
        TenantId = tenantId;
        RoleTitle = roleTitle;
        Department = department;
        CurrentIncumbentEmployeeId = currentIncumbentEmployeeId;
        Rationale = rationale;
        Risk = SuccessionRisk.Critical;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string TenantId { get; private set; }
    public string RoleTitle { get; private set; }
    public string? Department { get; private set; }
    public string? CurrentIncumbentEmployeeId { get; private set; }
    public string? Rationale { get; private set; }
    public SuccessionRisk Risk { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static CriticalRole Register(string tenantId, string roleTitle, string? department,
        string? currentIncumbentEmployeeId, string? rationale)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(roleTitle);
        return new CriticalRole(new CriticalRoleId(Guid.NewGuid()), tenantId, roleTitle,
            department, currentIncumbentEmployeeId, rationale);
    }

    public void UpdateIncumbent(string? employeeId)
    {
        CurrentIncumbentEmployeeId = employeeId;
    }

    public void Recompute(int readyNowCount, int oneToTwoCount)
    {
        Risk = (readyNowCount, oneToTwoCount) switch
        {
            ( >= 2, _) => SuccessionRisk.Low,
            (1, _) => SuccessionRisk.Medium,
            (0, >= 1) => SuccessionRisk.High,
            _ => SuccessionRisk.Critical,
        };
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
        PerformanceScore = performanceScore;  // 1-5
        PotentialScore = potentialScore;      // 1-5
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

    /// <summary>9-box grid position: x = Performance (1-3), y = Potential (1-3).</summary>
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
/// <summary>
/// Succession plan for a single critical role.
/// Tracks ordered list of successors with readiness and 9-box placement.
/// </summary>
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

    /// <summary>Bench strength: count of ReadyNow active successors.</summary>
    public int BenchStrength =>
        _candidates.Count(c => c.Status == SuccessorStatus.Active && c.Readiness == ReadinessLevel.ReadyNow);

    /// <summary>Overall succession risk for this role.</summary>
    public SuccessionRisk ComputeRisk()
    {
        var readyNow = _candidates.Count(c => c.Status == SuccessorStatus.Active && c.Readiness == ReadinessLevel.ReadyNow);
        var nearTerm = _candidates.Count(c => c.Status == SuccessorStatus.Active && c.Readiness == ReadinessLevel.OneToTwoYears);
        return (readyNow, nearTerm) switch
        {
            ( >= 2, _) => SuccessionRisk.Low,
            (1, _) => SuccessionRisk.Medium,
            (0, >= 1) => SuccessionRisk.High,
            _ => SuccessionRisk.Critical,
        };
    }
}
