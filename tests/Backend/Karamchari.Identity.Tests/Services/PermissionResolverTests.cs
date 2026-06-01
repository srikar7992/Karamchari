using FluentAssertions;
using Karamchari.Core.Security;
using Karamchari.Identity.Services;
using Xunit;

namespace Karamchari.Identity.Tests.Services;

public sealed class PermissionResolverTests
{
    private readonly PermissionResolver _sut = new();

    // ── Admin ────────────────────────────────────────────────────────────────

    [Fact]
    public void PermissionResolver_Admin_HasAllPermissions()
    {
        var result = _sut.ResolvePermissions([Roles.Admin]);

        result.Should().BeEquivalentTo(Permissions.All);
    }

    [Fact]
    public void PermissionResolver_SuperAdmin_HasAllPermissions()
    {
        var result = _sut.ResolvePermissions([Roles.SuperAdmin]);

        result.Should().BeEquivalentTo(Permissions.All);
    }

    // ── Manager ──────────────────────────────────────────────────────────────

    [Fact]
    public void PermissionResolver_Manager_HasEmployeeRead()
    {
        var result = _sut.ResolvePermissions([Roles.Manager]);

        result.Should().Contain(Permissions.EmployeeRead);
    }

    [Fact]
    public void PermissionResolver_Manager_HasPayrollRun()
    {
        var result = _sut.ResolvePermissions([Roles.Manager]);

        result.Should().Contain(Permissions.PayrollRun);
    }

    [Fact]
    public void PermissionResolver_Manager_DoesNotHaveRecruitmentHire()
    {
        var result = _sut.ResolvePermissions([Roles.Manager]);

        result.Should().NotContain(Permissions.RecruitmentHire);
    }

    // ── Employee ─────────────────────────────────────────────────────────────

    [Fact]
    public void PermissionResolver_Employee_HasEmployeeRead()
    {
        var result = _sut.ResolvePermissions([Roles.Employee]);

        result.Should().Contain(Permissions.EmployeeRead);
    }

    [Fact]
    public void PermissionResolver_Employee_HasLeaveRequest()
    {
        var result = _sut.ResolvePermissions([Roles.Employee]);

        result.Should().Contain(Permissions.LeaveRequest);
    }

    [Fact]
    public void PermissionResolver_Employee_DoesNotHavePayrollRun()
    {
        var result = _sut.ResolvePermissions([Roles.Employee]);

        result.Should().NotContain(Permissions.PayrollRun);
    }

    // ── ReadOnly ─────────────────────────────────────────────────────────────

    [Fact]
    public void PermissionResolver_ReadOnly_OnlyHasReadPermissions()
    {
        var result = _sut.ResolvePermissions([Roles.ReadOnly]);

        result.Should().NotBeEmpty();
        result.Should().OnlyContain(p => p.EndsWith(".read", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PermissionResolver_ReadOnly_DoesNotHaveWritePermissions()
    {
        var result = _sut.ResolvePermissions([Roles.ReadOnly]);

        result.Should().NotContain(Permissions.EmployeeWrite);
        result.Should().NotContain(Permissions.PayrollRun);
        result.Should().NotContain(Permissions.LeaveRequest);
        result.Should().NotContain(Permissions.ReimbursementSubmit);
        result.Should().NotContain(Permissions.BulkImportCreate);
    }

    // ── Recruiter ────────────────────────────────────────────────────────────

    [Fact]
    public void PermissionResolver_Recruiter_HasRecruitmentRead()
    {
        var result = _sut.ResolvePermissions([Roles.Recruiter]);

        result.Should().Contain(Permissions.RecruitmentRead);
    }

    [Fact]
    public void PermissionResolver_Recruiter_HasRecruitmentCreate()
    {
        var result = _sut.ResolvePermissions([Roles.Recruiter]);

        result.Should().Contain(Permissions.RecruitmentCreate);
    }

    [Fact]
    public void PermissionResolver_Recruiter_HasRecruitmentHire()
    {
        var result = _sut.ResolvePermissions([Roles.Recruiter]);

        result.Should().Contain(Permissions.RecruitmentHire);
    }

    [Fact]
    public void PermissionResolver_Recruiter_HasEmployeeRead()
    {
        var result = _sut.ResolvePermissions([Roles.Recruiter]);

        result.Should().Contain(Permissions.EmployeeRead);
    }

    [Fact]
    public void PermissionResolver_Recruiter_DoesNotHavePayrollRun()
    {
        var result = _sut.ResolvePermissions([Roles.Recruiter]);

        result.Should().NotContain(Permissions.PayrollRun);
    }

    // ── Unknown / empty ──────────────────────────────────────────────────────

    [Fact]
    public void PermissionResolver_UnknownRole_HasNoPermissions()
    {
        var result = _sut.ResolvePermissions(["UnknownRole"]);

        result.Should().BeEmpty();
    }

    // ── Multiple roles ───────────────────────────────────────────────────────

    [Fact]
    public void PermissionResolver_MultipleRoles_CombinesPermissions()
    {
        // Employee + Recruiter → union of both sets
        var result = _sut.ResolvePermissions([Roles.Employee, Roles.Recruiter]);

        // From Employee
        result.Should().Contain(Permissions.LeaveRequest);
        result.Should().Contain(Permissions.ReimbursementSubmit);

        // From Recruiter
        result.Should().Contain(Permissions.RecruitmentHire);
        result.Should().Contain(Permissions.RecruitmentInterview);

        // Shared (de-duped — no duplicate entries)
        result.Should().Contain(Permissions.EmployeeRead);
        result.Count(p => p == Permissions.EmployeeRead).Should().Be(1);
    }

    // ── Version ──────────────────────────────────────────────────────────────

    [Fact]
    public void PermissionResolver_GetPermissionVersion_ReturnsNonEmpty()
    {
        var version = _sut.GetPermissionVersion();

        version.Should().NotBeNullOrWhiteSpace();
    }
}
