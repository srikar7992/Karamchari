using FluentAssertions;
using Karamchari.HR.Domain.Departments;
using Xunit;

namespace Karamchari.HR.Tests.Domain;

public sealed class DepartmentTests
{
    // ─── Helpers ────────────────────────────────────────────────────────────

    private static Department CreateDefault(
        string name = "Engineering",
        string? description = "Software engineering department")
        => Department.Create(name, description);

    // ─── Create ──────────────────────────────────────────────────────────────

    [Fact]
    public void Department_Create_SetsActiveTrue()
    {
        var department = CreateDefault();

        department.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Department_Create_SetsName()
    {
        var department = CreateDefault(name: "Finance");

        department.Name.Should().Be("Finance");
    }

    [Fact]
    public void Department_Create_SetsDescription()
    {
        var department = CreateDefault(description: "Handles all financial operations");

        department.Description.Should().Be("Handles all financial operations");
    }

    [Fact]
    public void Department_Create_NullDescription_StoredAsNull()
    {
        var department = CreateDefault(description: null);

        department.Description.Should().BeNull();
    }

    [Fact]
    public void Department_Create_AssignsNewGuidId()
    {
        var department = CreateDefault();

        department.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Department_Create_TrimsNameWhitespace()
    {
        var department = CreateDefault(name: "  HR  ");

        department.Name.Should().Be("HR");
    }

    [Fact]
    public void Department_Create_EmptyName_ThrowsArgumentException()
    {
        var act = () => CreateDefault(name: "");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Department_Create_WhitespaceName_ThrowsArgumentException()
    {
        var act = () => CreateDefault(name: "   ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Department_Create_WhitespaceDescription_NormalizesToNull()
    {
        var department = CreateDefault(description: "   ");

        department.Description.Should().BeNull();
    }

    [Fact]
    public void Department_Create_TwoDepartments_HaveDifferentIds()
    {
        var dept1 = CreateDefault("HR");
        var dept2 = CreateDefault("Finance");

        dept1.Id.Should().NotBe(dept2.Id);
    }

    // ─── Rename ───────────────────────────────────────────────────────────────

    [Fact]
    public void Department_Rename_ChangesName()
    {
        var department = CreateDefault(name: "Old Name");

        department.Rename("New Name");

        department.Name.Should().Be("New Name");
    }

    [Fact]
    public void Department_Rename_TrimsWhitespace()
    {
        var department = CreateDefault();

        department.Rename("  Sales  ");

        department.Name.Should().Be("Sales");
    }

    [Fact]
    public void Department_Rename_EmptyString_ThrowsArgumentException()
    {
        var department = CreateDefault();

        var act = () => department.Rename("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Department_Rename_DoesNotAffectOtherProperties()
    {
        var department = CreateDefault(name: "Engineering", description: "Engineers");
        var originalId = department.Id;
        var originalIsActive = department.IsActive;

        department.Rename("Platform Engineering");

        department.Id.Should().Be(originalId);
        department.IsActive.Should().Be(originalIsActive);
        department.Description.Should().Be("Engineers");
    }

    // ─── ChangeDescription ───────────────────────────────────────────────────

    [Fact]
    public void Department_ChangeDescription_ChangesDescription()
    {
        var department = CreateDefault(description: "Old desc");

        department.ChangeDescription("New description");

        department.Description.Should().Be("New description");
    }

    [Fact]
    public void Department_ChangeDescription_ToNull_SetsNull()
    {
        var department = CreateDefault(description: "Some description");

        department.ChangeDescription(null);

        department.Description.Should().BeNull();
    }

    [Fact]
    public void Department_ChangeDescription_WhitespaceString_NormalizesToNull()
    {
        var department = CreateDefault(description: "Some description");

        department.ChangeDescription("   ");

        department.Description.Should().BeNull();
    }

    [Fact]
    public void Department_ChangeDescription_DoesNotAffectNameOrStatus()
    {
        var department = CreateDefault(name: "Engineering");

        department.ChangeDescription("Updated description");

        department.Name.Should().Be("Engineering");
        department.IsActive.Should().BeTrue();
    }

    // ─── Deactivate ───────────────────────────────────────────────────────────

    [Fact]
    public void Department_Deactivate_SetsActiveFalse()
    {
        var department = CreateDefault();

        department.Deactivate();

        department.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Department_Deactivate_AlreadyInactive_IsIdempotent_NoException()
    {
        var department = CreateDefault();
        department.Deactivate();

        // Calling again should not throw - idempotent
        var act = () => department.Deactivate();

        act.Should().NotThrow();
        department.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Department_Deactivate_DoesNotAffectNameOrDescription()
    {
        var department = CreateDefault(name: "HR", description: "Human Resources");

        department.Deactivate();

        department.Name.Should().Be("HR");
        department.Description.Should().Be("Human Resources");
    }
}
