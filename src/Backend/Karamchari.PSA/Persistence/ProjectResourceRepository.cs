namespace Karamchari.PSA.Persistence;

using Karamchari.PSA.Domain;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Repository for project resource (rate card) queries.
/// All rate lookups are time-bound to ensure historical accuracy.
/// </summary>
public sealed class ProjectResourceRepository
{
    private readonly PSADbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectResourceRepository"/> class.
    /// </summary>
    public ProjectResourceRepository(PSADbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);
        _db = db;
    }

    /// <summary>
    /// Fetches the rate card that was active on the given work date.
    /// Returns null if the employee was not assigned to the project on that date.
    /// </summary>
    /// <remarks>
    /// Rate cards are time-bound: EffectiveFrom &lt;= workDate &lt;= EffectiveTo (or null).
    /// If an employee had two consecutive rate cards (e.g., promoted mid-project),
    /// the one with the highest EffectiveFrom that is still &lt;= workDate is selected.
    /// </remarks>
    public async Task<ProjectResource?> GetActiveAssignmentAsync(
        Guid employeeId,
        Guid projectId,
        DateOnly workDate,
        CancellationToken ct = default)
    {
        return await _db.ProjectResources
            .Where(x =>
                x.EmployeeId == employeeId &&
                x.ProjectId == projectId &&
                x.EffectiveFrom <= workDate &&
                (x.EffectiveTo == null || x.EffectiveTo >= workDate))
            .OrderByDescending(x => x.EffectiveFrom)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>
    /// Returns all projects that an employee is currently assigned to (for UI dropdowns).
    /// Only returns assignments active on <paramref name="asOfDate"/>.
    /// </summary>
    public async Task<List<ClientProject>> GetAssignedProjectsAsync(
        Guid employeeId,
        DateOnly asOfDate,
        CancellationToken ct = default)
    {
        var assignedProjectIds = await _db.ProjectResources
            .Where(r =>
                r.EmployeeId == employeeId &&
                r.EffectiveFrom <= asOfDate &&
                (r.EffectiveTo == null || r.EffectiveTo >= asOfDate))
            .Select(r => r.ProjectId)
            .Distinct()
            .ToListAsync(ct);

        return await _db.Projects
            .Where(p => assignedProjectIds.Contains(p.Id) && p.IsActive)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Validates that an employee is assigned to a project on the given date.
    /// Throws if not — enforcing the non-negotiable rule that timesheets cannot
    /// reference projects the employee is not authorized to log against.
    /// </summary>
    public async Task ValidateAssignmentAsync(
        Guid employeeId,
        Guid projectId,
        DateOnly workDate,
        CancellationToken ct = default)
    {
        _ = await GetActiveAssignmentAsync(employeeId, projectId, workDate, ct)
            ?? throw new InvalidOperationException(
                $"Employee {employeeId} is not assigned to project {projectId} on {workDate:yyyy-MM-dd}. " +
                "Timesheet entry rejected.");
    }
}
