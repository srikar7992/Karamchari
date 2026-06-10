// -----------------------------------------------------------------------
// <copyright file="TalentPool.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Succession.Domain;

// ── Strong-typed ID ───────────────────────────────────────────────────────────
public record TalentPoolId(Guid Value);
public record TalentPoolMemberId(Guid Value);

public enum TalentPoolStatus
{
    Active = 1,
    Archived = 2,
}

public enum MembershipStatus
{
    Active = 1,
    Graduated = 2,   // moved to a senior role
    Exited = 3,
}

/// <summary>
/// An org-wide collection of high-potential employees tracked for future leadership roles.
/// Unlike SuccessionPlan (role-specific), TalentPool is aspirational — employees enter
/// before a specific role is identified for them.
/// </summary>
public sealed class TalentPool : AggregateRoot<TalentPoolId>, ITenantOwned
{
    private readonly List<TalentPoolMember> _members = [];

    private TalentPool() { TenantId = string.Empty; Name = string.Empty; }

    private TalentPool(
        TalentPoolId id,
        string tenantId,
        string name,
        string? description,
        string? targetJobFamily,
        Guid ownedByEmployeeId) : base(id)
    {
        TenantId = tenantId;
        Name = name;
        Description = description;
        TargetJobFamily = targetJobFamily;
        OwnedByEmployeeId = ownedByEmployeeId;
        Status = TalentPoolStatus.Active;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string TenantId { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public string? TargetJobFamily { get; private set; }
    public Guid OwnedByEmployeeId { get; private set; }
    public TalentPoolStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyList<TalentPoolMember> Members => _members.AsReadOnly();

    public static TalentPool Create(
        string tenantId,
        string name,
        string? description,
        string? targetJobFamily,
        Guid ownedByEmployeeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new TalentPool(
            new TalentPoolId(Guid.NewGuid()),
            tenantId, name, description, targetJobFamily, ownedByEmployeeId);
    }

    public TalentPoolMember AddMember(
        Guid employeeId,
        int performanceScore,
        int potentialScore,
        ReadinessLevel estimatedReadiness,
        string? nominationNotes)
    {
        if (_members.Any(m => m.EmployeeId == employeeId && m.Status == MembershipStatus.Active))
            throw new InvalidOperationException("Employee is already an active pool member.");

        var member = new TalentPoolMember(
            new TalentPoolMemberId(Guid.NewGuid()),
            Id, employeeId, performanceScore, potentialScore, estimatedReadiness, nominationNotes);
        _members.Add(member);
        return member;
    }

    public void UpdateMemberAssessment(
        TalentPoolMemberId memberId,
        int performanceScore,
        int potentialScore,
        ReadinessLevel readiness,
        string? notes)
    {
        var m = _members.FirstOrDefault(x => x.Id == memberId)
            ?? throw new InvalidOperationException("Member not found.");
        m.UpdateAssessment(performanceScore, potentialScore, readiness, notes);
    }

    public void GraduateMember(TalentPoolMemberId memberId)
    {
        var m = _members.FirstOrDefault(x => x.Id == memberId)
            ?? throw new InvalidOperationException("Member not found.");
        m.Graduate();
    }

    public void ExitMember(TalentPoolMemberId memberId, string reason)
    {
        var m = _members.FirstOrDefault(x => x.Id == memberId)
            ?? throw new InvalidOperationException("Member not found.");
        m.Exit(reason);
    }

    public void Archive() => Status = TalentPoolStatus.Archived;

    /// <summary>Active members sorted by NineBox position (high performers first).</summary>
    public IEnumerable<TalentPoolMember> RankedMembers =>
        _members
            .Where(m => m.Status == MembershipStatus.Active)
            .OrderByDescending(m => m.PotentialScore + m.PerformanceScore);
}

public sealed class TalentPoolMember : Entity<TalentPoolMemberId>
{
    private TalentPoolMember() { }

    internal TalentPoolMember(
        TalentPoolMemberId id,
        TalentPoolId poolId,
        Guid employeeId,
        int performanceScore,
        int potentialScore,
        ReadinessLevel estimatedReadiness,
        string? nominationNotes) : base(id)
    {
        PoolId = poolId;
        EmployeeId = employeeId;
        PerformanceScore = Clamp(performanceScore);
        PotentialScore = Clamp(potentialScore);
        EstimatedReadiness = estimatedReadiness;
        NominationNotes = nominationNotes;
        Status = MembershipStatus.Active;
        NominatedAt = DateTimeOffset.UtcNow;
    }

    public TalentPoolId PoolId { get; private set; } = null!;
    public Guid EmployeeId { get; private set; }
    public int PerformanceScore { get; private set; }    // 1-5
    public int PotentialScore { get; private set; }      // 1-5
    public ReadinessLevel EstimatedReadiness { get; private set; }
    public string? NominationNotes { get; private set; }
    public MembershipStatus Status { get; private set; }
    public DateTimeOffset NominatedAt { get; private set; }
    public DateTimeOffset? LastReviewedAt { get; private set; }
    public string? ExitReason { get; private set; }

    /// <summary>9-box grid position: Performance (1-3) x Potential (1-3).</summary>
    public (int Performance, int Potential) NineBoxPosition()
    {
        static int Bucket(int s) => s switch { <= 2 => 1, 3 or 4 => 2, _ => 3 };
        return (Bucket(PerformanceScore), Bucket(PotentialScore));
    }

    internal void UpdateAssessment(int perf, int pot, ReadinessLevel readiness, string? notes)
    {
        PerformanceScore = Clamp(perf);
        PotentialScore = Clamp(pot);
        EstimatedReadiness = readiness;
        NominationNotes = notes;
        LastReviewedAt = DateTimeOffset.UtcNow;
    }

    internal void Graduate()
    {
        Status = MembershipStatus.Graduated;
        LastReviewedAt = DateTimeOffset.UtcNow;
    }

    internal void Exit(string reason)
    {
        Status = MembershipStatus.Exited;
        ExitReason = reason;
        LastReviewedAt = DateTimeOffset.UtcNow;
    }

    private static int Clamp(int score) => Math.Clamp(score, 1, 5);
}
