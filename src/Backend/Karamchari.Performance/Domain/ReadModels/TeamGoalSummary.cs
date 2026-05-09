using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Performance.Domain.ReadModels;

/// <summary>
/// Aggregated goal completion summary per reporting unit per goal cycle.
/// One row per (manager, cycle). Refreshed whenever any team member's goal changes.
/// </summary>
public sealed class TeamGoalSummary : Entity<Guid>, ITenantOwned
{
    private TeamGoalSummary() { /* EF materialization */ }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid ManagerEmployeeId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid GoalCycleId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string CycleName { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Department { get; private set; } = string.Empty;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int TotalGoals { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int CompletedGoals { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int InProgressGoals { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int OffTrackGoals { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int NotStartedGoals { get; private set; }

    /// <summary>0â€“100 percentage.</summary>
    public decimal CompletionRate { get; private set; }

    /// <summary>Average goal progress across all team goals (0â€“100).</summary>
    public decimal AverageProgress { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int TeamSize { get; private set; }

    /// <summary>Number of employees with no goals set for this cycle.</summary>
    public int EmployeesWithNoGoals { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset LastRefreshedUtc { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static TeamGoalSummary Create(
        string tenantId,
        Guid managerEmployeeId,
        Guid goalCycleId,
        string cycleName,
        string department)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(cycleName);
        ArgumentException.ThrowIfNullOrWhiteSpace(department);
        return new TeamGoalSummary
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ManagerEmployeeId = managerEmployeeId,
            GoalCycleId = goalCycleId,
            CycleName = cycleName,
            Department = department,
            LastRefreshedUtc = DateTimeOffset.UtcNow,
        };
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Refresh(
        int totalGoals, int completedGoals, int inProgressGoals,
        int offTrackGoals, int notStartedGoals,
        decimal completionRate, decimal averageProgress,
        int teamSize, int employeesWithNoGoals)
    {
        TotalGoals = totalGoals;
        CompletedGoals = completedGoals;
        InProgressGoals = inProgressGoals;
        OffTrackGoals = offTrackGoals;
        NotStartedGoals = notStartedGoals;
        CompletionRate = completionRate;
        AverageProgress = averageProgress;
        TeamSize = teamSize;
        EmployeesWithNoGoals = employeesWithNoGoals;
        LastRefreshedUtc = DateTimeOffset.UtcNow;
    }
}
