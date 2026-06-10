// -----------------------------------------------------------------------
// <copyright file="PermissionsTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Core.Security;
using Xunit;

namespace Karamchari.Identity.Tests.Security;

public sealed class PermissionsTests
{
    [Fact]
    public void Permissions_All_ContainsRecruitmentPermissions()
    {
        Permissions.All.Should().Contain(Permissions.RecruitmentRead);
        Permissions.All.Should().Contain(Permissions.RecruitmentCreate);
        Permissions.All.Should().Contain(Permissions.RecruitmentUpdate);
        Permissions.All.Should().Contain(Permissions.RecruitmentInterview);
        Permissions.All.Should().Contain(Permissions.RecruitmentOffer);
        Permissions.All.Should().Contain(Permissions.RecruitmentHire);
    }

    [Fact]
    public void Permissions_All_ContainsEmployeePermissions()
    {
        Permissions.All.Should().Contain(Permissions.EmployeeRead);
        Permissions.All.Should().Contain(Permissions.EmployeeWrite);
        Permissions.All.Should().Contain(Permissions.EmployeeDelete);
    }

    [Fact]
    public void Permissions_RecruitmentRead_EndsWithRead()
    {
        Permissions.RecruitmentRead.Should().EndWith(".read");
    }

    [Fact]
    public void Permissions_All_IsNonEmpty()
    {
        Permissions.All.Should().NotBeEmpty();
    }
}
