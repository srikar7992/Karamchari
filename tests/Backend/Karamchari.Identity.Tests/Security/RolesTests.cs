using FluentAssertions;
using Karamchari.Core.Security;
using Xunit;

namespace Karamchari.Identity.Tests.Security;

public sealed class RolesTests
{
    [Fact]
    public void Roles_All_ContainsAdmin()
    {
        Roles.All.Should().Contain(Roles.Admin);
    }

    [Fact]
    public void Roles_All_ContainsManager()
    {
        Roles.All.Should().Contain(Roles.Manager);
    }

    [Fact]
    public void Roles_All_ContainsEmployee()
    {
        Roles.All.Should().Contain(Roles.Employee);
    }

    [Fact]
    public void Roles_All_ContainsReadOnly()
    {
        Roles.All.Should().Contain(Roles.ReadOnly);
    }

    [Fact]
    public void Roles_All_ContainsRecruiter()
    {
        Roles.All.Should().Contain(Roles.Recruiter);
    }

    [Fact]
    public void Roles_RecruiterConstant_Is_Recruiter()
    {
        Roles.Recruiter.Should().Be("Recruiter");
    }
}
