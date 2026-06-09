namespace Karamchari.Capability.Domain.Primitives;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum SkillLevel
{
    /// <inheritdoc/>
    Novice,
    /// <inheritdoc/>
    Intermediate,
    /// <inheritdoc/>
    Advanced,
    /// <inheritdoc/>
    Expert
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum CompetencyRating
{
    /// <inheritdoc/>
    NeedsImprovement,
    /// <inheritdoc/>
    MeetsExpectations,
    /// <inheritdoc/>
    ExceedsExpectations,
    /// <inheritdoc/>
    RoleModel
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum WorkforceReadinessLevel
{
    /// <inheritdoc/>
    CriticalRisk,
    /// <inheritdoc/>
    AtRisk,
    /// <inheritdoc/>
    Ready,
    /// <inheritdoc/>
    HighlyReady
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum EnrollmentStatus
{
    /// <inheritdoc/>
    Assigned,
    /// <inheritdoc/>
    InProgress,
    /// <inheritdoc/>
    Completed,
    /// <inheritdoc/>
    Failed,
    /// <inheritdoc/>
    Expired,
    /// <inheritdoc/>
    Dropped
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum CertificationStatus
{
    /// <inheritdoc/>
    Active,
    /// <inheritdoc/>
    ExpiringSoon,
    /// <inheritdoc/>
    Expired,
    /// <inheritdoc/>
    Revoked
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum GrowthPlanStatus
{
    /// <inheritdoc/>
    Draft,
    /// <inheritdoc/>
    Active,
    /// <inheritdoc/>
    OnHold,
    /// <inheritdoc/>
    Completed,
    /// <inheritdoc/>
    Abandoned
}

/// <summary>
/// Skill-based career readiness band for an employee against a specific role requirement.
/// Derived from coverage percentage and critical gap count — independent of succession planning readiness.
/// </summary>
public enum CareerReadinessBand
{
    /// <summary>Coverage >= 90% and no critical gaps. Employee qualifies now.</summary>
    ReadyNow,
    /// <summary>Coverage >= 70% with at most two critical gaps. Employee qualifies with targeted development.</summary>
    ReadySoon,
    /// <summary>Coverage below 70% or more than two critical gaps. Meaningful development needed.</summary>
    NeedsDevelopment,
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record ReadinessScore(decimal Value)
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static ReadinessScore Create(decimal value)
    {
        if (value < 0 || value > 100)
            throw new ArgumentException("Readiness score must be between 0 and 100.");
        return new ReadinessScore(value);
    }
}
