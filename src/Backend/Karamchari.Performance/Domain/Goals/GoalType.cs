namespace Karamchari.Performance.Domain.Goals;

public enum GoalType
{
    /// <summary>Numeric target with a unit (e.g., "close 50 deals").</summary>
    Measurable = 1,
    /// <summary>Discrete milestone checklist.</summary>
    Milestone = 2,
    /// <summary>Binary done/not-done.</summary>
    Binary = 3
}
