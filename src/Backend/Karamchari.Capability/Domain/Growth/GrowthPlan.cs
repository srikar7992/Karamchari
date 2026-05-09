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

    public string TenantId { get; private set; } = string.Empty;
    public Guid EmployeeId { get; private set; }
    public Guid? MentorId { get; private set; } // Optional mentor assignment
    public string TargetRoleOrCapability { get; private set; } = string.Empty;

    public GrowthPlanStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? TargetCompletionUtc { get; private set; }
    
    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<GrowthMilestone> Milestones => _milestones.AsReadOnly();

    private GrowthPlan() { }

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

    public void Activate()
    {
        if (Status != GrowthPlanStatus.Draft && Status != GrowthPlanStatus.OnHold)
            throw new InvalidOperationException($"Cannot activate growth plan from state {Status}.");

        Status = GrowthPlanStatus.Active;
        // Raise GrowthPlanActivatedEvent
    }

    public void AddMilestone(string description, Guid? requiredSkillId = null)
    {
        if (Status == GrowthPlanStatus.Completed || Status == GrowthPlanStatus.Abandoned)
            throw new InvalidOperationException($"Cannot add milestones to a {Status} plan.");

        _milestones.Add(GrowthMilestone.Create(Id, description, requiredSkillId));
    }
}

public sealed class GrowthMilestone : Entity<Guid>
{
    public Guid GrowthPlanId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public Guid? RequiredSkillId { get; private set; }
    public bool IsAchieved { get; private set; }
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
