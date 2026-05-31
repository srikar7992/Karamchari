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
