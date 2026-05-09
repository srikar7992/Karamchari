namespace Karamchari.Capability.Domain.Primitives;

public enum SkillLevel
{
    Novice,
    Intermediate,
    Advanced,
    Expert
}

public enum CompetencyRating
{
    NeedsImprovement,
    MeetsExpectations,
    ExceedsExpectations,
    RoleModel
}

public enum WorkforceReadinessLevel
{
    CriticalRisk,
    AtRisk,
    Ready,
    HighlyReady
}

public enum EnrollmentStatus
{
    Assigned,
    InProgress,
    Completed,
    Failed,
    Expired,
    Dropped
}

public enum CertificationStatus
{
    Active,
    ExpiringSoon,
    Expired,
    Revoked
}

public enum GrowthPlanStatus
{
    Draft,
    Active,
    OnHold,
    Completed,
    Abandoned
}

public sealed record ReadinessScore(decimal Value)
{
    public static ReadinessScore Create(decimal value)
    {
        if (value < 0 || value > 100)
            throw new ArgumentException("Readiness score must be between 0 and 100.");
        return new ReadinessScore(value);
    }
}
