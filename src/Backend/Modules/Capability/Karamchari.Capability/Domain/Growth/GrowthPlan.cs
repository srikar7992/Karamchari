// -----------------------------------------------------------------------
// <copyright file="GrowthPlan.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Capability.Domain.Primitives;
using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Capability.Domain.Growth;

/// <summary>
/// Aggregate root orchestrating an employee's career progression and mentorship.
/// Protects against invisible career stagnation.
/// </summary>
public sealed class GrowthPlan : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<GrowthMilestone> _milestones = [];

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid EmployeeId { get; private set; }
    /// <inheritdoc/>
    public Guid? MentorId { get; private set; } // Optional mentor assignment
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TargetRoleOrCapability { get; private set; } = string.Empty;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public GrowthPlanStatus Status { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }
    /// <inheritdoc/>
    public DateTimeOffset? TargetCompletionUtc { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public IReadOnlyCollection<GrowthMilestone> Milestones => _milestones.AsReadOnly();

    private GrowthPlan() { }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static GrowthPlan Create(
        string tenantId,
        Guid employeeId,
        string targetRole,
        DateTimeOffset? targetCompletion = null,
        Guid? mentorId = null)
    {
        return new GrowthPlan
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            TargetRoleOrCapability = targetRole,
            TargetCompletionUtc = targetCompletion,
            MentorId = mentorId,
            Status = GrowthPlanStatus.Draft,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Activate()
    {
        if (Status != GrowthPlanStatus.Draft && Status != GrowthPlanStatus.OnHold)
            throw new InvalidOperationException($"Cannot activate growth plan from state {Status}.");

        Status = GrowthPlanStatus.Active;
        // Raise GrowthPlanActivatedEvent
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void AddMilestone(string description, Guid? requiredSkillId = null)
    {
        if (Status == GrowthPlanStatus.Completed || Status == GrowthPlanStatus.Abandoned)
            throw new InvalidOperationException($"Cannot add milestones to a {Status} plan.");

        _milestones.Add(GrowthMilestone.Create(Id, description, requiredSkillId));
    }
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed class GrowthMilestone : Entity<Guid>
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid GrowthPlanId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Description { get; private set; } = string.Empty;
    /// <inheritdoc/>
    public Guid? RequiredSkillId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool IsAchieved { get; private set; }
    /// <inheritdoc/>
    public DateTimeOffset? AchievedAtUtc { get; private set; }

    private GrowthMilestone() { }

    internal static GrowthMilestone Create(Guid planId, string description, Guid? requiredSkill)
    {
        return new GrowthMilestone
        {
            Id = Guid.NewGuid(),
            GrowthPlanId = planId,
            Description = description,
            RequiredSkillId = requiredSkill,
            IsAchieved = false
        };
    }
}
