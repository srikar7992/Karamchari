using Karamchari.HR.Contracts.Projections;
using Karamchari.HR.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.HR.Services;

/// <summary>
/// Provides the canonical EmployeeSummaryProjection platform-wide.
/// Implements Priority 5 Step 28.
/// </summary>
public sealed class EmployeeSummaryService
{
    private readonly HRDbContext _db;

    public EmployeeSummaryService(HRDbContext db)
    {
        _db = db;
    }

    public async Task<EmployeeSummaryProjection?> GetSummaryAsync(Guid employeeId, CancellationToken ct = default)
    {
        var employee = await _db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == employeeId, ct);

        if (employee == null) return null;

        string? departmentName = null;
        if (employee.DepartmentId.HasValue)
        {
            var dept = await _db.Departments.FindAsync([employee.DepartmentId], ct);
            departmentName = dept?.Name;
        }

        string? designationName = null;
        if (employee.DesignationId.HasValue)
        {
            var desig = await _db.Designations.FindAsync([employee.DesignationId], ct);
            designationName = desig?.Name;
        }

        string? managerName = null;
        if (employee.ReportingManagerId.HasValue)
        {
            var manager = await _db.Employees.FindAsync([employee.ReportingManagerId], ct);
            managerName = manager?.LegalName;
        }

        return new EmployeeSummaryProjection(
            employee.Id,
            employee.EmployeeNumber,
            employee.LegalName,
            employee.WorkEmail,
            employee.Status.ToString(),
            employee.DepartmentId,
            departmentName,
            employee.DesignationId,
            designationName,
            employee.ReportingManagerId,
            managerName,
            employee.TimeZoneId);
    }
}
