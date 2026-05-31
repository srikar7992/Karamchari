namespace Karamchari.Compensation.Domain;

/// <summary>Performance-linked merit rating used in merit matrix lookup.</summary>
public enum MeritRating
{
    /// <summary>Far exceeds expectations.</summary>
    Outstanding = 5,

    /// <summary>Consistently exceeds expectations.</summary>
    ExceedsExpectations = 4,

    /// <summary>Meets all expectations.</summary>
    MeetsExpectations = 3,

    /// <summary>Partially meets expectations; on improvement plan.</summary>
    BelowExpectations = 2,

    /// <summary>Does not meet expectations.</summary>
    Unsatisfactory = 1,
}
