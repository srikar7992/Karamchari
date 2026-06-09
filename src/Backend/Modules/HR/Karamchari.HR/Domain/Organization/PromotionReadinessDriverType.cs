namespace Karamchari.HR.Domain.Organization;

/// <summary>
/// Specifies the type of driver contributing to an employee's promotion readiness score.
/// </summary>
public enum PromotionReadinessDriverType
{
    /// <summary>Employee is calibrated as a high performer in the recent performance cycle.</summary>
    HighGoalVelocity,
    /// <summary>Employee holds verified skills meeting or exceeding the development benchmark.</summary>
    SkillMilestonesMet,
    /// <summary>Employee role tenure meets or exceeds thedevelopment window milestone.</summary>
    RoleTenureMilestone
}

