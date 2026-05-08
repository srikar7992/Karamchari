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

    public string TenantId { get; private set; } = string.Empty;
    public Guid ManagerEmployeeId { get; private set; }
    public Guid GoalCycleId { get; private set; }
    public string CycleName { get; private set; } = string.Empty;
    public string Department { get; private set; } = string.Empty;

    public int TotalGoals { get; private set; }
    public int CompletedGoals { get; private set; }
    public int InProgressGoals { get; private set; }
    public int OffTrackGoals { get; private set; }
    public int NotStartedGoals { get; private set; }

    /// <summary>0–100 percentage.</summary>
    public decimal CompletionRate { get; private set; }

    /// <summary>Average goal progress across all team goals (0–100).</summary>
    public decimal AverageProgress { get; private set; }

    public int TeamSize { get; private set; }

    /// <summary>Number of employees with no goals set for this cycle.</summary>
    public int EmployeesWithNoGoals { get; private set; }

    public DateTimeOffset LastRefreshedUtc { get; private set; }

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
