using Karamchari.HR.Persistence;
using Karamchari.Payroll.Data;
using Karamchari.TimeAttendance.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.DataMigration.Services;

/// <summary>
/// Resolves external identifiers (names, numbers) to internal domain IDs by querying
/// the appropriate bounded-context DbContexts.
/// </summary>
public sealed class ReferenceResolver : IReferenceResolver
{
    private readonly HRDbContext _hrDb;
    private readonly TimeAttendanceDbContext _taDb;
    private readonly PayrollDbContext _payrollDb;

    public ReferenceResolver(HRDbContext hrDb, TimeAttendanceDbContext taDb, PayrollDbContext payrollDb)
    {
        _hrDb = hrDb;
        _taDb = taDb;
        _payrollDb = payrollDb;
    }

    public async Task<Guid?> ResolveEmployeeIdAsync(string employeeNumber, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(employeeNumber)) return null;
        var emp = await _hrDb.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EmployeeNumber == employeeNumber, ct);
        return emp?.Id;
    }

    public async Task<string?> ResolveEmployeeNameAsync(Guid employeeId, CancellationToken ct = default)
    {
        var emp = await _hrDb.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == employeeId, ct);
        return emp?.LegalName;
    }

    public async Task<Guid?> ResolveDepartmentIdAsync(string departmentName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(departmentName)) return null;
        var dept = await _hrDb.Departments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Name == departmentName, ct);
        return dept?.Id;
    }

    public async Task<Guid?> ResolveLeavePolicyIdAsync(string policyName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(policyName)) return null;
        var policy = await _taDb.LeavePolicies
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Name == policyName, ct);
        return policy?.Id;
    }

    public async Task<Guid?> ResolveSalaryComponentIdAsync(string componentName, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(componentName)) return null;
        var component = await _payrollDb.SalaryComponents
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Name == componentName, ct);
        return component?.Id;
    }
}
