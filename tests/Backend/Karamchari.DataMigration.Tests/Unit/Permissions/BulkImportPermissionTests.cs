// -----------------------------------------------------------------------
// <copyright file="BulkImportPermissionTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Identity.Services;
using Xunit;
using SecurityPermissions = Karamchari.Core.Security.Permissions;
using SecurityRoles = Karamchari.Core.Security.Roles;

namespace Karamchari.DataMigration.Tests.Unit.Permissions;

/// <summary>
/// Verifies the full permission matrix for bulk import operations
/// across every platform role.
/// </summary>
public sealed class BulkImportPermissionTests
{
    private readonly PermissionResolver _sut = new();

    private IReadOnlyCollection<string> PermissionsFor(string role) =>
        _sut.ResolvePermissions([role]);

    // ------------------------------------------------------------------ permission constant values

    [Fact]
    public void BulkImportRead_ConstantValue_IsCorrect()
    {
        SecurityPermissions.BulkImportRead.Should().Be("bulkimport.read");
    }

    [Fact]
    public void BulkImportCreate_ConstantValue_IsCorrect()
    {
        SecurityPermissions.BulkImportCreate.Should().Be("bulkimport.create");
    }

    [Fact]
    public void BulkImportExecute_ConstantValue_IsCorrect()
    {
        SecurityPermissions.BulkImportExecute.Should().Be("bulkimport.execute");
    }

    [Fact]
    public void BulkImportCancel_ConstantValue_IsCorrect()
    {
        SecurityPermissions.BulkImportCancel.Should().Be("bulkimport.cancel");
    }

    // ------------------------------------------------------------------ SecurityPermissions.All catalog

    [Fact]
    public void PermissionsAll_ContainsBulkImportRead()
    {
        SecurityPermissions.All.Should().Contain(SecurityPermissions.BulkImportRead);
    }

    [Fact]
    public void PermissionsAll_ContainsBulkImportCreate()
    {
        SecurityPermissions.All.Should().Contain(SecurityPermissions.BulkImportCreate);
    }

    [Fact]
    public void PermissionsAll_ContainsBulkImportExecute()
    {
        SecurityPermissions.All.Should().Contain(SecurityPermissions.BulkImportExecute);
    }

    [Fact]
    public void PermissionsAll_ContainsBulkImportCancel()
    {
        SecurityPermissions.All.Should().Contain(SecurityPermissions.BulkImportCancel);
    }

    // ------------------------------------------------------------------ Admin role

    [Fact]
    public void Admin_HasBulkImportRead()
    {
        PermissionsFor(SecurityRoles.Admin).Should().Contain(SecurityPermissions.BulkImportRead);
    }

    [Fact]
    public void Admin_HasBulkImportCreate()
    {
        PermissionsFor(SecurityRoles.Admin).Should().Contain(SecurityPermissions.BulkImportCreate);
    }

    [Fact]
    public void Admin_HasBulkImportExecute()
    {
        PermissionsFor(SecurityRoles.Admin).Should().Contain(SecurityPermissions.BulkImportExecute);
    }

    [Fact]
    public void Admin_HasBulkImportCancel()
    {
        PermissionsFor(SecurityRoles.Admin).Should().Contain(SecurityPermissions.BulkImportCancel);
    }

    // ------------------------------------------------------------------ SuperAdmin role

    [Fact]
    public void SuperAdmin_HasBulkImportRead()
    {
        PermissionsFor(SecurityRoles.SuperAdmin).Should().Contain(SecurityPermissions.BulkImportRead);
    }

    [Fact]
    public void SuperAdmin_HasBulkImportCreate()
    {
        PermissionsFor(SecurityRoles.SuperAdmin).Should().Contain(SecurityPermissions.BulkImportCreate);
    }

    [Fact]
    public void SuperAdmin_HasBulkImportExecute()
    {
        PermissionsFor(SecurityRoles.SuperAdmin).Should().Contain(SecurityPermissions.BulkImportExecute);
    }

    [Fact]
    public void SuperAdmin_HasBulkImportCancel()
    {
        PermissionsFor(SecurityRoles.SuperAdmin).Should().Contain(SecurityPermissions.BulkImportCancel);
    }

    // ------------------------------------------------------------------ Manager role

    [Fact]
    public void Manager_HasBulkImportRead()
    {
        PermissionsFor(SecurityRoles.Manager).Should().Contain(SecurityPermissions.BulkImportRead);
    }

    [Fact]
    public void Manager_DoesNotHaveBulkImportCreate()
    {
        PermissionsFor(SecurityRoles.Manager).Should().NotContain(SecurityPermissions.BulkImportCreate);
    }

    [Fact]
    public void Manager_DoesNotHaveBulkImportExecute()
    {
        PermissionsFor(SecurityRoles.Manager).Should().NotContain(SecurityPermissions.BulkImportExecute);
    }

    [Fact]
    public void Manager_DoesNotHaveBulkImportCancel()
    {
        PermissionsFor(SecurityRoles.Manager).Should().NotContain(SecurityPermissions.BulkImportCancel);
    }

    // ------------------------------------------------------------------ Employee role

    [Fact]
    public void Employee_DoesNotHaveBulkImportRead()
    {
        PermissionsFor(SecurityRoles.Employee).Should().NotContain(SecurityPermissions.BulkImportRead);
    }

    [Fact]
    public void Employee_DoesNotHaveBulkImportCreate()
    {
        PermissionsFor(SecurityRoles.Employee).Should().NotContain(SecurityPermissions.BulkImportCreate);
    }

    [Fact]
    public void Employee_DoesNotHaveBulkImportExecute()
    {
        PermissionsFor(SecurityRoles.Employee).Should().NotContain(SecurityPermissions.BulkImportExecute);
    }

    [Fact]
    public void Employee_DoesNotHaveBulkImportCancel()
    {
        PermissionsFor(SecurityRoles.Employee).Should().NotContain(SecurityPermissions.BulkImportCancel);
    }

    // ------------------------------------------------------------------ ReadOnly role

    [Fact]
    public void ReadOnly_HasBulkImportRead()
    {
        // ReadOnly gets all permissions ending with ".read"
        PermissionsFor(SecurityRoles.ReadOnly).Should().Contain(SecurityPermissions.BulkImportRead);
    }

    [Fact]
    public void ReadOnly_DoesNotHaveBulkImportCreate()
    {
        PermissionsFor(SecurityRoles.ReadOnly).Should().NotContain(SecurityPermissions.BulkImportCreate);
    }

    [Fact]
    public void ReadOnly_DoesNotHaveBulkImportExecute()
    {
        PermissionsFor(SecurityRoles.ReadOnly).Should().NotContain(SecurityPermissions.BulkImportExecute);
    }

    [Fact]
    public void ReadOnly_DoesNotHaveBulkImportCancel()
    {
        PermissionsFor(SecurityRoles.ReadOnly).Should().NotContain(SecurityPermissions.BulkImportCancel);
    }

    // ------------------------------------------------------------------ multi-role resolution

    [Fact]
    public void MultiRole_AdminAndEmployee_HasAllBulkImportPermissions()
    {
        var perms = _sut.ResolvePermissions([SecurityRoles.Admin, SecurityRoles.Employee]);

        perms.Should().Contain(SecurityPermissions.BulkImportRead);
        perms.Should().Contain(SecurityPermissions.BulkImportCreate);
        perms.Should().Contain(SecurityPermissions.BulkImportExecute);
        perms.Should().Contain(SecurityPermissions.BulkImportCancel);
    }

    [Fact]
    public void UnknownRole_HasNoBulkImportPermissions()
    {
        var perms = _sut.ResolvePermissions(["SomeUnknownRole"]);

        perms.Should().NotContain(SecurityPermissions.BulkImportRead);
        perms.Should().NotContain(SecurityPermissions.BulkImportCreate);
        perms.Should().NotContain(SecurityPermissions.BulkImportExecute);
        perms.Should().NotContain(SecurityPermissions.BulkImportCancel);
    }

    // ------------------------------------------------------------------ permission resolver contract

    [Fact]
    public void ResolvePermissions_EmptyRoles_ReturnsEmptySet()
    {
        var perms = _sut.ResolvePermissions(Array.Empty<string>());

        perms.Should().BeEmpty();
    }

    [Fact]
    public void GetPermissionVersion_ReturnsNonEmptyString()
    {
        _sut.GetPermissionVersion().Should().NotBeNullOrWhiteSpace();
    }
}
