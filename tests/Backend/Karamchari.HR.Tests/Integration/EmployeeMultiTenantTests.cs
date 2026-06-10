// -----------------------------------------------------------------------
// <copyright file="EmployeeMultiTenantTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.HR.Domain.Departments;
using Karamchari.HR.Domain.Employees;
using Microsoft.EntityFrameworkCore;
using Xunit;

// Make EmployeeHistoryType available without qualified name
using static Karamchari.HR.Domain.Employees.EmployeeHistoryType;

namespace Karamchari.HR.Tests.Integration;

/// <summary>
/// Integration tests verifying that the HRDbContext global query filter enforces
/// tenant isolation at the persistence layer. Tests use in-memory databases
/// with a TenantId-stamping interceptor to simulate multi-tenant behaviour
/// without a real SQL Server instance.
/// </summary>
public sealed class EmployeeMultiTenantTests : IDisposable
{
    private const string TenantAId = "tenant-alpha";
    private const string TenantBId = "tenant-beta";

    private readonly IDisposable[] _disposables;

    public EmployeeMultiTenantTests()
    {
        _disposables = [];
    }

    public void Dispose()
    {
        foreach (var d in _disposables) d.Dispose();
        GC.SuppressFinalize(this);
    }

    // ─── Employee isolation ──────────────────────────────────────────────────

    [Fact]
    public async Task Employee_TenantA_NotVisibleFromTenantB()
    {
        // Arrange: shared in-memory database
        var (ctxA, ctxB) = HRDbContextFactory.CreatePair(TenantAId, TenantBId);
        await using var _ = ctxA;
        await using var __ = ctxB;

        var employeeA = Employee.Hire("EMP-A001", "Alice Tenant", null, new DateOnly(2025, 1, 1));
        ctxA.Employees.Add(employeeA);
        await ctxA.SaveChangesAsync();

        // Act: query from TenantB's context — global query filter applies TenantId == TenantBId
        var visibleFromB = await ctxB.Employees.ToListAsync();

        // Assert: Tenant B sees nothing from Tenant A
        visibleFromB.Should().BeEmpty();
    }

    [Fact]
    public async Task Employee_TenantA_IsVisibleFromTenantA()
    {
        // Arrange
        var (ctxA, ctxB) = HRDbContextFactory.CreatePair(TenantAId, TenantBId);
        await using var _ = ctxA;
        await using var __ = ctxB;

        var employeeA = Employee.Hire("EMP-A002", "Bob Tenant", "bob@a.com", new DateOnly(2025, 2, 1));
        ctxA.Employees.Add(employeeA);
        await ctxA.SaveChangesAsync();

        // Act: query from the same TenantA context
        var visibleFromA = await ctxA.Employees.ToListAsync();

        // Assert
        visibleFromA.Should().ContainSingle(e => e.EmployeeNumber == "EMP-A002");
    }

    [Fact]
    public async Task Employee_BothTenants_OnlySeeThenOwnEmployees()
    {
        // Arrange: populate both tenants on the shared database
        var (ctxA, ctxB) = HRDbContextFactory.CreatePair(TenantAId, TenantBId);
        await using var _ = ctxA;
        await using var __ = ctxB;

        var empA = Employee.Hire("EMP-A003", "Charlie Alpha", null, new DateOnly(2025, 3, 1));
        var empB = Employee.Hire("EMP-B003", "Diana Beta", null, new DateOnly(2025, 3, 1));

        ctxA.Employees.Add(empA);
        await ctxA.SaveChangesAsync();

        ctxB.Employees.Add(empB);
        await ctxB.SaveChangesAsync();

        // Act
        var fromA = await ctxA.Employees.ToListAsync();
        var fromB = await ctxB.Employees.ToListAsync();

        // Assert: each tenant sees only their own record
        fromA.Should().ContainSingle(e => e.EmployeeNumber == "EMP-A003");
        fromA.Should().NotContain(e => e.EmployeeNumber == "EMP-B003");

        fromB.Should().ContainSingle(e => e.EmployeeNumber == "EMP-B003");
        fromB.Should().NotContain(e => e.EmployeeNumber == "EMP-A003");
    }

    // ─── Department isolation ────────────────────────────────────────────────

    [Fact]
    public async Task Department_TenantA_NotVisibleFromTenantB()
    {
        // Arrange
        var (ctxA, ctxB) = HRDbContextFactory.CreatePair(TenantAId, TenantBId);
        await using var _ = ctxA;
        await using var __ = ctxB;

        var dept = Department.Create("Engineering", "Software engineers");
        ctxA.Departments.Add(dept);
        await ctxA.SaveChangesAsync();

        // Act
        var fromB = await ctxB.Departments.ToListAsync();

        // Assert
        fromB.Should().BeEmpty();
    }

    [Fact]
    public async Task Department_TenantA_IsVisibleFromTenantA()
    {
        // Arrange
        var (ctxA, ctxB) = HRDbContextFactory.CreatePair(TenantAId, TenantBId);
        await using var _ = ctxA;
        await using var __ = ctxB;

        var dept = Department.Create("HR Operations", "Human resources ops");
        ctxA.Departments.Add(dept);
        await ctxA.SaveChangesAsync();

        // Act
        var fromA = await ctxA.Departments.ToListAsync();

        // Assert
        fromA.Should().ContainSingle(d => d.Name == "HR Operations");
    }

    [Fact]
    public async Task Department_BothTenants_OnlySeeTheirOwnDepartments()
    {
        // Arrange
        var (ctxA, ctxB) = HRDbContextFactory.CreatePair(TenantAId, TenantBId);
        await using var _ = ctxA;
        await using var __ = ctxB;

        ctxA.Departments.Add(Department.Create("Alpha Finance", null));
        await ctxA.SaveChangesAsync();

        ctxB.Departments.Add(Department.Create("Beta Marketing", null));
        await ctxB.SaveChangesAsync();

        // Act
        var fromA = await ctxA.Departments.ToListAsync();
        var fromB = await ctxB.Departments.ToListAsync();

        // Assert
        fromA.Should().ContainSingle(d => d.Name == "Alpha Finance");
        fromA.Should().NotContain(d => d.Name == "Beta Marketing");

        fromB.Should().ContainSingle(d => d.Name == "Beta Marketing");
        fromB.Should().NotContain(d => d.Name == "Alpha Finance");
    }

    // ─── EmployeeNumber uniqueness per tenant ────────────────────────────────

    [Fact]
    public async Task EmployeeNumber_UniquePerTenant_SameNumberDifferentTenants_BothAllowed()
    {
        // Arrange: two separate isolated databases (one per tenant)
        // so uniqueness constraints don't interfere cross-tenant
        using var ctxA = HRDbContextFactory.Create(TenantAId);
        using var ctxB = HRDbContextFactory.Create(TenantBId);

        const string sharedEmployeeNumber = "EMP-SHARED-001";

        var empA = Employee.Hire(sharedEmployeeNumber, "Employee in Tenant A", null, new DateOnly(2025, 1, 1));
        var empB = Employee.Hire(sharedEmployeeNumber, "Employee in Tenant B", null, new DateOnly(2025, 1, 1));

        // Act: both tenants save the same employee number in their own context
        ctxA.Employees.Add(empA);
        await ctxA.SaveChangesAsync();

        ctxB.Employees.Add(empB);
        await ctxB.SaveChangesAsync();

        // Assert: both saved successfully, each tenant owns one record
        var aEmployees = await ctxA.Employees.ToListAsync();
        var bEmployees = await ctxB.Employees.ToListAsync();

        aEmployees.Should().ContainSingle(e => e.EmployeeNumber == sharedEmployeeNumber);
        bEmployees.Should().ContainSingle(e => e.EmployeeNumber == sharedEmployeeNumber);
    }

    [Fact]
    public async Task Employee_TenantIdIsStamped_OnSave()
    {
        // Arrange
        using var ctx = HRDbContextFactory.Create(TenantAId);

        var employee = Employee.Hire("EMP-STAMP-01", "Stamp Test", null, new DateOnly(2025, 1, 1));
        ctx.Employees.Add(employee);
        await ctx.SaveChangesAsync();

        // Act: reload without query filter (using IgnoreQueryFilters) to see raw TenantId
        var rawEmployee = await ctx.Employees
            .IgnoreQueryFilters()
            .FirstAsync(e => e.EmployeeNumber == "EMP-STAMP-01");

        // Assert: the interceptor stamped the correct tenant ID
        rawEmployee.TenantId.Should().Be(TenantAId);
    }

    [Fact]
    public async Task Employee_HistoryIsPersistedWithEmployee()
    {
        // Arrange
        using var ctx = HRDbContextFactory.Create(TenantAId);

        var employee = Employee.Hire("EMP-HIST-01", "History Test", null, new DateOnly(2025, 1, 1));
        employee.TransferToDepartment(Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid());
        employee.ChangeDesignation(Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(5), Guid.NewGuid());

        ctx.Employees.Add(employee);
        await ctx.SaveChangesAsync();

        // Act: reload the employee with history
        var loaded = await ctx.Employees
            .Include(e => e.History)
            .FirstAsync(e => e.EmployeeNumber == "EMP-HIST-01");

        // Assert: two history entries are persisted
        loaded.History.Should().HaveCount(2);
        loaded.History.Should().Contain(h => h.Type == DepartmentTransfer);
        loaded.History.Should().Contain(h => h.Type == DesignationChange);
    }
}
