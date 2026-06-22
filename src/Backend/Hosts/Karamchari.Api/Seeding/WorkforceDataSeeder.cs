using Karamchari.Core.Multitenancy;
using Karamchari.Core.Multitenancy.Execution;
using Karamchari.HR.Domain.Departments;
using Karamchari.HR.Domain.Employees;
using Karamchari.HR.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Api.Seeding;

/// <summary>
/// Seeds representative workforce data (departments + employees) for each dev tenant.
/// Idempotent — skips any tenant that already has departments.
/// Calls ClearDomainEvents() before saving to prevent event dispatch in seed context.
/// </summary>
public static class WorkforceDataSeeder
{
    private static readonly string[] Tenants = ["dev", "acme", "contoso", "globex"];

    private static readonly string[] DepartmentNames =
    [
        "Engineering", "Human Resources", "Finance", "Operations",
        "Sales", "Customer Success", "Security & Compliance"
    ];

    private static readonly (string Name, string EmailPrefix, string Dept)[] EmployeeTemplates =
    [
        ("Arjun Sharma",     "arjun.sharma",     "Engineering"),
        ("Priya Nair",       "priya.nair",        "Human Resources"),
        ("Rohan Mehta",      "rohan.mehta",       "Finance"),
        ("Divya Krishnan",   "divya.krishnan",    "Operations"),
        ("Vikram Singh",     "vikram.singh",      "Sales"),
        ("Ananya Iyer",      "ananya.iyer",       "Customer Success"),
        ("Siddharth Rao",    "siddharth.rao",     "Engineering"),
        ("Meera Pillai",     "meera.pillai",      "Security & Compliance"),
        ("Rahul Gupta",      "rahul.gupta",       "Engineering"),
        ("Kavya Reddy",      "kavya.reddy",       "Human Resources"),
        ("Aditya Patel",     "aditya.patel",      "Finance"),
        ("Sneha Joshi",      "sneha.joshi",       "Sales"),
    ];

    public static async Task SeedAsync(IServiceProvider services, ILogger<Program> logger)
    {
        using var scope = services.CreateScope();
        var hrDb = scope.ServiceProvider.GetRequiredService<HRDbContext>();

        foreach (var tenant in Tenants)
        {
            var envelope = new TenantExecutionEnvelope(
                tenant,
                Guid.NewGuid().ToString("N"),
                Guid.NewGuid().ToString("N"),
                ExecutionSource.BackgroundJob,
                TenantSource.Background);
            var ctx = new TenantExecutionContext(envelope);
            using var tenantScope = ctx.Establish();

            var alreadySeeded = await hrDb.Departments
                .IgnoreQueryFilters()
                .AnyAsync(d => d.TenantId == tenant);

            if (alreadySeeded)
            {
                logger.LogInformation("WorkforceDataSeeder: {Tenant} already seeded — skipping.", tenant);
                continue;
            }

            var hireBase = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-2);
            var deptMap = new Dictionary<string, Department>(StringComparer.Ordinal);

            foreach (var name in DepartmentNames)
            {
                var dept = Department.Create(name, null);
                dept.ClearDomainEvents();
                hrDb.Departments.Add(dept);
                deptMap[name] = dept;
            }

            var empNum = 1;
            foreach (var (name, emailPrefix, deptName) in EmployeeTemplates)
            {
                var emp = Employee.Hire(
                    employeeNumber: $"{tenant.ToUpperInvariant()}-{empNum:D4}",
                    legalName: name,
                    workEmail: $"{emailPrefix}@{tenant}.local",
                    hiredOn: hireBase.AddDays(empNum * 7));

                if (deptMap.TryGetValue(deptName, out var dept))
                    emp.TransferToDepartment(dept.Id, DateTimeOffset.UtcNow, Guid.Empty);

                emp.ClearDomainEvents();
                hrDb.Employees.Add(emp);
                empNum++;
            }

            await hrDb.SaveChangesAsync();
            logger.LogInformation(
                "WorkforceDataSeeder: seeded {Depts} departments + {Emps} employees for '{Tenant}'.",
                DepartmentNames.Length, EmployeeTemplates.Length, tenant);
        }
    }
}
