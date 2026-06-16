// -----------------------------------------------------------------------
// <copyright file="DomainDataSeeder.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Multitenancy;
using Karamchari.Core.Multitenancy.Execution;
using Karamchari.HR.Contracts.Employees;
using Karamchari.HR.Domain.Departments;
using Karamchari.HR.Domain.Organization;
using Karamchari.HR.Persistence;
using Karamchari.Onboarding.Domain;
using Karamchari.Onboarding.Persistence;
using Karamchari.TimeAttendance.Domain.Attendance;
using Karamchari.TimeAttendance.Domain.Shifts;
using Karamchari.TimeAttendance.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Karamchari.Api.Seeding;

/// <summary>
/// Seeds foundational DOMAIN data into each local tenant so the wired frontend
/// surfaces have live, non-empty records to reason over. <see cref="DevDataSeeder"/>
/// only creates login identities; this seeder creates the tenant-scoped business
/// data those identities operate on.
///
/// Currently seeds:
///   • HR employees (8 per tenant)
///   • TimeAttendance: a default shift policy, two fixed shifts, and ~10 recent
///     working-day attendance records per employee (mixed Present/Late/Absent).
///
/// Tenant context: there is no HttpContext during provisioning, so we establish an
/// ambient <see cref="TenantExecutionContext"/> per tenant. The schema-rewrite and
/// RLS-session interceptors read that ambient envelope (see <c>HttpTenantProvider</c>),
/// so writes land in the correct <c>tenant_&lt;id&gt;</c> schema and pass RLS.
///
/// All writes create fresh aggregates (<c>Add</c>), so they are unaffected by the
/// load-then-add-owned-child write defect noted for the asset lifecycle.
///
/// Idempotent per domain: skips a domain that already has rows for the tenant.
/// </summary>
public static class DomainDataSeeder
{
    private static readonly string[] Tenants = { "dev", "acme", "contoso", "globex" };

    // (EmployeeNumber, LegalName, email local-part). HiredOn is staggered below.
    private static readonly (string Number, string Name, string EmailLocal)[] Employees =
    {
        ("EMP-1001", "Asha Venkatesan", "asha.venkatesan"),
        ("EMP-1002", "Rajesh Patel", "rajesh.patel"),
        ("EMP-1003", "Priya Sharma", "priya.sharma"),
        ("EMP-1004", "Suresh Kumar", "suresh.kumar"),
        ("EMP-1005", "Meera Krishnan", "meera.krishnan"),
        ("EMP-1006", "David Patel", "david.patel"),
        ("EMP-1007", "Anita Desai", "anita.desai"),
        ("EMP-1008", "Tomas Chen", "tomas.chen"),
    };

    public static async Task SeedAsync(IServiceProvider rootServices, ILogger logger)
    {
        foreach (var tenant in Tenants)
        {
            var envelope = new TenantExecutionEnvelope(
                tenant,
                Guid.NewGuid().ToString("N"),
                Guid.NewGuid().ToString("N"),
                ExecutionSource.BackgroundJob,
                TenantSource.Background);

            var context = new TenantExecutionContext(envelope);
            using (context.Establish())
            {
                using var scope = rootServices.CreateScope();
                var sp = scope.ServiceProvider;

                await SeedEmployeesAsync(sp, tenant, logger);
                await SeedEmployeeHistoryAsync(sp, tenant, logger);
                await SeedOrgMasterDataAsync(sp, tenant, logger);
                await SeedAttendanceAsync(sp, tenant, logger);
                await SeedOnboardingAsync(sp, tenant, logger);
            }
        }
    }

    private static async Task SeedEmployeesAsync(IServiceProvider sp, string tenant, ILogger logger)
    {
        var hr = sp.GetRequiredService<HRDbContext>();
        if (await hr.Employees.AnyAsync())
        {
            logger.LogInformation("DomainDataSeeder: tenant '{Tenant}' already has employees; skipping HR.", tenant);
            return;
        }

        var employeeService = sp.GetRequiredService<IEmployeeService>();
        var hiredOn = new DateOnly(2024, 1, 8);

        foreach (var (number, name, emailLocal) in Employees)
        {
            await employeeService.OnboardEmployeeAsync(
                new OnboardEmployeeCommand(number, name, $"{emailLocal}@{tenant}.local", hiredOn),
                CancellationToken.None);
            hiredOn = hiredOn.AddMonths(2);
        }

        logger.LogInformation("DomainDataSeeder: seeded {Count} employees into tenant '{Tenant}'.", Employees.Length, tenant);
    }

    // Assigns EMP-1001 as reporting manager for the rest, which appends a
    // ManagerChange history entry to each — giving Employee360's immutable-record
    // chapter live data whose values resolve to a real employee (the manager).
    private static async Task SeedEmployeeHistoryAsync(IServiceProvider sp, string tenant, ILogger logger)
    {
        var hr = sp.GetRequiredService<HRDbContext>();
        var employees = await hr.Employees
            .Include(e => e.History)
            .OrderBy(e => e.EmployeeNumber)
            .ToListAsync();

        if (employees.Count < 2 || employees.Any(e => e.History.Count > 0))
        {
            logger.LogInformation("DomainDataSeeder: tenant '{Tenant}' history already seeded or too few employees; skipping.", tenant);
            return;
        }

        var managerId = employees[0].Id;
        var effectiveFrom = new DateTimeOffset(2024, 2, 1, 0, 0, 0, TimeSpan.Zero);
        for (var i = 1; i < employees.Count; i++)
        {
            employees[i].ChangeManager(managerId, effectiveFrom, managerId);
        }

        await hr.SaveChangesAsync();
        logger.LogInformation("DomainDataSeeder: seeded reporting-manager history for {Count} employees in tenant '{Tenant}'.", employees.Count - 1, tenant);
    }

    // Seeds organizational master data (departments, designations, branches) and
    // assigns each employee a department + designation + branch — filling the org
    // names the employee biography surface shows, and adding the corresponding
    // history entries. Master entities already exist in the model (provisioned);
    // this only populates rows.
    private static async Task SeedOrgMasterDataAsync(IServiceProvider sp, string tenant, ILogger logger)
    {
        var hr = sp.GetRequiredService<HRDbContext>();
        if (await hr.Departments.AnyAsync())
        {
            logger.LogInformation("DomainDataSeeder: tenant '{Tenant}' already has org master data; skipping.", tenant);
            return;
        }

        var departments = new[]
        {
            Department.Create("Core Infrastructure", "Platform and infrastructure engineering"),
            Department.Create("Zone Operations", "Field and zone operations"),
            Department.Create("Finance & Payroll", "Financial operations and payroll"),
        };
        hr.Departments.AddRange(departments);

        var designations = new[]
        {
            Designation.Create(tenant, "Engineer", "ENG", 3),
            Designation.Create(tenant, "Senior Engineer", "SR-ENG", 4),
            Designation.Create(tenant, "Manager", "MGR", 5),
            Designation.Create(tenant, "Director", "DIR", 6),
        };
        hr.Designations.AddRange(designations);

        var branches = new[]
        {
            Branch.Create(tenant, "Hyderabad East", "HYD-E", "Asia/Kolkata"),
            Branch.Create(tenant, "Zurich Node", "ZRH", "Europe/Zurich"),
        };
        hr.Branches.AddRange(branches);

        var employees = await hr.Employees
            .Include(e => e.History)
            .OrderBy(e => e.EmployeeNumber)
            .ToListAsync();

        var effectiveFrom = new DateTimeOffset(2024, 3, 1, 0, 0, 0, TimeSpan.Zero);
        var changedBy = employees.Count > 0 ? employees[0].Id : Guid.Empty;

        for (var i = 0; i < employees.Count; i++)
        {
            var e = employees[i];
            e.TransferToDepartment(departments[i % departments.Length].Id, effectiveFrom, changedBy);
            // First employee (the manager) is the Director; the rest spread across the lower ranks.
            var designation = i == 0 ? designations[3] : designations[i % 3];
            e.ChangeDesignation(designation.Id, effectiveFrom, changedBy);
            e.TransferToBranch(branches[i % branches.Length].Id, effectiveFrom, changedBy);
        }

        await hr.SaveChangesAsync();
        logger.LogInformation(
            "DomainDataSeeder: seeded {Depts} departments, {Desigs} designations, {Branches} branches and assigned {Employees} employees in tenant '{Tenant}'.",
            departments.Length, designations.Length, branches.Length, employees.Count, tenant);
    }

    // Seeds the onboarding journey: one reusable "Standard New Hire" template, then three
    // cases derived from it for seeded employees, in deliberately different states so the
    // case surface has live data to render:
    //   • InProgress — partway through (a few tasks done, mixed document states)
    //   • InProgress — just started (no tasks done yet)
    //   • Completed  — every task done (auto-completes via the aggregate)
    // All writes create fresh aggregates (Add), so the asset load-then-add-owned-child
    // write defect does not apply. OnboardingCase raises domain events handled by the
    // DomainEventDispatchInterceptor on SaveChangesAsync (no running bus required).
    private static async Task SeedOnboardingAsync(IServiceProvider sp, string tenant, ILogger logger)
    {
        var ob = sp.GetRequiredService<OnboardingDbContext>();
        if (await ob.Cases.AnyAsync())
        {
            logger.LogInformation("DomainDataSeeder: tenant '{Tenant}' already has onboarding cases; skipping.", tenant);
            return;
        }

        var hr = sp.GetRequiredService<HRDbContext>();
        var employees = await hr.Employees
            .OrderBy(e => e.EmployeeNumber)
            .Select(e => new { e.Id, e.LegalName })
            .ToListAsync();

        if (employees.Count < 4)
        {
            logger.LogWarning("DomainDataSeeder: tenant '{Tenant}' has too few employees for onboarding; skipping.", tenant);
            return;
        }

        var managerId = employees[0].Id; // the Director, used as the actor completing/verifying.

        // Reusable template with tasks spanning the cross-functional checklist.
        var template = OnboardingTemplate.Create(tenant, "Standard New Hire Onboarding", OnboardingCaseType.NewHire);
        template.AddTask("Sign employment contract", TaskCategory.Legal, 0, "Counter-signed copy filed.");
        template.AddTask("Complete HR documentation", TaskCategory.HR, 1, "Personal details, emergency contacts.");
        template.AddTask("Provision laptop and accounts", TaskCategory.IT, 1, "Hardware + SSO + email.");
        template.AddTask("Desk and access card setup", TaskCategory.Facilities, 2, "Seat allocation, badge.");
        template.AddTask("Payroll bank setup", TaskCategory.Finance, 2, "Salary account verification.");
        template.AddTask("Manager intro and goal-setting", TaskCategory.Manager, 3, "First 30-day objectives.");
        ob.Templates.Add(template);

        var startDate = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

        // (employee index, role, department, how many tasks to complete from the front).
        var plans = new[]
        {
            (Index: 1, Role: "Senior Engineer", Dept: "Zone Operations",   Complete: 3),
            (Index: 2, Role: "Manager",         Dept: "Finance & Payroll", Complete: 0),
            (Index: 3, Role: "Engineer",        Dept: "Core Infrastructure", Complete: 6),
        };

        foreach (var plan in plans)
        {
            var emp = employees[plan.Index];
            var @case = OnboardingCase.Initiate(
                tenant, emp.Id, emp.LegalName, OnboardingCaseType.NewHire,
                startDate, plan.Dept, plan.Role, template.Id);

            template.ApplyTo(@case);
            @case.Start();

            // Complete the first N tasks; if all are completed the aggregate auto-completes.
            var orderedTasks = @case.Tasks.ToList();
            for (var i = 0; i < plan.Complete && i < orderedTasks.Count; i++)
                @case.CompleteTask(orderedTasks[i].Id, managerId, "Seeded as complete.");

            // Documentation ledger: vary states so verified + pending both render.
            var idProof = @case.AddRequiredDocument(DocumentType.IDProof);
            @case.SubmitDocument(idProof.Id, "id-proof.pdf", $"seed://{tenant}/{emp.Id}/id-proof");
            @case.VerifyDocument(idProof.Id, managerId.ToString());

            var bank = @case.AddRequiredDocument(DocumentType.BankDetails);
            if (plan.Complete > 0)
            {
                @case.SubmitDocument(bank.Id, "bank-details.pdf", $"seed://{tenant}/{emp.Id}/bank");
                @case.VerifyDocument(bank.Id, managerId.ToString());
            }

            // A still-pending document so the ledger shows a live "pending" row.
            var nda = @case.AddRequiredDocument(DocumentType.NDAAgreement);
            if (plan.Complete >= 6)
            {
                @case.SubmitDocument(nda.Id, "nda.pdf", $"seed://{tenant}/{emp.Id}/nda");
                @case.VerifyDocument(nda.Id, managerId.ToString());
            }

            ob.Cases.Add(@case);
        }

        await ob.SaveChangesAsync();
        logger.LogInformation(
            "DomainDataSeeder: seeded 1 onboarding template + {Cases} cases into tenant '{Tenant}'.",
            plans.Length, tenant);
    }

    private static async Task SeedAttendanceAsync(IServiceProvider sp, string tenant, ILogger logger)
    {
        var ta = sp.GetRequiredService<TimeAttendanceDbContext>();
        if (await ta.ShiftDefinitions.AnyAsync())
        {
            logger.LogInformation("DomainDataSeeder: tenant '{Tenant}' already has shifts; skipping TimeAttendance.", tenant);
            return;
        }

        var hr = sp.GetRequiredService<HRDbContext>();
        var employeeIds = await hr.Employees.Select(e => e.Id).ToListAsync();
        if (employeeIds.Count == 0)
        {
            logger.LogWarning("DomainDataSeeder: tenant '{Tenant}' has no employees; skipping attendance.", tenant);
            return;
        }

        // Default policy + two fixed shifts.
        ta.AttendanceShiftPolicies.Add(AttendanceShiftPolicy.CreateDefault(tenant));
        var dayShift = ShiftDefinition.CreateFixed(tenant, "General Day", "DAY", new TimeOnly(9, 0), new TimeOnly(18, 0));
        var eveningShift = ShiftDefinition.CreateFixed(tenant, "Evening", "EVE", new TimeOnly(14, 0), new TimeOnly(23, 0));
        ta.ShiftDefinitions.AddRange(dayShift, eveningShift);

        // The Workforce_AttendanceRecords table has a unique index on
        // (TenantId, EmployeeId, ShiftId) — at most one record per employee per
        // shift — so we seed one record per shift per employee (two shifts → two
        // records). Distinct recent working days (skip weekends) per shift.
        var workdays = new List<DateOnly>();
        var cursor = DateOnly.FromDateTime(DateTime.UtcNow);
        while (workdays.Count < 2)
        {
            if (cursor.DayOfWeek != DayOfWeek.Saturday && cursor.DayOfWeek != DayOfWeek.Sunday)
                workdays.Add(cursor);
            cursor = cursor.AddDays(-1);
        }

        var shifts = new[] { dayShift, eveningShift };
        var records = 0;
        for (var ei = 0; ei < employeeIds.Count; ei++)
        {
            for (var si = 0; si < shifts.Length; si++)
            {
                var (status, worked, overtime) = ClassifyDay(ei, si);
                ta.AttendanceRecords.Add(AttendanceRecord.FinalizeLegacy(
                    tenant, employeeIds[ei], workdays[si], shifts[si].Id, status, worked, overtime));
                records++;
            }
        }

        await ta.SaveChangesAsync();
        logger.LogInformation(
            "DomainDataSeeder: seeded 2 shifts + default policy + {Records} attendance records into tenant '{Tenant}'.",
            records, tenant);
    }

    // Deterministic status mix so the seeded register looks realistic without randomness.
    private static (AttendanceStatus Status, decimal Worked, decimal Overtime) ClassifyDay(int employeeIndex, int dayIndex)
    {
        var bucket = (employeeIndex * 31 + dayIndex) % 12;
        return bucket switch
        {
            0 => (AttendanceStatus.Absent, 0m, 0m),
            1 => (AttendanceStatus.Late, 7.5m, 0m),
            2 => (AttendanceStatus.HalfDay, 4m, 0m),
            3 => (AttendanceStatus.Present, 9m, 1m),
            _ => (AttendanceStatus.Present, 8m, 0m),
        };
    }
}
